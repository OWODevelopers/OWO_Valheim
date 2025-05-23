using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;

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

        /*
        When player is eating food
        */
        [HarmonyPatch(typeof(Player), "EatFood")]
        class OnEatingFood
        {
            public static void Postfix(Player __instance)
            {
                if (__instance != Player.m_localPlayer || !owoSkin.CanFeel()) return;
                owoSkin.Feel("Eating");
            }
        }


        /*
        When a status Effect starts, creates and starts thread
        DEBUG FOR MORE STATUS EFFECTS
        */
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

        /*
        When a statusEffect stops, stops thread corresponding to effect name
        */
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

        /*
            When any player is using guardian power
        */
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
    }
}
