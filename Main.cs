using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
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
        private bool AimUp_Micro1 = false;
        private bool AimUp_Micro2 = false;
        private bool AimUp_Micro3 = false;
        private bool AimUp_Micro4 = false;
        private bool AimUp_Micro5 = false;
        private bool AimUp_Micro6 = false;
        private bool AimUp_Micro7 = false;
        private bool AimUp_Micro8 = false;
        private bool AimUp_Micro9 = false;
        private bool AimUp_Micro10 = false;
        private bool AimUp_Micro11 = false;
        private bool AimDown_Micro1 = false;
        private bool AimDown_Micro2 = false;
        private bool AimDown_Micro3 = false;
        private bool AimDown_Micro4 = false;
        private bool AimDown_Micro5 = false;
        private bool AimDown_Micro6 = false;
        private bool AimDown_Micro7 = false;
        private int shootCycle = 0;
        private int oneMag;
        private int bothMags;
        private int accuracy;
        private Ped ShooterL;
        private Ped ShooterR;
        private Entity GunL;
        private Entity GunR;
        public static Ped MC = Game.Player.Character;
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
        private Vector3 aimPosL = new Vector3(0.17f, 0.021f, 0.01f);
        private readonly Vector3 aimRotL = new Vector3(70f, 180f, 165f);
        public static Vector3 aimPosR = new Vector3(0.17f, 0.031f, 0f);
        public static readonly Vector3 aimRotR = new Vector3(100f, 195f, 168f);
        private readonly Vector3 aimUpRotL = new Vector3(70f, 175f, 165f);
        private readonly Vector3 aimUpRotR = new Vector3(110f, 175f, 165f);
        private readonly Vector3 aimDownRotL = new Vector3(90f, 155f, 175f);
        private readonly Vector3 aimDownRotR = new Vector3(85f, 200f, 165f);
        private readonly Vector3 aimRotL_Dodge = new Vector3(70f, 150f, 165f);
        private readonly Vector3 aimRotR_Dodge = new Vector3(95f, 200f, 168f);
        private Vector3 aimPosL_gunAdj = Vector3.Zero;
        private Vector3 aimPosR_gunAdj = Vector3.Zero;
        private readonly float aimDownDeg = -45.0f;
        private readonly float aimUpDeg = 36.0f;
        public static int Shootdodge;
        public static Type DodgeType;
        public static FieldInfo DodgeField;
        public static Weapon MC_Wpn;
        private Weapon lastWpn;
        public static bool Conflict = false;
        public static bool Notified = false;
        public static float ikRecoil = 1f;

        private float padButtonTimer = 0f;
        private float padButtonTimer2 = 0f;

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
            if (Game.IsCutsceneActive || Game.IsPaused) return;
            Utils.CheckConflict();
            MC = Game.Player.Character;
            MC_Wpn = MC.Weapons.Current;
            CheckController();
            // new TextElement("Debug" + , new PointF(50f, 38f), 0.5f).ScaledDraw();

            if (MC_Wpn.IsPresent && !DualWielding && !WpnOff.Contains(MC_Wpn.Group))
                MC.Weapons.CurrentWeaponObject.IsVisible = true;

            if (!DualWielding)
                return;

            if (!MC.IsInVehicle() && !WpnOff.Contains(MC_Wpn.Group))
            {
                GunL = ShooterL.Weapons.CurrentWeaponObject;
                GunR = ShooterR.Weapons.CurrentWeaponObject;
                ShooterL.PositionNoOffset = MC.Position + new Vector3(0f, 0f, 1000f);
                ShooterR.PositionNoOffset = MC.Position + new Vector3(0f, 0f, 1000f);
                ShooterL.Weapons.Current.InfiniteAmmoClip = true;
                ShooterR.Weapons.Current.InfiniteAmmoClip = true;
                AdjustShotguns();

                //Disable Roll & No Changing Weapon On Reload
                if (MC.IsAiming)
                    Game.DisableControlThisFrame(GTA.Control.Jump);
                if (MC.IsReloading)
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);
                    Game.DisableControlThisFrame(GTA.Control.SelectWeapon); Game.DisableControlThisFrame(GTA.Control.SelectPrevWeapon); Game.DisableControlThisFrame(GTA.Control.SelectNextWeapon);
                    Hud.HideComponentThisFrame(HudComponent.WeaponWheel);
                }
                //In-Cover & Jumping Pose
                if (!MC.IsAiming && MC.IsJumping)
                {
                    if (!MC.IsPlayingAnimation(handAnim))
                        MC.Task.PlayAnimation(handAnim, AnimationBlendDelta.InstantBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)48, 0f);
                }
                else if ((MC.IsInCover || MC.IsGoingIntoCover) && !Function.Call<bool>(Hash.IS_PED_SWITCHING_WEAPON, MC) && !MC.IsAiming)
                {
                    Utils.SetIK(false);
                    if (!MC.IsPlayingAnimation(handAnim))
                        MC.Task.PlayAnimation(handAnim, AnimationBlendDelta.VerySlowBlendIn, AnimationBlendDelta.InstantBlendOut, -1, (AnimationFlags)48, 0f);
                    else if (MC.GetAnimationCurrentTime(handAnim) > 0.2f)
                        MC.SetAnimationSpeed(handAnim, 0f);
                }
                else if (!MC.IsJumping && MC.IsPlayingAnimation(handAnim))
                    MC.Task.StopScriptedAnimationTask(handAnim, AnimationBlendDelta.SlowBlendOut);
                //Normal Aiming Pose
                if (MC.IsAiming && !MC.IsReloading && !MC.IsFalling && Shootdodge == 0 && !AimDown && !AimUp)
                {
                    if (!MC.IsPlayingAnimation(turretAnim))
                        MC.Task.PlayAnimation(turretAnim, AnimationBlendDelta.SlowBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0f);
                }
                else if (MC.IsPlayingAnimation(turretAnim))
                    MC.Task.StopScriptedAnimationTask(turretAnim, AnimationBlendDelta.SlowBlendOut);
                //Weapon Pitch Trickery
                if (Shootdodge == 0 && Config.pitchAnims)
                {
                    float pitch = GameplayCamera.RelativePitch;
                    //AimDown_Pose
                    if (!AimDown && pitch < aimDownDeg && MC.IsPlayingAnimation(turretAnim))
                    {
                        if (!MC.IsPlayingAnimation(aimDown))
                        {
                            MC.Task.PlayAnimation(aimDown, AnimationBlendDelta.VerySlowBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0f);
                            GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimDownRotL, false, false, false, true, default);
                            GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimDownRotR, false, false, false, true, default);
                        }
                        AimDown = true;
                        if (AimDown_Micro7)
                            AimDown_Micro7 = false;
                    }
                    if (AimDown && (pitch >= aimDownDeg || !MC.IsAiming))
                    {
                        if (MC.IsPlayingAnimation(aimDown))
                            MC.Task.StopScriptedAnimationTask(aimDown, AnimationBlendDelta.SlowBlendOut);
                        if (!AimDown_Micro1 || !AimDown_Micro2 || !AimDown_Micro3 || !AimDown_Micro4 || !AimDown_Micro5 || !AimDown_Micro6 || !AimDown_Micro7)
                        {
                            GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimRotL, false, false, false, true, default);
                            GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimRotR, false, false, false, true, default);
                        }
                        AimDown = false;
                    }
                    //AimUp_Pose
                    if (!AimUp && pitch > aimUpDeg && MC.IsPlayingAnimation(turretAnim))
                    {
                        if (!MC.IsPlayingAnimation(aimUp))
                        {
                            MC.Task.PlayAnimation(aimUp, AnimationBlendDelta.VerySlowBlendIn, AnimationBlendDelta.SlowBlendOut, -1, (AnimationFlags)50, 0.07f);
                            GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimUpRotL, false, false, false, true, default);
                            GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimUpRotR, false, false, false, true, default);
                        }
                        AimUp = true;
                        if (AimUp_Micro11)
                            AimUp_Micro11 = false;
                    }
                    if (MC.GetAnimationCurrentTime(aimUp) > 0.08f)
                        MC.SetAnimationSpeed(aimUp, 0f);
                    if (AimUp && (pitch <= aimUpDeg || !MC.IsAiming))
                    {
                        if (MC.IsPlayingAnimation(aimUp))
                            MC.Task.StopScriptedAnimationTask(aimUp, AnimationBlendDelta.SlowBlendOut);
                        if (!AimUp_Micro1 || !AimUp_Micro2 || !AimUp_Micro3 || !AimUp_Micro4 || !AimUp_Micro5 || !AimUp_Micro6 || !AimUp_Micro7 || !AimUp_Micro8 || !AimUp_Micro9 || !AimUp_Micro10 || !AimUp_Micro11)
                        {
                            GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimRotL, false, false, false, true, default);
                            GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimRotR, false, false, false, true, default);
                        }
                        AimUp = false;
                    }


                    //Semi_AimUp
                    float aimUpDeg_1 = aimUpDeg - 33f;
                    float aimUpDeg_2 = aimUpDeg - 30f;
                    float aimUpDeg_3 = aimUpDeg - 27f;
                    float aimUpDeg_4 = aimUpDeg - 24f;
                    float aimUpDeg_5 = aimUpDeg - 21f;
                    float aimUpDeg_6 = aimUpDeg - 18f;
                    float aimUpDeg_7 = aimUpDeg - 15f;
                    float aimUpDeg_8 = aimUpDeg - 12f;
                    float aimUpDeg_9 = aimUpDeg - 9f;
                    float aimUpDeg_10 = aimUpDeg - 6;
                    float aimUpDeg_11 = aimUpDeg - 3f;

                    Vector3 posUp_Adj = new Vector3 (0f, 0.020f,0f);

                    if (!AimUp_Micro1 && pitch > aimUpDeg_1 && pitch < aimUpDeg_2 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 4f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 0.25f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 0.25f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro1 = true;
                        AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro2 && pitch > aimUpDeg_2 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 7.6f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 0.5f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 0.5f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro2 = true;
                        AimUp_Micro1 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro3 && pitch > aimUpDeg_3 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 11.2f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 0.75f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro3 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro4 && pitch > aimUpDeg_4 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 14.8f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 1.25f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 1.25f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro4 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro5 && pitch > aimUpDeg_5 && MC.IsPlayingAnimation(turretAnim) && !Utils.GunNeedAdjustment.Contains(MC_Wpn))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 18.4f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 1.45f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 1.45f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro5 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro6 && pitch > aimUpDeg_6 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 22f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 1.65f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 1.65f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro6 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro7 && pitch > aimUpDeg_7 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 25.6f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 1.85f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 1.85f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro7 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro8 && pitch > aimUpDeg_8 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 29.2f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 2f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 2f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro8 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro9 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro9 && pitch > aimUpDeg_9 && MC.IsPlayingAnimation(turretAnim) && !Utils.GunNeedAdjustment.Contains(MC_Wpn))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 32.8f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 2f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 2f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro9 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro10 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro10 && pitch > aimUpDeg_10 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 36.4f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 2.25f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 2.25f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro8 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro11 = false;
                    }
                    if (!AimUp_Micro11 && pitch > aimUpDeg_11 && MC.IsPlayingAnimation(turretAnim) && !Utils.GunNeedAdjustment.Contains(MC_Wpn))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 40f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + posUp_Adj * 2.5f, aimRotL + pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + posUp_Adj * 2.5f, aimRotR + pitchAdj, false, false, false, true, default);
                        AimUp_Micro9 = true;
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10 = false;
                    }

                    //Semi_AimDown
                    float aimDownDeg_1 = aimDownDeg + 42.5f;
                    float aimDownDeg_2 = aimDownDeg + 37.5f;
                    float aimDownDeg_3 = aimDownDeg + 32.5f;
                    float aimDownDeg_4 = aimDownDeg + 27.5f;
                    float aimDownDeg_5 = aimDownDeg + 22.5f;
                    float aimDownDeg_6 = aimDownDeg + 17.5f;
                    float aimDownDeg_7 = aimDownDeg + 12.5f;
                    if (!AimDown_Micro1 && pitch < aimDownDeg_1 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 5f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimRotL - pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimRotR - pitchAdj, false, false, false, true, default);
                        AimDown_Micro1 = true;
                        AimDown_Micro2 = AimDown_Micro3 = AimDown_Micro4 = AimDown_Micro5 = AimDown_Micro6 = AimDown_Micro7 = false;
                    }
                    if (!AimDown_Micro2 && pitch < aimDownDeg_2 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 10f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + new Vector3(0f, -0.005f, 0f), aimRotL - pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + new Vector3(0f, -0.005f, 0f), aimRotR - pitchAdj, false, false, false, true, default);
                        AimDown_Micro2 = true;
                        AimDown_Micro1 = AimDown_Micro3 = AimDown_Micro4 = AimDown_Micro5 = AimDown_Micro6 = AimDown_Micro7 = false;
                    }
                    if (!AimDown_Micro3 && pitch < aimDownDeg_3 && MC.IsPlayingAnimation(turretAnim) && !Utils.GunNeedAdjustment.Contains(MC_Wpn))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 15f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + new Vector3(0f, -0.005f, 0f), aimRotL - pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + new Vector3(0f, -0.015f, 0f), aimRotR - pitchAdj, false, false, false, true, default);
                        AimDown_Micro3 = true;
                        AimDown_Micro1 = AimDown_Micro2 = AimDown_Micro4 = AimDown_Micro5 = AimDown_Micro6 = AimDown_Micro7 = false;
                    }
                    if (!AimDown_Micro4 && pitch < aimDownDeg_4 && MC.IsPlayingAnimation(turretAnim))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 20f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + new Vector3(0f, -0.015f, 0f), aimRotL - pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + new Vector3(0f, -0.025f, 0f), aimRotR - pitchAdj, false, false, false, true, default);
                        AimDown_Micro4 = true;
                        AimDown_Micro1 = AimDown_Micro2 = AimDown_Micro3 = AimDown_Micro5 = AimDown_Micro6 = AimDown_Micro7 = false;
                    }
                    if (!AimDown_Micro5 && pitch < aimDownDeg_5 && MC.IsPlayingAnimation(turretAnim) && !Utils.GunNeedAdjustment.Contains(MC_Wpn))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 25f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + new Vector3(0f, -0.015f, 0f), aimRotL - pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + new Vector3(0f, -0.025f, 0f), aimRotR - pitchAdj, false, false, false, true, default);
                        AimDown_Micro5 = true;
                        AimDown_Micro1 = AimDown_Micro2 = AimDown_Micro3 = AimDown_Micro4 = AimDown_Micro6 = AimDown_Micro7 = false;
                    }
                    if (!AimDown_Micro6 && pitch < aimDownDeg_6 && MC.IsPlayingAnimation(turretAnim) && !Utils.GunNeedAdjustment.Contains(MC_Wpn))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 30f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + new Vector3(0f, -0.025f, 0f), aimRotL - pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + new Vector3(0f, -0.035f, 0f), aimRotR - pitchAdj, false, false, false, true, default);
                        AimDown_Micro6 = true;
                        AimDown_Micro1 = AimDown_Micro2 = AimDown_Micro3 = AimDown_Micro4 = AimDown_Micro5 = AimDown_Micro7 = false;
                    }
                    if (!AimDown_Micro7 && pitch < aimDownDeg_7 && MC.IsPlayingAnimation(turretAnim) && !Utils.GunNeedAdjustment.Contains(MC_Wpn))
                    {
                        Vector3 pitchAdj = new Vector3(0f, 0f, 35f);
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj + new Vector3(0f, -0.025f, 0f), aimRotL - pitchAdj, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj + new Vector3(0f, -0.035f, 0f), aimRotR - pitchAdj, false, false, false, true, default);
                        AimDown_Micro7 = true;
                        AimDown_Micro1 = AimDown_Micro2 = AimDown_Micro3 = AimDown_Micro4 = AimDown_Micro5 = AimDown_Micro6 = false;
                    }

                    //SemiAimExit
                    if (!MC.IsAiming || (AimUp_Micro1 && pitch <= aimUpDeg_1) || (AimDown_Micro1 && pitch >= aimDownDeg_1))
                    {
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimRotL, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimRotR, false, false, false, true, default);
                        AimUp_Micro1 = AimUp_Micro2 = AimUp_Micro3 = AimUp_Micro4 = AimUp_Micro5 = AimUp_Micro6 = AimUp_Micro7 = AimUp_Micro8 = AimUp_Micro9 = AimUp_Micro10  = AimUp_Micro11 = false;
                        AimDown_Micro1 = AimDown_Micro2 = AimDown_Micro3 = AimDown_Micro4 = AimDown_Micro5 = AimDown_Micro6 = AimDown_Micro7 = false;
                    }
                }



                //SetIK
                if (MC.IsAiming && !MC.IsReloading && !MC.IsJumping && GameplayCamera.FollowPedCamViewMode != CamViewMode.FirstPerson)
                {
                    Utils.SetIK(true);
                    float camRotZ = GameplayCamera.Rotation.Z;
                    if (Shootdodge > 0)
                    {
                        if (!GunMoved)
                        {
                            GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimRotL_Dodge, false, false, false, true, default);
                            GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimRotR_Dodge, false, false, false, true, default);
                        }
                        GameplayCamera.SetThirdPersonCameraRelativeHeadingLimitsThisUpdate(-23.5f, 23.5f);
                        GunMoved = true;
                        GunReset = false;
                    }
                    else if (Shootdodge == 0)
                        MC.Heading = camRotZ;
                    if (Shootdodge == 0)
                        Utils.SetIkTarget(MC);
                }
                //Weapon change when aiming in FirstPersonView broke the script. Fuck It!
                if (MC.IsAiming && GameplayCamera.FollowPedCamViewMode == CamViewMode.FirstPerson)
                {
                    Utils.SetIK(false);
                    Game.DisableControlThisFrame(GTA.Control.SelectWeapon); Game.DisableControlThisFrame(GTA.Control.SelectPrevWeapon); Game.DisableControlThisFrame(GTA.Control.SelectNextWeapon);
                    Hud.HideComponentThisFrame(HudComponent.WeaponWheel);
                }
                //ShootdodgeSequence
                if (Shootdodge != 0)
                {
                    if (MC.IsPlayingAnimation(turretAnim))
                        MC.Task.StopScriptedAnimationTask(turretAnim);
                    if (!MC.IsPlayingAnimation(dodgeAnim))
                        MC.Task.PlayAnimation(dodgeAnim, AnimationBlendDelta.NormalBlendIn, AnimationBlendDelta.NormalBlendOut, -1, (AnimationFlags)49, 0f);
                }
                else if (MC.IsPlayingAnimation(dodgeAnim))
                    MC.Task.StopScriptedAnimationTask(dodgeAnim);
                //Revert Gun Pos If No Shootdodge
                if (Shootdodge == 0)
                {
                    if (GunMoved)
                        GunMoved = false;
                    if (!GunMoved && !GunReset)
                    {
                        GunL.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimRotL, false, false, false, true, default);
                        GunR.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimRotR, false, false, false, true, default);
                        GunReset = true;
                    }
                }
                //ReloadSequence
                if (Shootdodge == 0 && !MC.IsRagdoll)
                {
                    if ((Game.IsControlJustPressed(GTA.Control.Reload) || bothMags == 0) && !imReloading && bothMags != oneMag * 2)
                        Reload1();
                    if (imReloading && !GunL.IsVisible && !MC.IsReloading)
                        Reload2();
                }
                else
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);
                    if (bothMags == 0)
                    {
                        MC_Wpn.Ammo -= oneMag;
                        MC_Wpn.AmmoInClip = 0;
                        Game.TimeScale = 1f;
                        Game.Player.DisableFiringThisFrame();
                    }
                }
                //Prevent Normal Reload
                if (MC_Wpn.AmmoInClip < MC_Wpn.MaxAmmoInClip)
                {
                    Game.DisableControlThisFrame(GTA.Control.Reload);
                    if (Game.IsControlJustPressed(GTA.Control.Reload))
                        MC_Wpn.AmmoInClip = MC_Wpn.MaxAmmoInClip;
                }
                //ShowWeapon    
                if (!imReloading && !MC.IsReloading)
                {
                    if (!GunL.IsVisible && !GunR.IsVisible)
                    {
                        Utils.ShowPlayerWpn(false);
                        GunL.IsVisible = true;
                        GunR.IsVisible = true;
                    }
                    else if (MC.Weapons.CurrentWeaponObject.IsVisible)
                        Utils.ShowPlayerWpn(false);
                }
                if (Game.IsEnabledControlJustPressed(GTA.Control.Attack))
                    Game.Player.DisableFiringThisFrame();
                //ShootingSequence
                if (MC.IsShooting && !MC.IsReloading && bothMags >= 1)
                {
                    MC.Weapons.CurrentWeaponObject.RemoveParticleEffects();
                    Utils.SortPtfx();
                    Utils.surpressed = MC_Wpn.Components.GetSuppressorComponent().Active;
                    if (WpnFilter.Contains(MC_Wpn.Group))
                    {
                        Utils.PlayerDamage(0.9f);
                        if (shootCycle == 0)
                            Utils.ShootAt(ShooterL, GunL);
                        else
                            Utils.ShootAt(ShooterR, GunR);
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
                    else if (WpnFilter2.Contains(MC_Wpn.Group) && gameTimer <= 0)
                    {
                        Utils.PlayerDamage(0.7f);
                        if (shootCycle == 0)
                            Utils.ShootAt(ShooterL, GunL);
                        else
                            Utils.ShootAt(ShooterR, GunR);
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
                oneMag = MC_Wpn.AmmoInClip;
                if (Game.GameTime >= gameTimer && WpnFilter2.Contains(MC_Wpn.Group))
                    gameTimer = 0;
            }

            foreach (Ped allPed in World.GetAllPeds())
            {
                if (allPed.IsInCombatAgainst(ShooterL) || allPed.IsInCombatAgainst(ShooterR))
                {
                    allPed.Task.ClearAll();
                    allPed.Task.Combat(MC);
                }
            }
            //WeaponSwitching
            if (Game.IsControlJustPressed(GTA.Control.SelectWeapon))
                lastWpn = MC_Wpn;
            if (Function.Call<bool>(Hash.IS_PED_SWITCHING_WEAPON, MC))
            {
                GunSwap = true;
                EndDualWield();
            }
            if (GunSwap && !DualWielding && !WpnOff.Contains(MC_Wpn.Group))
            {
                GunSwap = false;
                StartDualWield();
            }

            if (MC.IsAiming || imReloading)
                DisplayHud();

            if ((MC.IsGettingIntoVehicle || !MC.IsAlive || MC.IsInWater || (MC_Wpn.Ammo - MC_Wpn.AmmoInClip <= MC_Wpn.MaxAmmoInClip && MC.IsReloading) || WpnOff.Contains(MC_Wpn.Group)) && !GunSwap)
                EndDualWield();
        }

        private void StartDualWield()
        {
            if (DualWielding)
                return;
            else if (!WpnOff.Contains(MC_Wpn.Group) && MC_Wpn.Ammo - MC_Wpn.AmmoInClip >= MC_Wpn.MaxAmmoInClip && !MC.IsInVehicle() &&
                    !MC.IsRagdoll && MC.IsAlive && !MC.IsFalling && !MC.IsSwimming && Shootdodge == 0)
            {
                if (Conflict)
                    Notified = false;
                Utils.LoadClipSet(wpnAnim);
                wpnAnim.Request();
                accuracy = MC.Accuracy;
                oneMag = MC_Wpn.AmmoInClip;
                MC_Wpn.Ammo -= oneMag;
                imReloading = false;
                MC.Task.PlayAnimation("melee@holster", "unholster", 8f, -1, (AnimationFlags)48);
                Utils.ShowPlayerWpn(false);
                if (MC.GetAnimationCurrentTime(new CrClipAsset("melee@holster", "unholster")) < 1.0f)
                    Yield();
                ShooterL = CreateFakeShooter(1);
                ShooterR = CreateFakeShooter(2);
                MC_Wpn.InfiniteAmmoClip = true;
                bothMags = oneMag * 2;
                MC.Accuracy = 0;
                MC.SetWeaponMovementClipSet(wpnAnim);
                lastWpn = MC_Wpn;
                AimDown = false;
                AimUp = false;
                DualWielding = true;
            }
        }

        private Ped CreateFakeShooter(int LR)
        {
            AdjustShotguns();
            Ped ped = World.CreatePed((Model)PedHash.Famdnf01GMY, MC.Position);
            ped.IsVisible = false;
            ped.Weapons.Give(MC_Wpn.Hash, 999, true, true);
            Utils.GetAttachments(ped);
            ped.Weapons.CurrentWeaponObject.IsVisible = false;
            ped.Weapons.Select(MC_Wpn.Hash, true);
            Function.Call(Hash.SET_ENTITY_PROOFS, ped, true, true, true, true, true, true, true, true);
            Function.Call(Hash.STOP_PED_SPEAKING, ped, true);
            ped.CanRagdoll = false;
            ped.IsCollisionEnabled = false;
            ped.IsPositionFrozen = true;
            ped.BlockPermanentEvents = true;
            ped.RelationshipGroup = MC.RelationshipGroup;
            ped.Accuracy = 0;
            ped.Weapons.CurrentWeaponObject.Detach();
            if (LR == 1)
            {
                ped.Weapons.CurrentWeaponObject.AttachTo(MC.Bones[Bone.SkelLeftHand], aimPosL + aimPosL_gunAdj, aimRotL, false, false, false, true, default);
            }
            else
            {
                ped.Weapons.CurrentWeaponObject.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + aimPosR_gunAdj, aimRotR, false, false, false, true, default);
            }
            ped.Weapons.CurrentWeaponObject.IsVisible = true;
            return ped;
        }

        private void Reload1()
        {
            Utils.SetIK(false);
            MC.BlocksAnyDamageButHasReactions = true;
            MC.Task.StopScriptedAnimationTask(handAnim, AnimationBlendDelta.InstantBlendOut);
            MC.ResetWeaponMovementClipSet();
            MC.Task.ReloadWeapon();
            GunL.IsVisible = false;
            GunR.IsVisible = false;
            Utils.ShowPlayerWpn(true);
            while (MC.IsReloading)
                Yield();
            MC.Task.PlayAnimation("weapon@w_sp_jerrycan", "holster", 8f, -1, (AnimationFlags)48);
            oneMag = MC_Wpn.AmmoInClip;
            if (MC.GetAnimationCurrentTime(new CrClipAsset("weapon@w_sp_jerrycan", "holster")) < 1.0f)
                Yield();
            if (bothMags != (oneMag * 2) - 1)
                imReloading = true;
            else
            {
                bothMags = oneMag * 2;
                MC_Wpn.Ammo -= 1;
                MC.SetWeaponMovementClipSet(wpnAnim);
            }
            MC.BlocksAnyDamageButHasReactions = false;
        }

        private void Reload2()
        {
            if (MC_Wpn.AmmoInClip >= MC_Wpn.MaxAmmoInClip)
                imReloading = false;
            Utils.SetIK(false);
            MC.BlocksAnyDamageButHasReactions = true;
            MC.Task.StopScriptedAnimationTask(handAnim, AnimationBlendDelta.InstantBlendOut);
            GunL.IsVisible = false;
            GunR.IsVisible = false;
            Utils.ShowPlayerWpn(true);
            MC.Task.PlayAnimation("melee@holster", "holster", 8f, -1, (AnimationFlags)48);
            if (MC.GetAnimationCurrentTime(new CrClipAsset("melee@holster", "holster")) < 1.0f)
                Yield();
            MC.Task.ReloadWeapon();
            while (MC.IsReloading)
                Yield();
            MC.Task.PlayAnimation("weapon@w_sp_jerrycan", "unholster", 8f, -1, (AnimationFlags)48);
            if (MC.GetAnimationCurrentTime(new CrClipAsset("weapon@w_sp_jerrycan", "unholster")) < 1.0f)
                Yield();
            MC.Task.PlayAnimation("weapon@w_sp_jerrycan", "holster_2_aim", 8f, -1, (AnimationFlags)48);
            if (MC.GetAnimationCurrentTime(new CrClipAsset("weapon@w_sp_jerrycan", "holster_2_aim")) < 1.0f)
                Yield();
            if (bothMags != 0)
            {
                int ammoUsed = (oneMag * 2) - bothMags;
                MC_Wpn.Ammo -= ammoUsed;
                bothMags = oneMag * 2;
            }
            else
            {
                bothMags = oneMag * 2;
                MC_Wpn.Ammo -= bothMags;
            }
            imReloading = false;
            MC.BlocksAnyDamageButHasReactions = false;
            MC.SetWeaponMovementClipSet(wpnAnim);
        }

        private void EndDualWield()
        {
            Utils.SetIK(false);
            MC.ResetWeaponMovementClipSet();
            MC.Task.StopScriptedAnimationTask(turretAnim, AnimationBlendDelta.VerySlowBlendOut);
            ShooterL.MarkAsNoLongerNeeded();
            ShooterR.MarkAsNoLongerNeeded();
            ShooterL.Delete();
            ShooterR.Delete();
            MC.Accuracy = accuracy;
            if (!GunSwap) // Stop Ammo Loss when Weapon Changed
            {
                MC_Wpn.InfiniteAmmoClip = false;
                MC_Wpn.Ammo += bothMags / 2;
                MC_Wpn.AmmoInClip = bothMags / 2;
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
                MC.Task.PlayAnimation("melee@holster", "holster", 8f, -1, (AnimationFlags)48);
                if (MC.GetAnimationCurrentTime(new CrClipAsset("melee@holster", "holster")) < 1.0f)
                    Yield();
                EndDualWield();
            }
        }

        private void DisplayHud()
        {
            new TextElement("" + bothMags, new PointF(1255f, 60f), 0.52f, Color.Red, GTA.UI.Font.Pricedown, Alignment.Center, false, true).ScaledDraw();
            new TextElement("Dual", new PointF(1210f, 61f), 0.25f, Color.Red, GTA.UI.Font.RockstarTag, Alignment.Left, false, true).ScaledDraw();
            new TextElement("Wield", new PointF(1210f, 69f), 0.25f, Color.Red, GTA.UI.Font.RockstarTag, Alignment.Left, false, true).ScaledDraw();
        }

        private void AdjustShotguns()
        {
            if (Utils.GunNeedAdjustment.Contains(MC_Wpn))
            {
                aimPosL_gunAdj = new Vector3(-0.015f, 0.030f, -0.015f);
                aimPosR_gunAdj = new Vector3(-0.035f, 0.025f, 0f);
            }
            else
            {
                aimPosL_gunAdj = Vector3.Zero;
                aimPosR_gunAdj = Vector3.Zero;
            }
        }

        private void CheckController()
        {
            if (Game.LastInputMethod == InputMethod.GamePad && MC.IsAiming)
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
