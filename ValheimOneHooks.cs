using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OWO_Valheim
{
    internal static class ValheimOneHooks
    {
        private static readonly Dictionary<string, float> lastFeedback = new Dictionary<string, float>();

        private static bool CanFeel()
        {
            return Plugin.owoSkin != null && Plugin.owoSkin.CanFeel();
        }

        private static MethodBase FindMethod(string typeName, string methodName)
        {
            Type type = AccessTools.TypeByName(typeName);
            return type == null ? null : AccessTools.Method(type, methodName);
        }

        private static Character GetCharacter(object instance)
        {
            if (instance == null) return null;
            FieldInfo field = AccessTools.Field(instance.GetType(), "m_character");
            return field == null ? null : field.GetValue(instance) as Character;
        }

        private static bool IsNearLocalPlayer(Component component, float range)
        {
            return component != null && Player.m_localPlayer != null &&
                Vector3.Distance(component.transform.position, Player.m_localPlayer.transform.position) <= range;
        }

        private static void FeelWithCooldown(string cooldownKey, float cooldown, string sensation, int priority)
        {
            if (!CanFeel()) return;

            float now = Time.realtimeSinceStartup;
            float last;
            if (lastFeedback.TryGetValue(cooldownKey, out last) && now - last < cooldown) return;

            lastFeedback[cooldownKey] = now;
            Plugin.owoSkin.Feel(sensation, priority);
        }

        [HarmonyPatch]
        private static class OnGrappleActivated
        {
            private static MethodBase TargetMethod() => FindMethod("GrapplingPoint", "Activate");
            private static bool Prepare() => TargetMethod() != null;

            private static void Postfix(Character character)
            {
                if (character == Player.m_localPlayer && CanFeel())
                {
                    Plugin.owoSkin.StartGrappling();
                }
            }
        }

        [HarmonyPatch]
        private static class OnGrapplePull
        {
            private static MethodBase TargetMethod() => FindMethod("GrapplingPoint", "Pull");
            private static bool Prepare() => TargetMethod() != null;

            private static void Postfix(object __instance)
            {
                if (GetCharacter(__instance) == Player.m_localPlayer && CanFeel())
                {
                    Plugin.owoSkin.FeelWithMuscles("Attack", "Both Arms", 2);
                }
            }
        }

        [HarmonyPatch]
        private static class OnGrappleBroken
        {
            private static MethodBase TargetMethod() => FindMethod("GrapplingPoint", "Break");
            private static bool Prepare() => TargetMethod() != null;

            private static void Prefix(object __instance)
            {
                if (GetCharacter(__instance) == Player.m_localPlayer && Plugin.owoSkin != null)
                {
                    Plugin.owoSkin.StopGrappling();
                    if (CanFeel())
                    {
                        Plugin.owoSkin.Feel("Grapple Release", 2);
                    }
                }
            }
        }

        [HarmonyPatch]
        private static class OnPerfectDodge
        {
            private static readonly FieldInfo BeenHitWhileDodgingField = AccessTools.Field(typeof(Player), "m_beenHitWhileDodging");

            private static MethodBase TargetMethod() => FindMethod("Player", "RPC_HitWhileDodging");
            private static bool Prepare() => TargetMethod() != null && BeenHitWhileDodgingField != null;

            private static void Prefix(Player __instance, out bool __state)
            {
                __state = (bool)BeenHitWhileDodgingField.GetValue(__instance);
            }

            private static void Postfix(Player __instance, bool __state)
            {
                bool wasJustTriggered = !__state && (bool)BeenHitWhileDodgingField.GetValue(__instance);
                if (__instance == Player.m_localPlayer && wasJustTriggered)
                {
                    FeelWithCooldown("perfect-dodge", 0.5f, "Perfect Dodge", 3);
                }
            }
        }

        [HarmonyPatch]
        private static class OnAdrenalineChanged
        {
            private static readonly MethodInfo GetAdrenalineMethod = AccessTools.Method(typeof(Player), "GetAdrenaline");
            private static readonly MethodInfo GetMaxAdrenalineMethod = AccessTools.Method(typeof(Player), "GetMaxAdrenaline");

            private static MethodBase TargetMethod() => FindMethod("Player", "AddAdrenaline");
            private static bool Prepare() => TargetMethod() != null && GetAdrenalineMethod != null && GetMaxAdrenalineMethod != null;

            private static void Prefix(Player __instance, out float __state)
            {
                __state = (float)GetAdrenalineMethod.Invoke(__instance, null);
            }

            private static void Postfix(Player __instance, float v, float __state)
            {
                if (__instance != Player.m_localPlayer || v <= 0f || !CanFeel()) return;

                float current = (float)GetAdrenalineMethod.Invoke(__instance, null);
                float maximum = (float)GetMaxAdrenalineMethod.Invoke(__instance, null);
                if (maximum <= 0f || current <= __state) return;

                // Only the moment the bar reaches full is worth a sensation. The
                // __state comparison keeps this to the crossing itself, so topping
                // up an already full bar stays silent.
                if (current >= maximum && __state < maximum)
                {
                    FeelWithCooldown("adrenaline-full", 1f, "Adrenaline", 3);
                }
            }
        }

        [HarmonyPatch]
        private static class OnSnowDestroyed
        {
            private static MethodBase TargetMethod() => FindMethod("SnowDestruction", "Damage");
            private static bool Prepare() => TargetMethod() != null;

            private static void Postfix(Component __instance, float strength)
            {
                if (strength > 0f && IsNearLocalPlayer(__instance, 6f))
                {
                    FeelWithCooldown("snow-impact", 0.3f, "Snow Impact", 1);
                }
            }
        }

        [HarmonyPatch]
        private static class OnSnowClearedFromPiece
        {
            private static MethodBase TargetMethod() => FindMethod("WearNTear", "ChangeSnow");
            private static bool Prepare() => TargetMethod() != null;

            private static void Postfix(WearNTear __instance, float change)
            {
                if (change < 0f && IsNearLocalPlayer(__instance, 4f))
                {
                    FeelWithCooldown("snow-clear", 0.35f, "Snow Impact", 1);
                }
            }
        }

        [HarmonyPatch]
        private static class OnAchievementUnlocked
        {
            private static MethodBase TargetMethod() => FindMethod("Achievements", "AchievementEvent");
            private static bool Prepare() => TargetMethod() != null;

            private static void Postfix(bool showPopup)
            {
                if (showPopup)
                {
                    FeelWithCooldown("achievement", 0.5f, "Achievement", 2);
                }
            }
        }
    }
}
