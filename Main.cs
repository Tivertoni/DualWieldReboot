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
        private readonly List<WeaponGroup> WpnOff = new List<WeaponGroup>()
        { WeaponGroup.Melee, WeaponGroup.Parachute, WeaponGroup.Thrown, WeaponGroup.PetrolCan, WeaponGroup.Stungun,
            WeaponGroup.Unarmed, WeaponGroup.FireExtinguisher, WeaponGroup.Sniper, WeaponGroup.Heavy};
        private readonly ClipSet wpnAnim = new ClipSet("weapons@pistol@");
        private readonly CrClipAsset turretAnim = new CrClipAsset("anim@veh@armordillo@turret@base", "sit");
        private readonly CrClipAsset dodgeAnim = new CrClipAsset("amb@world_human_sunbathe@female@front@base", "base");
        private readonly CrClipAsset handAnim = new CrClipAsset("move_fall@weapons@jerrycan", "land_walk_arms");
        private readonly CrClipAsset aimDown = new CrClipAsset("anim@heists@box_carry@", "idle");
        private readonly CrClipAsset aimUp = new CrClipAsset("amb@world_human_yoga@female@base", "base_b");
        private readonly Vector3 aimRotL = new Vector3(70f, 175f, 165f);
        private readonly Vector3 aimRotR = new Vector3(95f, 195f, 168f);
        private readonly Vector3 aimUpRotL = new Vector3(70f, 175f, 165f);
        private readonly Vector3 aimUpRotR = new Vector3(110f, 175f, 165f);
        private readonly Vector3 aimDownRotL = new Vector3(100f, 160f, 165f);
        private readonly Vector3 aimDownRotR = new Vector3(90f, 205f, 168f);
        private readonly float aimDownDeg = -30.0f;
        private readonly float aimUpDeg = 33.0f;
        private readonly Vector3 aimRotL_Dodge = new Vector3(70f, 150f, 165f);
        private readonly Vector3 aimRotR_Dodge = new Vector3(95f, 200f, 168f);
        private readonly Vector3 aimPosL = new Vector3(0.16f, 0.031f, 0.01f);
        private readonly Vector3 aimPosR = new Vector3(0.15f, 0.041f, 0.01f);

        public static int Shootdodge;
        public static Type DodgeType;
        public static FieldInfo DodgeField;
        public static Weapon CharWpn;
        public static bool Conflict = false;
        public static bool Notified = false;
        public static float ikRecoil = 0f;

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
            //new TextElement("Debug " + , new PointF(50f, 78f), 0.5f).ScaledDraw();
            if (Game.IsLoading || Game.IsPaused) return;
            Utils.CheckConflict();
            Char = Game.Player.Character;
            CharWpn = Char.Weapons.Current;
            CheckController();

            if (CharWpn.IsPresent && !DualWielding && CharWpn.Group != WeaponGroup.Unarmed)
                Char.Weapons.CurrentWeaponObject.IsVisible = true;
            if (GunSwap)
            {
                if (!WpnOff.Contains(CharWpn.Group))
                {
                    StartDualWield();
                    GunSwap = false;
                }
            }

            if (!DualWielding)
                return;
            if (!Char.IsInVehicle())
            {
                GunL = ShooterL.Weapons.CurrentWeaponObject;
                GunR = ShooterR.Weapons.CurrentWeaponObject;
                ShooterL.PositionNoOffset = Char.Position + new Vector3(0f, 0f, 1000f);
                ShooterR.PositionNoOffset = Char.Position + new Vector3(0f, 0f, 1000f);
                ShooterL.Weapons.Current.InfiniteAmmoClip = true;
                ShooterR.Weapons.Current.InfiniteAmmoClip = true;

                if (Char.IsAiming)
                    Game.DisableControlThisFrame(GTA.Control.Jump);

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

                if (Char.IsAiming && !Char.IsReloading && !Char.IsFalling && Shootdodge == 0 && !AimDown && !AimUp)
                {
                    if (!Char.IsPlayingAnimation(turretAnim))
                        Char.Task.PlayAnimation(turretAnim, AnimationBlendDelta.SlowBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0f);
                }
                else if (Char.IsPlayingAnimation(turretAnim))
                    Char.Task.StopScriptedAnimationTask(turretAnim, AnimationBlendDelta.SlowBlendOut);

                if (Shootdodge != 0)
                {
                    if (Char.IsPlayingAnimation(turretAnim))
                        Char.Task.StopScriptedAnimationTask(turretAnim);
                    if (!Char.IsPlayingAnimation(dodgeAnim))
                        Char.Task.PlayAnimation(dodgeAnim, AnimationBlendDelta.NormalBlendIn, AnimationBlendDelta.NormalBlendOut, -1, (AnimationFlags)49, 0f);
                }
                else if (Char.IsPlayingAnimation(dodgeAnim))
                    Char.Task.StopScriptedAnimationTask(dodgeAnim);

                if (Shootdodge == 0 && Config.pitchAnims)
                {
                    if (!AimDown && GameplayCamera.RelativePitch < aimDownDeg && Char.IsPlayingAnimation(turretAnim))
                    {
                        if (!Char.IsPlayingAnimation(aimDown))
                        {
                            Char.Task.PlayAnimation(aimDown, AnimationBlendDelta.WalkBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0f);
                            GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimDownRotL, false, false, false, true, default);
                            GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimDownRotR, false, false, false, true, default);
                        }
                        AimDown = true;
                    }
                    if (AimDown && (GameplayCamera.RelativePitch >= aimDownDeg || !Char.IsAiming))
                    {
                        if (Char.IsPlayingAnimation(aimDown))
                            Char.Task.StopScriptedAnimationTask(aimDown, AnimationBlendDelta.WalkBlendOut);
                        GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL, false, false, false, true, default);
                        GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR, false, false, false, true, default);
                        AimDown = false;
                    }

                    if (!AimUp && GameplayCamera.RelativePitch > aimUpDeg && Char.IsPlayingAnimation(turretAnim))
                    {
                        if (!Char.IsPlayingAnimation(aimUp))
                        {
                            Char.Task.PlayAnimation(aimUp, AnimationBlendDelta.WalkBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0.07f);
                            GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimUpRotL, false, false, false, true, default);
                            GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimUpRotR, false, false, false, true, default);
                        }
                        AimUp = true;
                    }
                    if (Char.GetAnimationCurrentTime(aimUp) > 0.08f)
                        Char.SetAnimationSpeed(aimUp, 0f);
                    if (AimUp && (GameplayCamera.RelativePitch <= aimUpDeg || !Char.IsAiming))
                    {
                        if (Char.IsPlayingAnimation(aimUp))
                            Char.Task.StopScriptedAnimationTask(aimUp, AnimationBlendDelta.WalkBlendOut);
                        GunL.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL, false, false, false, true, default);
                        GunR.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR, false, false, false, true, default);
                        AimUp = false;
                    }
                }

                if (Char.IsAiming && !Char.IsReloading && !Char.IsJumping && GameplayCamera.FollowPedCamViewMode != CamViewMode.FirstPerson)
                {
                    Utils.ArmIK(true);
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
                else Utils.ArmIK(false);

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

                if (Shootdodge == 0)
                {
                    if ((Game.IsControlJustPressed(GTA.Control.Reload) || bothMags == 0) && !imReloading && bothMags != oneMag * 2)
                        Reload1();
                    if (imReloading && !GunL.IsVisible && !Char.IsReloading)
                        Reload2();
                }
                else if (bothMags == 0)
                    EndDualWield();
                if (!imReloading && !Char.IsReloading && !GunL.IsVisible && !GunR.IsVisible)
                {
                    Utils.ShowPlayerWpn(false);
                    GunL.IsVisible = true;
                    GunR.IsVisible = true;
                }

                if (Char.IsShooting && !Char.IsReloading && bothMags >= 1)
                {
                    GameplayCamera.Shake(CameraShake.Hand, 10f);
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
                            GameplayCamera.RelativeHeading += Config.recoil;
                            GameplayCamera.RelativePitch += Config.recoil;
                        }
                        else
                        {
                            shootCycle = 0;
                            --bothMags;
                            GameplayCamera.RelativeHeading -= Config.recoil;
                            GameplayCamera.RelativePitch += Config.recoil;
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
                            GameplayCamera.RelativeHeading += Config.recoil;
                            GameplayCamera.RelativePitch += Config.recoil;
                        }
                        else
                        {
                            shootCycle = 0;
                            --bothMags;
                            GameplayCamera.RelativeHeading -= Config.recoil;
                            GameplayCamera.RelativePitch += Config.recoil;
                        }
                        gameTimer = Game.GameTime + 75;
                    }
                }
                else if (GameplayCamera.IsShaking) GameplayCamera.StopShaking();
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

            if (Function.Call<bool>(Hash.IS_PED_SWITCHING_WEAPON, Char))
            {
                EndDualWield();
                GunSwap = true;
            }

            if (Char.IsRagdoll)
                EndDualWield();

            if (Char.IsAiming || imReloading)
                DisplayHud();

            if ((Char.IsGettingIntoVehicle || !Char.IsAlive || Char.IsSwimming || CharWpn.Ammo == 0 || WpnOff.Contains(CharWpn.Group)) && !GunSwap)
                EndDualWield();
        }

        private void StartDualWield()
        {
            if (DualWielding)
                return;
            else if (!WpnOff.Contains(CharWpn.Group) && CharWpn.Ammo > 0 && !Char.IsInVehicle() &&
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
                ShooterL = CreateFakeShooter("L");
                ShooterR = CreateFakeShooter("R");
                CharWpn.InfiniteAmmoClip = true;
                bothMags = oneMag * 2;
                Char.Accuracy = 0;
                Char.SetWeaponMovementClipSet(wpnAnim);
                DualWielding = true;
            }
        }

        private Ped CreateFakeShooter(string LR)
        {
            Ped ped = World.CreatePed((Model)PedHash.Famdnf01GMY, Char.Position);
            ped.IsVisible = false;
            Function.Call(Hash.SET_ENTITY_PROOFS, ped, true, true, true, true, true, true, true, true);
            Function.Call(Hash.STOP_PED_SPEAKING, ped, true);
            ped.Weapons.Give(CharWpn.Hash, 400, true, true);
            Utils.GetAttachments(ped);
            ped.Weapons.CurrentWeaponObject.IsVisible = true;
            ped.Weapons.Select(ped.Weapons.BestWeapon, true);
            ped.RelationshipGroup = Char.RelationshipGroup;
            ped.CanRagdoll = false;
            ped.IsCollisionEnabled = false;
            ped.IsPositionFrozen = true;
            ped.BlockPermanentEvents = true;
            ped.Accuracy = 0;
            ped.Weapons.CurrentWeaponObject.Detach();
            if (LR == "L")
                ped.Weapons.CurrentWeaponObject.AttachTo(Char.Bones[Bone.SkelLeftHand], aimPosL, aimRotL, false, false, false, true, default);
            else ped.Weapons.CurrentWeaponObject.AttachTo(Char.Bones[Bone.SkelRightHand], aimPosR, aimRotR, false, false, false, true, default);
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
            Utils.ArmIK(false);
            Char.ResetWeaponMovementClipSet();
            Char.Task.StopScriptedAnimationTask(turretAnim, AnimationBlendDelta.VerySlowBlendOut);
            ShooterL.MarkAsNoLongerNeeded();
            ShooterR.MarkAsNoLongerNeeded();
            ShooterL.Delete();
            ShooterR.Delete();
            Char.Accuracy = accuracy;
            CharWpn.InfiniteAmmoClip = false;
            CharWpn.Ammo += bothMags / 2;
            CharWpn.AmmoInClip = bothMags / 2;
            Utils.PlayerDamage(1f);
            Utils.ShowPlayerWpn(true);
            shootCycle = 0;
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
            if (Game.LastInputMethod == InputMethod.GamePad && Char.IsAiming && Game.IsControlJustPressed(GTA.Control.PhoneRight))
            {
                if (!DualWielding)
                    StartDualWield();
                else
                    EndOnPressed();
            }
        }
    }
}
