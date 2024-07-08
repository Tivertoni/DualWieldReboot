using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using System;
using System.IO;
using System.Reflection;
using System.Xml;

namespace DualWield
{
    public static class Utils
    {
        private static string muzzleFx;
        public static bool surpressed = false;
        public static void SetIkTarget(Ped ped)
        {
            RaycastResult raycast1 = World.Raycast(GameplayCamera.Position, GameplayCamera.Direction, 9999f, IntersectFlags.Everything, Main.Char);
            if (raycast1.DidHit)
            {
                Function.Call(Hash.SET_IK_TARGET, ped, 3, null, null, raycast1.HitPosition.X, raycast1.HitPosition.Y, raycast1.HitPosition.Z, 64, -8, 8);
                Function.Call(Hash.SET_IK_TARGET, ped, 4, null, null, raycast1.HitPosition.X, raycast1.HitPosition.Y, raycast1.HitPosition.Z, 64, -8, 8);
                Function.Call(Hash.SET_IK_TARGET, ped, 1, null, null, raycast1.HitPosition.X, raycast1.HitPosition.Y, raycast1.HitPosition.Z, 64, 0, 0);
                Function.Call(Hash.SET_IK_TARGET, ped, 2, null, null, raycast1.HitPosition.X, raycast1.HitPosition.Y, raycast1.HitPosition.Z, 64, 0, 0);
            }
            else SetIK(false);
        }

        public static void SortPtfx()
        {
            if (!surpressed)
            {
                if (Main.CharWpn.Group == WeaponGroup.MG || Main.CharWpn.Group == WeaponGroup.AssaultRifle)
                    muzzleFx = "muz_assault_rifle";
                else if (Main.CharWpn.Group == WeaponGroup.Pistol)
                    muzzleFx = "muz_pistol";
                else if (Main.CharWpn.Group == WeaponGroup.SMG)
                    muzzleFx = "muz_smg";
                else
                    muzzleFx = "muz_shotgun";
            }
            else muzzleFx = "muz_pistol_silencer";
        }

        public static void ShootAt(RaycastResult raycast, Ped ped, Entity gun)
        {
            Function.Call(Hash.SET_PED_SHOOTS_AT_COORD, ped, raycast.HitPosition.X, raycast.HitPosition.Y, raycast.HitPosition.Z, 0);
            World.CreateParticleEffectNonLooped(new ParticleEffectAsset("core"), muzzleFx,
                Function.Call<Vector3>(Hash.GET_ENTITY_BONE_POSTION, gun, Function.Call<int>(Hash.GET_ENTITY_BONE_INDEX_BY_NAME, gun, "gun_muzzle")), gun.Rotation);
        }
        public static void SetIK(bool on)
        {
            Function.Call(Hash.SET_PED_CAN_ARM_IK, Main.Char, on);
            Function.Call(Hash.SET_PED_CAN_HEAD_IK, Main.Char, on);
            Function.Call(Hash.SET_PED_CAN_TORSO_IK, Main.Char, on);
        }

        public static void ShowPlayerWpn(bool shown)
        {
            Vector3 aimPos0 = new Vector3(-0.3f, 0f, 0f);
            if (!Main.WpnOff.Contains(Main.CharWpn.Group))
            {
                float WpnLength = Main.CharWpn.Model.Dimensions.frontTopRight.X - Main.CharWpn.Model.Dimensions.rearBottomLeft.X;
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
                    Main.Char.Weapons.CurrentWeaponObject.AttachTo(Main.Char.Bones[Bone.SkelSpine0], aimPos0, Vector3.Zero, false, false, false, true, default);
                else
                    Main.Char.Weapons.CurrentWeaponObject.AttachTo(Main.Char.Bones[Bone.SkelRightHand], Main.aimPosR + posAdj, Main.aimRotR + rotAdj, false, false, false, true, default);
                Function.Call(Hash.SET_PED_CURRENT_WEAPON_VISIBLE, Main.Char, shown, false, false, false);
                Main.Char.Weapons.CurrentWeaponObject.IsVisible = shown;
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
            Entity playerWpn = Main.Char.Weapons.CurrentWeaponObject;
            WeaponHash targetWpn = target.Weapons.Current.Hash;
            foreach (WeaponComponentHash component in Enum.GetValues(typeof(WeaponComponentHash)))
            {
                if (Function.Call<bool>(Hash.HAS_WEAPON_GOT_WEAPON_COMPONENT, playerWpn, component))
                    Function.Call(Hash.GIVE_WEAPON_COMPONENT_TO_PED, target, targetWpn, component);
            }
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
                Main.DodgeType = assembly.GetType("Shootdodge.Main");
                if (Main.DodgeType != null)
                {
                    Main.DodgeField = Main.DodgeType.GetField("ScriptStatus", BindingFlags.Static | BindingFlags.Public);
                    break;
                }
            }
            Main.Conflict = false;
            if (File.Exists("scripts\\More Gore Settings.xml"))
            {
                XmlDocument MoreGore = new XmlDocument();
                MoreGore.Load("scripts\\More Gore Settings.xml");
                XmlNode HealingOn = MoreGore.SelectSingleNode("/MoreGore/Options/Healing");
                if (HealingOn != null)
                {
                    XmlAttribute HealingAttrib = HealingOn.Attributes["PlayHealingAnimation"];
                    if (HealingAttrib != null)
                    {
                        if (bool.TryParse(HealingAttrib.Value, out bool HealingVal))
                            Main.Conflict = HealingVal;
                    }
                }
            }
        }

        public static void CheckConflict()
        {
            if (Main.Conflict && !Main.Notified)
            {
                Notification.PostTickerForced("Dual Wield WARNING!: More Gore mod installed. Disable PlayHealingAnimation in it's xml to fix triple gun bug when dual-wielding", true);
                Script.Wait(0);
                Main.Notified = true;
            }
            if (Main.DodgeField != null)
                Main.Shootdodge = (int)Main.DodgeField.GetValue(null);
            else
                Main.Shootdodge = 0;
        }
    }
}
