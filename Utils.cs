using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.IO;
using System.Reflection;
using System.Xml;
using static DualWield.Main;

namespace DualWield
{
    public static class Utils
    {
        private static string muzzleFx;
        public static bool surpressed = false;
        public static Vector3 recoilOffset = Vector3.Zero;
        public static void SetIkTarget(Ped ped)
        {
            Vector3 target = GameplayCamera.Position + (GameplayCamera.ForwardVector * 9999f);
            Function.Call(Hash.SET_IK_TARGET, ped, 3, null, -1, target.X, target.Y, target.Z, 0, -8, 8);
            Function.Call(Hash.SET_IK_TARGET, ped, 4, null, -1, target.X, target.Y, target.Z, 0, -8, 8);
            Function.Call(Hash.SET_IK_TARGET, ped, 1, null, -1, target.X, target.Y, target.Z, 0, 0, 0);
            Function.Call(Hash.SET_IK_TARGET, ped, 2, null, -1, target.X, target.Y, target.Z, 0, 0, 0);
        }

        public static void SortPtfx()
        {
            if (!surpressed)
            {
                if (MC_Wpn.Group == WeaponGroup.MG || MC_Wpn.Group == WeaponGroup.AssaultRifle)
                    muzzleFx = "muz_assault_rifle";
                else if (MC_Wpn.Group == WeaponGroup.Pistol)
                    muzzleFx = "muz_pistol";
                else if (MC_Wpn.Group == WeaponGroup.SMG)
                    muzzleFx = "muz_smg";
                else
                    muzzleFx = "muz_shotgun";
            }
            else muzzleFx = "muz_pistol_silencer";
        }

        public static void ShootAt(Ped ped, Entity gun)
        {
            Vector3 crosshair = GameplayCamera.Position + (GameplayCamera.ForwardVector * 9999f);
            Function.Call(Hash.SET_PED_SHOOTS_AT_COORD, ped, crosshair.X, crosshair.Y, crosshair.Z, false);
            World.CreateParticleEffectNonLooped(new ParticleEffectAsset("core"), muzzleFx,
                Function.Call<Vector3>(Hash.GET_ENTITY_BONE_POSTION, gun, Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, gun, "gun_muzzle")), gun.Rotation);
        }
        public static void SetIK(bool on_off)
        {
            Function.Call(Hash.SET_PED_CAN_ARM_IK, MC, on_off);
            Function.Call(Hash.SET_PED_CAN_HEAD_IK, MC, on_off);
            Function.Call(Hash.SET_PED_CAN_TORSO_IK, MC, on_off);
        }

        public static void ShowPlayerWpn(bool shown)
        {
            Vector3 aimPos0 = new Vector3(-0.8f, 0f, 0f);
            if (!WpnOff.Contains(MC_Wpn.Group))
            {
                float WpnLength = MC_Wpn.Model.Dimensions.frontTopRight.X - MC_Wpn.Model.Dimensions.rearBottomLeft.X;
                bool longWpn = WpnLength > 0.44f;
                Vector3 posAdj;
                Vector3 rotAdj;
                if (longWpn)
                {
                    posAdj = new Vector3(0f, 0f, 0.01f);
                    rotAdj = new Vector3(10f, -5f, 0f);
                }
                else
                {
                    posAdj = new Vector3(0f, 0f, -0.01f);
                    rotAdj = new Vector3(5f, -15f, 7f);
                }
                if (!shown)
                    MC.Weapons.CurrentWeaponObject.AttachTo(MC.Bones[Bone.SkelSpine0], aimPos0, Vector3.Zero, false, false, false, true, default);
                else
                    MC.Weapons.CurrentWeaponObject.AttachTo(MC.Bones[Bone.SkelRightHand], aimPosR + posAdj, aimRotR + rotAdj, false, false, false, true, default);
                Function.Call(Hash.SET_PED_CURRENT_WEAPON_VISIBLE, MC, shown, false, false, false);
                MC.Weapons.CurrentWeaponObject.IsVisible = shown;
            }
        }

