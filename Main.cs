using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace DualWield
{
    public class Main : Script
    {
        public static bool DualWielding = false;
        private bool imReloading = false;
        private bool GunSwap = false;
        private bool GunMoved = false;
        private bool GunReset = true;
        private bool AimDown = false;
        private bool AimUp = false;
        private bool AimUp_1 = false;
        private bool AimUp_2 = false;
        private bool AimDown_1 = false;
        private int shootCycle = 0;
        private int oneMag;
        private int bothMags;
        private int accuracy;
        private Ped ShooterL;
        private Ped ShooterR;
        private Entity GunL;
        private Entity GunR;
        public static Ped Char = Game.Player.Character;
        private int gameTimer;
        private readonly List<WeaponGroup> WpnFilter = new List<WeaponGroup>()
        { WeaponGroup.Pistol,  WeaponGroup.Shotgun };
        private readonly List<WeaponGroup> WpnFilter2 = new List<WeaponGroup>()
        { WeaponGroup.SMG, WeaponGroup.AssaultRifle, WeaponGroup.MG };
        public static readonly List<WeaponGroup> WpnOff = new List<WeaponGroup>()
        { WeaponGroup.Melee, WeaponGroup.Parachute, WeaponGroup.Thrown, WeaponGroup.PetrolCan, WeaponGroup.Stungun,
            WeaponGroup.Unarmed, WeaponGroup.FireExtinguisher, WeaponGroup.Sniper, WeaponGroup.Heavy};
        private readonly ClipSet wpnAnim = new ClipSet("weapons@pistol@");
        private readonly CrClipAsset turretAnim = new CrClipAsset("anim@veh@armordillo@turret@base", "sit");
        private readonly CrClipAsset dodgeAnim = new CrClipAsset("amb@world_human_sunbathe@female@front@base", "base");
        private readonly CrClipAsset handAnim = new CrClipAsset("move_fall@weapons@jerrycan", "land_walk_arms");
        private readonly CrClipAsset aimDown = new CrClipAsset("anim@heists@box_carry@", "idle");
        private readonly CrClipAsset aimUp = new CrClipAsset("amb@world_human_yoga@female@base", "base_b");
        private readonly Vector3 aimPosL = new Vector3(0.17f, 0.031f, 0.01f);
        private readonly Vector3 aimRotL = new Vector3(70f, 175f, 165f);
        public static readonly Vector3 aimPosR = new Vector3(0.17f, 0.041f, 0f);
        public static readonly Vector3 aimRotR = new Vector3(95f, 195f, 168f);
        private readonly Vector3 aimUpRotL = new Vector3(70f, 175f, 165f);
        private readonly Vector3 aimUpRotR = new Vector3(110f, 175f, 165f);
        private readonly Vector3 aimDownRotL = new Vector3(100f, 160f, 175f);
        private readonly Vector3 aimDownRotR = new Vector3(80f, 205f, 170f);
        private readonly Vector3 aimRotL_Dodge = new Vector3(70f, 150f, 165f);
        private readonly Vector3 aimRotR_Dodge = new Vector3(95f, 200f, 168f);
        private readonly float aimDownDeg = -30.0f;
        private readonly float aimUpDeg = 33.0f;

        public static int Shootdodge;
        public static Type DodgeType;
        public static FieldInfo DodgeField;
        public static Weapon CharWpn;
        private Weapon lastWpn;
        public static bool Conflict = false;
        public static bool Notified = false;
        public static float ikRecoil = 0f;

        private float padButtonTimer = 0f; // Variable to track hold time
        private float padButtonTimer2 = 0f; // Variable to track cooldown time

        public Main()
        {
            Tick += OnTick;
            KeyDown += OnKeyDown;
            Utils.ConflictGetter();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Config.toggleKey)
                return;
            if (!DualWielding)
                StartDualWield();
            else
                EndOnPressed();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (Game.IsLoading || Game.IsPaused) return;
            Utils.CheckConflict();
            Char = Game.Player.Character;
            CharWpn = Char.Weapons.Current;
            CheckController();
            // new TextElement("Debug" + , new PointF(50f, 38f), 0.5f).ScaledDraw();

            if (CharWpn.IsPresent && !DualWielding && !WpnOff.Contains(CharWpn.Group))
                Char.Weapons.CurrentWeaponObject.IsVisible = true;

            if (!DualWielding)
                return;
            if (!Char.IsInVehicle() && !WpnOff.Contains(CharWpn.Group))
            {
                GunL = ShooterL.Weapons.CurrentWeaponObject;
                GunR = ShooterR.Weapons.CurrentWeaponObject;
                ShooterL.PositionNoOffset = Char.Position + new Vector3(0f, 0f, 1000f);
                ShooterR.PositionNoOffset = Char.Position + new Vector3(0f, 0f, 1000f);
                ShooterL.Weapons.Current.InfiniteAmmoClip = true;
                ShooterR.Weapons.Current.InfiniteAmmoClip = true;
                //Disable Roll & No Changing Weapon On Reload
                if (Char.IsAiming)
                    Game.DisableControlThisFrame(GTA.Control.Jump);
                if (Char.IsReloading)
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);
                    Game.DisableControlThisFrame(GTA.Control.SelectWeapon); Game.DisableControlThisFrame(GTA.Control.SelectPrevWeapon); Game.DisableControlThisFrame(GTA.Control.SelectNextWeapon);
                    Hud.HideComponentThisFrame(HudComponent.WeaponWheel);
                }
                //In-Cover & Jumping Pose
                if (!Char.IsAiming && Game.IsControlJustPressed(GTA.Control.Jump))
                {
                    if (!Char.IsPlayingAnimation(handAnim))
                        Char.Task.PlayAnimation(handAnim, AnimationBlendDelta.InstantBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)48, 0f);
                }
                else if ((Char.IsInCover || Char.IsGoingIntoCover) && !Char.IsAiming)
                {
                    if (!Char.IsPlayingAnimation(handAnim))
                        Char.Task.PlayAnimation(handAnim, AnimationBlendDelta.VerySlowBlendIn, AnimationBlendDelta.InstantBlendOut, -1, (AnimationFlags)48, 0f);
                    else if (Char.GetAnimationCurrentTime(handAnim) > 0.2f)
                        Char.SetAnimationSpeed(handAnim, 0f);
                }
                else if (!Char.IsJumping && Char.IsPlayingAnimation(handAnim))
                    Char.Task.StopScriptedAnimationTask(handAnim, AnimationBlendDelta.SlowBlendOut);
                //Normal Aiming Pose
                if (Char.IsAiming && !Char.IsReloading && !Char.IsFalling && Shootdodge == 0 && !AimDown && !AimUp)
                {
                    if (!Char.IsPlayingAnimation(turretAnim))
                        Char.Task.PlayAnimation(turretAnim, AnimationBlendDelta.SlowBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0f);
                }
                else if (Char.IsPlayingAnimation(turretAnim))
                    Char.Task.StopScriptedAnimationTask(turretAnim, AnimationBlendDelta.SlowBlendOut);
                //Weapon Pitch Trickery
                if (Shootdodge == 0 && Config.pitchAnims)
                {
                    //AimDown_Pose
                    float pitch = GameplayCamera.RelativePitch;
                    if (!AimDown && pitch < aimDownDeg && Char.IsPlayingAnimation(turretAnim))
                    {
                        if (!Char.IsPlayingAnimation(aimDown))
                        {
                            Char.Task.PlayAnimation(aimDown, AnimationBlendDelta.VerySlowBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0f);
                            GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimDownRotL, false, false, false, true, default);
                            GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimDownRotR, false, false, false, true, default);
                        }
                        AimDown = true;
                        if (AimDown_1)
                            AimDown_1 = false;
                    }
                    if (AimDown && (pitch >= aimDownDeg || !Char.IsAiming))
                    {
                        if (Char.IsPlayingAnimation(aimDown))
                            Char.Task.StopScriptedAnimationTask(aimDown, AnimationBlendDelta.SlowBlendOut);
                        if (!AimDown_1)
                        {
                            GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL, false, false, false, true, default);
                            GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR, false, false, false, true, default);
                        }
                        AimDown = false;
                    }
                    //AimUp_Pose
                    if (!AimUp && pitch > aimUpDeg && Char.IsPlayingAnimation(turretAnim))
                    {
                        if (!Char.IsPlayingAnimation(aimUp))
                        {
                            Char.Task.PlayAnimation(aimUp, AnimationBlendDelta.VerySlowBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0.07f);
                            GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimUpRotL, false, false, false, true, default);
                            GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimUpRotR, false, false, false, true, default);
                        }
                        AimUp = true;
                        if (AimUp_2)
                            AimUp_2 = false;
                    }
                    if (Char.GetAnimationCurrentTime(aimUp) > 0.08f)
                        Char.SetAnimationSpeed(aimUp, 0f);
                    if (AimUp && (pitch <= aimUpDeg || !Char.IsAiming))
                    {
                        if (Char.IsPlayingAnimation(aimUp))
                            Char.Task.StopScriptedAnimationTask(aimUp, AnimationBlendDelta.SlowBlendOut);
                        if (!AimUp_1 || !AimUp_2)
                        {
                            GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL, false, false, false, true, default);
                            GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR, false, false, false, true, default);
                        }
                        AimUp = false;
                    }
                    //Semi_AimUp
                    float aimUpDeg_1 = aimUpDeg - 20f;
                    float aimUpDeg_2 = aimUpDeg - 10f;
                    Vector3 posAdj = new Vector3(0f, 0.025f, 0f);
                    if (!AimUp_1 && pitch > aimUpDeg_1 && pitch < aimUpDeg_2 && Char.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 15f);
                        GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL + posAdj, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR + posAdj, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_1 = true;
                    }
                    if (!AimUp_2 && pitch > aimUpDeg_2 && Char.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 25f);
                        GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL + posAdj * 2, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR + posAdj * 2, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_2 = true;
                    }
                    //Semi_AimDown
                    float aimDownDeg_1 = aimDownDeg + 15f;
                    if (!AimDown_1 && pitch < aimDownDeg_1 && Char.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 10f);

                        GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL - pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR - pitchAdj, false, false, false, true, default);
                        AimDown_1 = true;
                    }
                    //SemiAimExit
                    if ((AimUp_1 && pitch <= aimUpDeg_1) || (AimUp_2 && pitch <= aimUpDeg_2) || (AimDown_1 && pitch >= aimDownDeg_1) || !Char.IsAiming)
                    {
                        GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL, false, false, false, true, default);
                        GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR, false, false, false, true, default);
                        if (AimUp_1) { AimUp_1 = false; }
                        if (AimUp_2) { AimUp_2 = false; }
                        if (AimDown_1) { AimDown_1 = false; }
                    }
                }
                //SetIK
                if (Char.IsAiming && !Char.IsReloading && !Char.IsJumping && GameplayCamera.FollowPedCamViewMode != CamViewMode.FirstPerson)
                {
                    Utils.SetIK(true);
                    float camRotZ = GameplayCamera.Rotation.Z;
                    if (Shootdodge > 0)
                    {
                        if (!GunMoved)
                        {
                            GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL_Dodge, false, false, false, true, default);
                            GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR_Dodge, false, false, false, true, default);
                        }
                        GameplayCamera.SetThirdPersonCameraRelativeHeadingLimitsThisUpdate(-23.5f, 23.5f);
                        GunMoved = true;
                        GunReset = false;
                    }
                    else if (Shootdodge == 0)
                        Char.Heading = camRotZ;
                    if (Shootdodge == 0)
                        Utils.SetIkTarget(Char);
                }
                //Weapon change when aiming in FirstPersonView broke the script. Fuck It!
                if (Char.IsAiming && GameplayCamera.FollowPedCamViewMode == CamViewMode.FirstPerson)
                {
                    Utils.SetIK(false);
                    Game.DisableControlThisFrame(GTA.Control.SelectWeapon); Game.DisableControlThisFrame(GTA.Control.SelectPrevWeapon); Game.DisableControlThisFrame(GTA.Control.SelectNextWeapon);
                    Hud.HideComponentThisFrame(HudComponent.WeaponWheel);
                }
                //ShootdodgeSequence
                if (Shootdodge != 0)
                {
                    if (Char.IsPlayingAnimation(turretAnim))
                        Char.Task.StopScriptedAnimationTask(turretAnim);
                    if (!Char.IsPlayingAnimation(dodgeAnim))
                        Char.Task.PlayAnimation(dodgeAnim, AnimationBlendDelta.NormalBlendIn, AnimationBlendDelta.NormalBlendOut, -1, (AnimationFlags)49, 0f);
                }
                else if (Char.IsPlayingAnimation(dodgeAnim))
                    Char.Task.StopScriptedAnimationTask(dodgeAnim);
                //Revert Gun Pos If No Shootdodge
                if (Shootdodge == 0)
                {
                    if (GunMoved)
                        GunMoved = false;
                    if (!GunMoved && !GunReset)
                    {
                        GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL, false, false, false, true, default);
                        GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR, false, false, false, true, default);
                        GunReset = true;
                    }
                }
                //ReloadSequence
                if (Shootdodge == 0)
                {
                    if ((Game.IsControlJustPressed(GTA.Control.Reload) || bothMags == 0) && !imReloading && bothMags != oneMag * 2)
                        Reload1();
                    if (imReloading && !GunL.IsVisible && !Char.IsReloading)
                        Reload2();
                }
                else
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);
                    if (bothMags == 0)
                    {
                        CharWpn.Ammo -= oneMag;
                        CharWpn.AmmoInClip = 0;
                        Game.TimeScale = 1f;
                        Game.Player.DisableFiringThisFrame();
                    }
                }
                //Prevent Normal Reload
                if (CharWpn.AmmoInClip < CharWpn.MaxAmmoInClip)
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);
                    if (Game.IsControlJustPressed(GTA.Control.Reload))
                        CharWpn.AmmoInClip = CharWpn.MaxAmmoInClip;
                }
                //ShowWeapon    
                if (!imReloading && !Char.IsReloading)
                {
                    if (!GunL.IsVisible && !GunR.IsVisible)
                    {
                        Utils.ShowPlayerWpn(false);
                        GunL.IsVisible = true;
                        GunR.IsVisible = true;
                    }
                    else if (Char.Weapons.CurrentWeaponObject.IsVisible)
                        Utils.ShowPlayerWpn(false);
                }
                //ShootingSequence
                if (Char.IsShooting && !Char.IsReloading && bothMags >= 1)
                {
                    Char.Weapons.CurrentWeaponObject.RemoveParticleEffects();
                    Utils.SortPtfx();
                    Utils.surpressed = CharWpn.Components.GetSuppressorComponent().Active;
                    RaycastResult raycast = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 9999f, IntersectFlags.Everything, Char);
                    if (WpnFilter.Contains(CharWpn.Group))
                    {
                        Utils.PlayerDamage(0.9f);
                        if (shootCycle == 0)
                            Utils.ShootAt(raycast, ShooterL, GunL);
                        else
                            Utils.ShootAt(raycast, ShooterR, GunR);
                        if (shootCycle == 0)
                        {
                            shootCycle = 1;
                            --bothMags;
                        }
                        else
                        {
                            shootCycle = 0;
                            --bothMags;
                        }
                    }
                    else if (WpnFilter2.Contains(CharWpn.Group) && gameTimer <= 0)
                    {
                        Utils.PlayerDamage(0.7f);
                        if (shootCycle == 0)
                            Utils.ShootAt(raycast, ShooterL, GunL);
                        else
                            Utils.ShootAt(raycast, ShooterR, GunR);
                        if (shootCycle == 0)
                        {
                            shootCycle = 1;
                            --bothMags;
                        }
                        else
                        {
                            shootCycle = 0;
                            --bothMags;
                        }
                        gameTimer = Game.GameTime + 75;
                    }
                    Utils.FakeRecoil(shootCycle);
                }
                else if (GameplayCamera.IsShaking && Config.recoil > 0.0f) GameplayCamera.StopShaking();
                oneMag = CharWpn.AmmoInClip;
                if (Game.GameTime >= gameTimer && WpnFilter2.Contains(CharWpn.Group))
                    gameTimer = 0;
            }

            foreach (Ped allPed in World.GetAllPeds())
            {
                if (allPed.IsInCombatAgainst(ShooterL) || allPed.IsInCombatAgainst(ShooterR))
                {
                    allPed.Task.ClearAll();
                    allPed.Task.Combat(Char);
                }
            }
            //WeaponSwitching
            if (Game.IsControlJustPressed(GTA.Control.SelectWeapon))
                lastWpn = CharWpn;
            if (Function.Call<bool>(Hash.IS_PED_SWITCHING_WEAPON, Char))
            {
                GunSwap = true;
                EndDualWield();
            }
            if (GunSwap && !DualWielding && !WpnOff.Contains(CharWpn.Group))
            {
                GunSwap = false;
                StartDualWield();
            }

            if (Char.IsRagdoll)
                EndDualWield();

            if (Char.IsAiming || imReloading)
                DisplayHud();

            if ((Char.IsGettingIntoVehicle || !Char.IsAlive || Char.IsSwimming || (CharWpn.Ammo - CharWpn.AmmoInClip <= CharWpn.MaxAmmoInClip && Char.IsReloading) || WpnOff.Contains(CharWpn.Group)) && !GunSwap)
                EndDualWield();
        }

        private void StartDualWield()
        {
            if (DualWielding)
                return;
            else if (!WpnOff.Contains(CharWpn.Group) && CharWpn.Ammo - CharWpn.AmmoInClip >= CharWpn.MaxAmmoInClip && !Char.IsInVehicle() &&
                    !Char.IsRagdoll && Char.IsAlive && !Char.IsFalling && !Char.IsSwimming && Shootdodge == 0)
            {
                if (Conflict)
                    Notified = false;
                Utils.LoadClipSet(wpnAnim);
                wpnAnim.Request();
                accuracy = Char.Accuracy;
                oneMag = CharWpn.AmmoInClip;
                CharWpn.Ammo -= oneMag;
                imReloading = false;
                Char.Task.PlayAnimation("melee@holster", "unholster", 8f, -1, (AnimationFlags)48);
                Utils.ShowPlayerWpn(false);
                if (Char.GetAnimationCurrentTime(new CrClipAsset("melee@holster", "unholster")) < 1.0f)
                    Wait(0);
                ShooterL = CreateFakeShooter(1);
                ShooterR = CreateFakeShooter(2);
                CharWpn.InfiniteAmmoClip = true;
                bothMags = oneMag * 2;
                Char.Accuracy = 0;
                Char.SetWeaponMovementClipSet(wpnAnim);
                lastWpn = CharWpn;
                AimDown = false;
                AimUp = false;
                DualWielding = true;
            }
        }

        private Ped CreateFakeShooter(int LR)
        {
            Ped ped = World.CreatePed((Model)PedHash.Famdnf01GMY, Char.Position);
            ped.IsVisible = false;
            ped.Weapons.Give(CharWpn.Hash, 999, true, true);
            Utils.GetAttachments(ped);
            ped.Weapons.CurrentWeaponObject.IsVisible = false;
            ped.Weapons.Select(CharWpn.Hash, true);
            Function.Call(Hash.SET_ENTITY_PROOFS, ped, true, true, true, true, true, true, true, true);
            Function.Call(Hash.STOP_PED_SPEAKING, ped, true);
            ped.CanRagdoll = false;
            ped.IsCollisionEnabled = false;
            ped.IsPositionFrozen = true;
            ped.BlockPermanentEvents = true;
            ped.RelationshipGroup = Char.RelationshipGroup;
            ped.Accuracy = 0;
            ped.Weapons.CurrentWeaponObject.Detach();
            if (LR == 1)
            {
                ped.Weapons.CurrentWeaponObject.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL, false, false, false, true, default);
            }
            else
            {
                ped.Weapons.CurrentWeaponObject.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR, false, false, false, true, default);
            }
            ped.Weapons.CurrentWeaponObject.IsVisible = true;
            return ped;
        }

        private void Reload1()
        {
            Char.BlocksAnyDamageButHasReactions = true;
            Char.ResetWeaponMovementClipSet();
            Char.Task.ReloadWeapon();
            GunL.IsVisible = false;
            GunR.IsVisible = false;
            Utils.ShowPlayerWpn(true);
            Char.Task.PlayAnimation("weapon@w_sp_jerrycan", "holster", 8f, -1, (AnimationFlags)48);
            oneMag = CharWpn.AmmoInClip;
            if (Char.GetAnimationCurrentTime(new CrClipAsset("weapon@w_sp_jerrycan", "holster")) < 1.0f)
                Wait(0);
            if (bothMags != (oneMag * 2) - 1)
                imReloading = true;
            else
            {
                bothMags = oneMag * 2;
                CharWpn.Ammo -= 1;
                Char.SetWeaponMovementClipSet(wpnAnim);
            }
            Char.BlocksAnyDamageButHasReactions = false;
        }

        private void Reload2()
        {
            if (CharWpn.AmmoInClip >= CharWpn.MaxAmmoInClip)
                imReloading = false;
            Char.BlocksAnyDamageButHasReactions = true;
            GunL.IsVisible = false;
            GunR.IsVisible = false;
            Utils.ShowPlayerWpn(true);
            Char.Task.PlayAnimation("melee@holster", "holster", 8f, -1, (AnimationFlags)48);
            if (Char.GetAnimationCurrentTime(new CrClipAsset("melee@holster", "holster")) < 1.0f)
                Wait(0);
            Char.Task.ReloadWeapon();
            Char.Task.PlayAnimation("weapon@w_sp_jerrycan", "unholster", 8f, -1, (AnimationFlags)48);
            if (Char.GetAnimationCurrentTime(new CrClipAsset("weapon@w_sp_jerrycan", "unholster")) < 1.0f)
                Wait(0);
            Char.Task.PlayAnimation("weapon@w_sp_jerrycan", "holster_2_aim", 8f, -1, (AnimationFlags)48);
            if (Char.GetAnimationCurrentTime(new CrClipAsset("weapon@w_sp_jerrycan", "holster_2_aim")) < 1.0f)
                Wait(0);
            if (bothMags != 0)
            {
                int ammoUsed = (oneMag * 2) - bothMags;
                CharWpn.Ammo -= ammoUsed;
                bothMags = oneMag * 2;
            }
            else
            {
                bothMags = oneMag * 2;
                CharWpn.Ammo -= bothMags;
            }
            imReloading = false;
            Char.BlocksAnyDamageButHasReactions = false;
            Char.SetWeaponMovementClipSet(wpnAnim);
        }

        private void EndDualWield()
        {
            Utils.SetIK(false);
            Char.ResetWeaponMovementClipSet();
            Char.Task.StopScriptedAnimationTask(turretAnim, AnimationBlendDelta.VerySlowBlendOut);
            ShooterL.MarkAsNoLongerNeeded();
            ShooterR.MarkAsNoLongerNeeded();
            ShooterL.Delete();
            ShooterR.Delete();
            Char.Accuracy = accuracy;
            if (!GunSwap) // Stop Ammo Loss when Weapon Changed
            {
                CharWpn.InfiniteAmmoClip = false;
                CharWpn.Ammo += bothMags / 2;
                CharWpn.AmmoInClip = bothMags / 2;
            }
            else
            {
                lastWpn.InfiniteAmmoClip = false;
                lastWpn.Ammo += bothMags / 2;
                lastWpn.AmmoInClip = bothMags / 2;
            }
            Utils.PlayerDamage(1f);
            Utils.ShowPlayerWpn(true);
            shootCycle = 0;
            imReloading = false;
            AimDown = false;
            AimUp = false;
            DualWielding = false;
        }

        private void EndOnPressed()
        {
            if (DualWielding)
            {
                Char.Task.PlayAnimation("melee@holster", "holster", 8f, -1, (AnimationFlags)48);
                if (Char.GetAnimationCurrentTime(new CrClipAsset("melee@holster", "holster")) < 1.0f)
                    Wait(0);
                EndDualWield();
            }
        }

        private void DisplayHud()
        {
            new TextElement("" + bothMags, new PointF(1255f, 60f), 0.52f, Color.Red, GTA.UI.Font.Pricedown, Alignment.Center, false, true).ScaledDraw();
            new TextElement("Dual", new PointF(1210f, 61f), 0.25f, Color.Red, GTA.UI.Font.RockstarTag, Alignment.Left, false, true).ScaledDraw();
            new TextElement("Wield", new PointF(1210f, 69f), 0.25f, Color.Red, GTA.UI.Font.RockstarTag, Alignment.Left, false, true).ScaledDraw();
        }

        private void CheckController()
        {
            if (Game.LastInputMethod == InputMethod.GamePad && Char.IsAiming)
            {
                if (padButtonTimer2 > 0)
                {
                    padButtonTimer2 -= Game.LastFrameTime * 1000;
                    return;
                }

                if (Game.IsControlPressed(GTA.Control.PhoneRight))
                {
                    padButtonTimer += Game.LastFrameTime * 1000; 
                    if (padButtonTimer >= 1000f) // Holding timer
                    {
                        if (!DualWielding)
                        {
                            StartDualWield();
                            padButtonTimer2 = 3000f; // Cooldown after gamepad hold button
                        }
                        else
                        {
                            EndOnPressed();
                            padButtonTimer2 = 3000f;
                        }
                    }
                }
                else
                {
                    padButtonTimer = 0f;
                }
            }
        }
    }
}
