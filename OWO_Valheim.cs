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
    }
}
