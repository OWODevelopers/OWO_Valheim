using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using System;

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
                owoSkin.Feel("Eating");
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
                    //Posible loop
                    owoSkin.Feel(EffectName);
                }
            }
        }

        [HarmonyPatch(typeof(StatusEffect), "Stop")]
        class OnStatusEffectStop
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
                    //Stop Loop
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
                    owoSkin.Feel("SuperPower");
                }
            }
        }

        [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
        class OnWeaponAttack
        {
            public static void Postfix(Attack __instance, ref Humanoid ___m_character, ref ItemDrop.ItemData ___m_weapon)
            {
                if (___m_character != Player.m_localPlayer || !owoSkin.CanFeel()) return;
                owoSkin.LOG($"HUMANOID {___m_weapon.m_shared.m_itemType} -- {___m_weapon.m_shared.m_animationState} -- {___m_weapon.m_shared.m_name}");
                switch (___m_weapon.m_shared.m_itemType)
                {
                    case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                    case ItemDrop.ItemData.ItemType.Tool:
                        owoSkin.Feel("Attack");
                        break;
                    case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                    case ItemDrop.ItemData.ItemType.Bow:
                        owoSkin.Feel("Attack");
                        break;
                    case ItemDrop.ItemData.ItemType.Shield:
                        owoSkin.Feel("Attack");
                        break;
                }
            }
        }

        [HarmonyPatch(typeof(Character), "SetHealth")]
        class OnPlayerSetHealth
        {
            public static void Postfix(Character __instance)
            {
                if (__instance != Player.m_localPlayer || !owoSkin.CanFeel()) return;
                int hp = Convert.ToInt32(__instance.GetHealth() * 100 / __instance.GetMaxHealth());
                if (hp < 20 && hp > 0)
                {
                    owoSkin.StartHeartBeat();
                }
                else if (hp <= 0)
                {
                    owoSkin.StopAllHapticFeedback();
                    owoSkin.Feel("Death");
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
                owoSkin.Feel("Block");
            }
        }
    }
}
