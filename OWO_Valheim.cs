using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace OWO_Valheim
{
    [BepInPlugin("org.bepinex.plugins.OWO_Valheim", "OWO_Valheim", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
#pragma warning disable CS0109
        internal static new ManualLogSource Log;
#pragma warning restore CS0109

        public static OWOSkin owoSkin;


        private void Awake()
        {
            Log = Logger;
            Logger.LogMessage("OWO_Valheim plugin is loaded!");

            owoSkin = new OWOSkin();

            var harmony = new Harmony("owo.patch.valheim");
            harmony.PatchAll();
        }


        [HarmonyPatch(typeof(Player), "EatFood")]
        class OnEatingFood
        {
            public static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer || !owoSkin.CanFeel()) return;
                owoSkin.Feel("Eating", 2);
            }
        }

        [HarmonyPatch(typeof(StatusEffect), "TriggerStartEffects")]
        class OnTriggerStatus
        {
            public static void Postfix(StatusEffect __instance)
            {
                if (!owoSkin.CanFeel() || __instance.m_character != Player.m_localPlayer) return;
                string EffectName = "";
                switch (__instance.m_name)
                {
                    case "$se_puke_name":
                        EffectName = "Vomit";
                        break;
                    case "$se_poison_name":
                        EffectName = "Poison";
                        break;
                    case "$se_burning_name":
                        EffectName = "Flame";
                        break;
                    case "$se_freezing_name":
                        EffectName = "Freezing";
                        break;
                }
                if (EffectName != "")
                {
                    owoSkin.Feel(EffectName);
                }
            }
        }

        [HarmonyPatch(typeof(Player), "ActivateGuardianPower")]
        class OnActiveGuardianPower
        {
            public static void Postfix(Player __instance)
            {
                if (!owoSkin.CanFeel()) return;

                if (Player.IsPlayerInRange(__instance.transform.position, 10f, Player.m_localPlayer.GetPlayerID()))
                {
                    owoSkin.Feel("Super Power", 2);
                }
            }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
        class OnWeaponAttack
        {
            public static Dictionary<string, float> rangeBoss = new Dictionary<string, float>()
        {
            {"boss_eikthyr",  20f},
            {"boss_gdking", 40f },
            {"boss_bonemass",  20f},
            {"boss_moder",  40f},
            {"boss_goblinking",  60f},
        };
            public static void Postfix(Attack __instance, Humanoid ___m_character, ItemDrop.ItemData ___m_weapon)
            {
                if (!owoSkin.CanFeel()) return;

                if (___m_character.IsBoss()) goto Boss;

                if (___m_character == Player.m_localPlayer) goto Player;

                return;


            Boss:
                float range = (rangeBoss.ContainsKey(___m_character.m_bossEvent)) ? rangeBoss[___m_character.m_bossEvent] : 20f;
                bool closeTo = Player.IsPlayerInRange(___m_character.transform.position, range, Player.m_localPlayer.GetPlayerID());

                if (!closeTo) return;

                switch (___m_character.m_bossEvent)
                {
                    case "boss_eikthyr":
                        if (__instance.m_attackAnimation == "attack2")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        if (__instance.m_attackAnimation == "attack_stomp")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        break;
                    case "boss_gdking":
                        if (__instance.m_attackAnimation == "spawn")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        if (__instance.m_attackAnimation == "stomp")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        if (__instance.m_attackAnimation == "shoot")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        break;
                    case "boss_bonemass":
                        if (__instance.m_attackAnimation == "aoe")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        break;
                    case "boss_moder":
                        if (__instance.m_attackAnimation == "attack_iceball")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        if (__instance.m_attackAnimation == "attack_breath")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        break;
                    case "boss_goblinking":
                        if (__instance.m_attackAnimation == "beam")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        if (__instance.m_attackAnimation == "nova")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        if (__instance.m_attackAnimation == "cast1")
                        {
                            owoSkin.Feel("Earthquake", 2);
                        }
                        break;

                }
                return;


            Player:
                owoSkin.LOG($"HUMANOID {___m_weapon.m_shared.m_itemType} -- {___m_weapon.m_shared.m_animationState} -- {___m_weapon.m_shared.m_name}");
                switch (___m_weapon.m_shared.m_itemType)
                {
                    case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                    case ItemDrop.ItemData.ItemType.Tool:
                        owoSkin.Feel("Attack", 3);
                        break;
                    case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                    case ItemDrop.ItemData.ItemType.Bow:
                        owoSkin.Feel("Attack", 3);
                        break;
                    case ItemDrop.ItemData.ItemType.Shield:
                        owoSkin.Feel("Attack", 3);
                        break;
                }
                return;
            }
        }

        [HarmonyPatch(typeof(Character), "SetHealth")]
        class OnPlayerSetHealth
        {
            public static void Postfix(Character __instance)
            {
                if (__instance != Player.m_localPlayer || !owoSkin.CanFeel()) return;
                int hp = Convert.ToInt32(__instance.GetHealth() * 100 / __instance.GetMaxHealth());

                if (hp <= 0)
                {
                    owoSkin.StopAllHapticFeedback();
                    owoSkin.playerEnabled = false;
                    owoSkin.Feel("Death", 4);
                }
                else if (hp < 20 && hp > 0)
                {
                    owoSkin.StartHeartBeat();
                }
                else
                {
                    owoSkin.StopHeartBeat();
                }
            }
        }

        [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
        class OnBlocking
        {
            public static void Postfix(Humanoid __instance, bool __result)
            {

                if (!owoSkin.CanFeel() || __instance != Player.m_localPlayer || !__result) return;
                owoSkin.Feel("Block", 3);
            }
        }

        [HarmonyPatch(typeof(Character), "ApplyDamage")]
        class OnPlayerHit
        {
            public static void Postfix(Character __instance, HitData hit)
            {

                if (__instance != Player.m_localPlayer || !owoSkin.CanFeel()) return;
                if (Mathf.FloorToInt(hit.GetTotalDamage()) > 0)
                    owoSkin.Feel("Impact", 3);
            }
        }

        [HarmonyPatch(typeof(Player), "TeleportTo")]
        class OnTeleportStart
        {
            public static void Postfix(Player __instance, bool __result, ref bool ___m_teleporting)
            {
                if (__instance != Player.m_localPlayer || !owoSkin.CanFeel() || !__result) return;

                if (___m_teleporting)
                {
                    owoSkin.StopAllHapticFeedback();
                    owoSkin.StartTeleporting();
                }
            }
        }

        [HarmonyPatch(typeof(Player), "UpdateTeleport")]
        class OnTeleportUpdate
        {
            public static void Postfix(Player __instance, ref bool ___m_teleporting)
            {
                if (__instance != Player.m_localPlayer || !owoSkin.CanFeel()) return;
                if (!___m_teleporting)
                {
                    owoSkin.StopTeleporting();
                }
            }
        }

        [HarmonyPatch(typeof(Thunder), "DoThunder")]
        class OnThunder
        {
            public static void Postfix()
            {
                if (!owoSkin.CanFeel()) return;
                owoSkin.Feel("Thunder", 1);
            }
        }

        [HarmonyPatch(typeof(Tameable), "Interact")]
        class OnPet
        {
            public static void Postfix(bool __result, bool alt)
            {
                if (!owoSkin.CanFeel()) return;
                if (__result && !alt)
                {
                    owoSkin.Feel("Pet", 2);
                }
            }
        }

        [HarmonyPatch(typeof(WearNTear), "Damage")]
        class OnBoatDamage
        {
            public static void Postfix(WearNTear __instance)
            {
                if (!owoSkin.CanFeel()) return;
                Ship component = __instance.GetComponent<Ship>();
                if (component != null && component.IsPlayerInBoat(Player.m_localPlayer))
                {
                    owoSkin.Feel("Ship Damage", 3);
                }
            }
        }

        [HarmonyPatch(typeof(WearNTear), "Repair")]
        class OnReapir
        {
            public static void Postfix(WearNTear __instance, bool __result)
            {
                if (!owoSkin.CanFeel()) return;
                Piece component = __instance.GetComponent<Piece>();
                if (__result && component != null && component == Player.m_localPlayer.GetHoveringPiece())
                {
                    owoSkin.Feel("Hammer", 2);
                }
            }
        }

        [HarmonyPatch(typeof(WearNTear), "OnPlaced")]
        class OnPlaceHammer
        {
            public static void Postfix(WearNTear __instance)
            {
                if (!owoSkin.CanFeel()) return;
                Piece component = __instance.GetComponent<Piece>();
                if (component != null && component.IsCreator())
                {
                    owoSkin.Feel("Hammer", 2);
                }
            }
        }

        [HarmonyPatch(typeof(EnvMan), "UpdateEnvironment")]
        class OnEnvChange
        {
            private static readonly string[] rain = { "ThunderStorm", "Rain" };
            private static string currentEnv = "";
            private static int envStarted = 0;
            //when env is changing, there is a delay between effective change of env in the code,
            //and rain actually showing on screen
            private static readonly int envDelay = 12;
            public static void Postfix(EnvSetup ___m_currentEnv)
            {
                if (!owoSkin.CanFeel() || !Player.m_localPlayer) return;

                if (currentEnv != ___m_currentEnv.m_name)
                {
                    currentEnv = ___m_currentEnv.m_name;
                    envStarted = (int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
                }

                if (rain.Contains(currentEnv))
                {
                    if (Player.m_localPlayer.InShelter())
                    {
                        owoSkin.StopRaining();
                    }
                    else if (!owoSkin.rainingIsActive)
                    {
                        int startDelay = (int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - envStarted;
                        if (startDelay > envDelay)
                        {
                            owoSkin.StartRaining();
                        }
                    }
                }
                else if (owoSkin.rainingIsActive)
                {
                    int endDelay = (int)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds() - envStarted;
                    if (endDelay > envDelay)
                    {
                        owoSkin.StopRaining();
                    }
                }
            }

            [HarmonyPatch(typeof(Player), "SetGuardianPower")]
            class OnSetGuardianPower
            {
                public static void Postfix(Player __instance, bool ___m_isLoading)
                {
                    if (!owoSkin.CanFeel() || Player.m_localPlayer != __instance || ___m_isLoading) return;
                    owoSkin.Feel("Set Power", 2);
                }
            }
        }

        [HarmonyPatch(typeof(OfferingBowl), "SpawnBoss")]
        class OnBossSpawn
        {
            public static void Postfix(OfferingBowl __instance, Vector3 spawnPoint)
            {

                if (!Player.IsPlayerInRange(spawnPoint, 100f, Player.m_localPlayer.GetPlayerID()) || !owoSkin.CanFeel()) return;
                owoSkin.Feel("Boss Spawn", 3);
            }
        }

        [HarmonyPatch(typeof(Ragdoll), "DestroyNow")]
        class OnCorpseExplosion
        {
            public static void Postfix(Ragdoll __instance)
            {
                if (!owoSkin.CanFeel() || !Player.IsPlayerInRange(__instance.transform.position, 20f, Player.m_localPlayer.GetPlayerID())) return;
                foreach (EffectList.EffectData obj in __instance.m_removeEffect.m_effectPrefabs)
                {
                    switch (obj.m_prefab.name)
                    {
                        case "vfx_corpse_destruction_medium":
                            goto SendSensation;
                        case "vfx_corpse_destruction_small":
                            if (!Player.IsPlayerInRange(__instance.transform.position, 7f, Player.m_localPlayer.GetPlayerID())) return;
                            goto SendSensation;
                    }
                }

            SendSensation:
                owoSkin.Feel("Explosion", 2);
            }
        }

        [HarmonyPatch(typeof(Humanoid), "GetAttackDrawPercentage")]
        class OnDrawBow
        {
            public static void Postfix(Humanoid __instance, float __result)
            {
                if (!owoSkin.CanFeel() || __instance != Player.m_localPlayer) return;
                if (__result == 0f)
                {
                    owoSkin.StopStringBow();
                    return;
                }

                owoSkin.stringBowIntensity = Mathf.FloorToInt(Mathf.Clamp(__result * 100, 40f, 100f));
                owoSkin.StartStringBow();
            }
        }

        [HarmonyPatch(typeof(Player), "OnSpawned")]
        class OnPlayerSpawn
        {
            public static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer) return;

                owoSkin.playerEnabled = true;
            }
        }

        [HarmonyPatch(typeof(Player), "OnJump")]
        class OnPlayerJump
        {
            public static void Postfix(Player __instance)
            {
                if (!owoSkin.CanFeel() || __instance != Player.m_localPlayer) return;
                owoSkin.Feel("Jump", 2);
            }
        }

        //[HarmonyPatch(typeof(Character), "OnCollisionStay")]
        //class OnPlayerLand
        //{
        //    public static void Postfix(Player __instance, bool ___m_groundContact)
        //    {
        //        if (!owoSkin.CanFeel() || __instance != Player.m_localPlayer) return;
        //        //owoSkin.Feel("Landing", 1);
                
        //        if (___m_groundContact)
        //        {
        //        }
        //    }
        //}

        //[HarmonyPatch(typeof(Player), "Dodge")]
        //class OnPlayerDodge
        //{
        //    public static void Postfix(Character __instance)
        //    {
        //        if (!owoSkin.CanFeel() || __instance != Player.m_localPlayer) return;
        //        owoSkin.Feel("Dodge", 2);
        //    }
        //}

    }
}
