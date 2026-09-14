using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace OWO_Valheim
{
    /// <summary>
    /// Correlates Valheim's authored tactile sound events with semantic attack data.
    /// The game's vibration values are deliberately not translated to OWO intensity;
    /// they are only used to identify events that are worth mapping to curated sensations.
    /// </summary>
    internal static class TactileEventRouter
    {
        private sealed class AttackContext
        {
            public Character Actor;
            public float CreatedAt;
            public string Animation;
            public string BossEvent;
            public string WeaponName;
            public ItemDrop.ItemData.ItemType WeaponType;
            public bool IsBoss;
            public bool IsLocalPlayer;
            public bool TactileFeedbackSent;
        }

        private const float ContextLifetime = 1.5f;
        private const float DuplicateWindow = 0.35f;
        private const float BossCorrelationRange = 80f;

        private static readonly List<AttackContext> recentAttacks = new List<AttackContext>();
        private static readonly HashSet<string> discoveredEvents = new HashSet<string>();
        private static readonly Dictionary<string, float> lastSensations = new Dictionary<string, float>();

        private static readonly FieldInfo VibrateAllAudibleField = AccessTools.Field(typeof(ZSFX), "m_vibrateAllAudible");
        private static readonly FieldInfo VibrationModifierField = AccessTools.Field(typeof(ZSFX), "m_vibrationModifier");
        private static readonly MethodInfo IsPlayerCreatorMethod = AccessTools.Method(typeof(ZSFX), "IsPlayerCreator");

        private static readonly string[] ExplosionTokens =
        {
            "bomb", "blast", "deton", "explos", "grenade", "meteor", "nova"
        };

        private static readonly string[] GroundImpactTokens =
        {
            "earthquake", "ground", "land", "slam", "stomp"
        };

        public static void RecordAttack(Attack attack, Humanoid actor, ItemDrop.ItemData weapon)
        {
            if (attack == null || actor == null) return;

            PruneContexts();
            recentAttacks.Add(new AttackContext
            {
                Actor = actor,
                CreatedAt = Time.realtimeSinceStartup,
                Animation = attack.m_attackAnimation ?? "",
                BossEvent = actor.m_bossEvent ?? "",
                WeaponName = weapon?.m_shared?.m_name ?? "",
                WeaponType = weapon?.m_shared?.m_itemType ?? ItemDrop.ItemData.ItemType.None,
                IsBoss = actor.IsBoss(),
                IsLocalPlayer = actor == Player.m_localPlayer
            });
        }

        public static void OnTactileSound(ZSFX source, string clipName)
        {
            if (source == null || string.IsNullOrEmpty(clipName)) return;

            PruneContexts();

            bool global = ReadBool(VibrateAllAudibleField, source);
            bool localCreator = InvokeBool(IsPlayerCreatorMethod, source);
            if (!global && !localCreator) return;

            AttackContext context = FindContext(source.transform.position, localCreator, global);
            LogDiscovery(source, clipName, global, localCreator, context);

            if (context == null || Plugin.owoSkin == null || !Plugin.owoSkin.CanFeel()) return;

            if (context.IsLocalPlayer)
            {
                // Normal weapon feedback is already emitted by OnAttackTrigger. Add a
                // second semantic cue only for an authored explosive event.
                if (!context.TactileFeedbackSent && ContainsAny(clipName, ExplosionTokens))
                {
                    SendOnce("player-explosion:" + clipName, "Explosion", 3);
                    context.TactileFeedbackSent = true;
                }
                return;
            }

            if (!context.IsBoss || context.TactileFeedbackSent || HasLegacyBossFeedback(context)) return;

            string sensation = ContainsAny(clipName, ExplosionTokens) ? "Explosion" : "Earthquake";
            if (ContainsAny(clipName, GroundImpactTokens)) sensation = "Earthquake";
            SendOnce("boss:" + context.BossEvent + ":" + context.Animation + ":" + clipName, sensation, 2);
            context.TactileFeedbackSent = true;
        }

        private static AttackContext FindContext(Vector3 sourcePosition, bool localCreator, bool global)
        {
            AttackContext best = null;
            float bestScore = float.MaxValue;

            for (int i = recentAttacks.Count - 1; i >= 0; i--)
            {
                AttackContext candidate = recentAttacks[i];
                if (!candidate.IsBoss || candidate.Actor == null) continue;

                float distance = Vector3.Distance(sourcePosition, candidate.Actor.transform.position);
                if (distance <= BossCorrelationRange && distance < bestScore)
                {
                    best = candidate;
                    bestScore = distance;
                }
            }

            if (global && best != null) return best;

            if (localCreator)
            {
                for (int i = recentAttacks.Count - 1; i >= 0; i--)
                {
                    if (recentAttacks[i].IsLocalPlayer) return recentAttacks[i];
                }
            }

            return best;
        }

        private static void LogDiscovery(ZSFX source, string clipName, bool global, bool localCreator, AttackContext context)
        {
            string actor = context == null ? "none" : context.IsLocalPlayer ? "local-player" : context.IsBoss ? "boss" : "other";
            string weapon = context?.WeaponName ?? "";
            string weaponType = context == null ? "" : context.WeaponType.ToString();
            string boss = context?.BossEvent ?? "";
            string animation = context?.Animation ?? "";
            string key = clipName + "|" + actor + "|" + weapon + "|" + boss + "|" + animation;
            if (!discoveredEvents.Add(key)) return;

            float modifier = ReadFloat(VibrationModifierField, source, 1f);
            Plugin.Log.LogInfo(
                "Tactile event discovered: " +
                "clip=" + clipName +
                ", actor=" + actor +
                ", weapon=" + weapon +
                ", weaponType=" + weaponType +
                ", boss=" + boss +
                ", attack=" + animation +
                ", global=" + global +
                ", localCreator=" + localCreator +
                ", vibrationModifier=" + modifier.ToString("0.00"));
        }

        private static void SendOnce(string eventKey, string sensation, int priority)
        {
            float now = Time.realtimeSinceStartup;
            float last;
            if (lastSensations.TryGetValue(eventKey, out last) && now - last < DuplicateWindow) return;

            lastSensations[eventKey] = now;
            Plugin.owoSkin.Feel(sensation, priority);
        }

        private static void PruneContexts()
        {
            float oldest = Time.realtimeSinceStartup - ContextLifetime;
            recentAttacks.RemoveAll(context => context.CreatedAt < oldest || context.Actor == null);
        }

        private static bool ContainsAny(string value, string[] tokens)
        {
            for (int i = 0; i < tokens.Length; i++)
            {
                if (value.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        private static bool HasLegacyBossFeedback(AttackContext context)
        {
            switch (context.BossEvent)
            {
                case "boss_eikthyr":
                    return context.Animation == "attack2" || context.Animation == "attack_stomp";
                case "boss_gdking":
                    return context.Animation == "spawn" || context.Animation == "stomp" || context.Animation == "shoot";
                case "boss_bonemass":
                    return context.Animation == "aoe";
                case "boss_moder":
                    return context.Animation == "attack_iceball" || context.Animation == "attack_breath";
                case "boss_goblinking":
                    return context.Animation == "beam" || context.Animation == "nova" || context.Animation == "cast1";
                default:
                    return false;
            }
        }

        private static bool ReadBool(FieldInfo field, object instance)
        {
            return field != null && (bool)field.GetValue(instance);
        }

        private static float ReadFloat(FieldInfo field, object instance, float fallback)
        {
            return field == null ? fallback : (float)field.GetValue(instance);
        }

        private static bool InvokeBool(MethodInfo method, object instance)
        {
            if (method == null) return false;
            try
            {
                return (bool)method.Invoke(instance, null);
            }
            catch (Exception exception)
            {
                Plugin.Log.LogDebug("Could not determine tactile event ownership: " + exception.Message);
                return false;
            }
        }
    }
}