        public static void PlayerDamage(float damage)
        {
            Function.Call(Hash.SET_PLAYER_WEAPON_DAMAGE_MODIFIER, Game.Player, damage);
        }

        public static void LoadClipSet(ClipSet clipset)
        {
            if (!clipset.IsLoaded)
                clipset.Request();
        }

        public static void GetAttachments(Ped target)
        {
            Entity playerWpnObj = MC.Weapons.CurrentWeaponObject;
            Entity targetWpnObj = target.Weapons.CurrentWeaponObject;
            WeaponHash targetWpn = target.Weapons.Current;
            WeaponHash playerWpn = target.Weapons.Current.Hash;
            foreach (var component in WeaponComponent.GetAllHashes()) //Thanks to SHVDN NativeMemory
            {
                if (Function.Call<bool>(Hash.HAS_WEAPON_GOT_WEAPON_COMPONENT, playerWpnObj, component))
                {
                    Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, target, targetWpn, component);
                    int tintCompID = Function.Call<int>(Hash.GET_WEAPON_OBJECT_COMPONENT_TINT_INDEX, playerWpnObj, component);
                    Function.Call(Hash.SET_WEAPON_OBJECT_COMPONENT_TINT_INDEX, targetWpnObj, component, tintCompID);
                }
            }

            int tintID = Function.Call<int>(Hash.GET_PED_WEAPON_TINT_INDEX, MC, playerWpn);
            Function.Call(Hash.SET_PED_WEAPON_TINT_INDEX, target, targetWpn, tintID);
            int camoID = Function.Call<int>(Hash.GET_PED_WEAPON_CAMO_INDEX, MC, playerWpnObj);
            Function.Call(Hash.SET_WEAPON_OBJECT_CAMO_INDEX, target, targetWpnObj, camoID);
        }

        public static void FakeRecoil(int cycle)
        {
            float recoil = Config.recoil;
            if (recoil > 0.0f)
            {
                GameplayCamera.Shake(CameraShake.Hand, recoil * 40f);
                GameplayCamera.RelativePitch += recoil;
                if (cycle > 0)
                    GameplayCamera.RelativeHeading += recoil;
                else
                    GameplayCamera.RelativeHeading -= recoil;
            }
        }

        public static void ConflictGetter()
        {
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                DodgeType = assembly.GetType("Shootdodge.Main");
                if (DodgeType != null)
                {
                    DodgeField = DodgeType.GetField("ScriptStatus", BindingFlags.Static | BindingFlags.Public);
                    break;
                }
            }
            Conflict = false;
            if (File.Exists("scripts\\More Gore Settings.xml"))
            {
                XmlDocument MoreGore = new XmlDocument();
                MoreGore.Load("scripts\\More Gore Settings.xml");
                XmlNode MGheal = MoreGore.SelectSingleNode("/MoreGore/Options/Healing");
                if (MGheal != null)
                {
                    XmlAttribute HealingAttrib = MGheal.Attributes["PlayHealingAnimation"];
                    if (HealingAttrib != null)
                    {
                        if (bool.TryParse(HealingAttrib.Value, out bool HealingVal))
                            Conflict = HealingVal;
                    }
                }
            }
        }

        public static void CheckConflict()
        {
            if (Conflict && !Notified)
            {
                Notification.PostTickerForced("Dual Wield WARNING!: More Gore mod installed. Disable PlayHealingAnimation in it's xml to fix triple gun bug when dual-wielding", true);
                Script.Wait(0);
                Notified = true;
            }
            if (DodgeField != null)
                Shootdodge = (int)DodgeField.GetValue(null);
            else
                Shootdodge = 0;
        }

        public static readonly WeaponHash[] GunNeedAdjustment = new WeaponHash[7]
        {
            WeaponHash.AssaultShotgun,
            WeaponHash.PumpShotgun,
            WeaponHash.BullpupShotgun,
            WeaponHash.DoubleBarrelShotgun,
            WeaponHash.SawnOffShotgun,
            WeaponHash.PumpShotgunMk2,
            WeaponHash.CombatShotgun
        };
    }
}
