using HarmonyLib;
using System.Reflection;
using UnityEngine;

#pragma warning disable CS0414

namespace NeonLite.Modules.Misc
{
    internal class FilledInsight : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Misc", "fillInsight", "Filled Insight", "Replaces the spawned insights with their filled variants, even if they should be empty.", true);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate)
        {
            Patching.TogglePatch(activate, typeof(ObjectSpawner), "Spawn", SpawnOverride, Patching.PatchTarget.Prefix);

            active = activate;
        }

        static bool SpawnOverride(ObjectSpawner __instance, ref GameObject ____spawnedObject, ref bool ____hasSpawnedObject, ref FollowSpring ___m_spring)
        {
            var level = NeonLite.Game.GetCurrentLevel();
            var gd = NeonLite.Game.GetGameData();
            if (__instance._objectType != ObjectSpawner.Type.Goal || !level.isSidequest || (level.sidequestGiver?.ID ?? "GREEN") == "GREEN")
                return true;
            if (____spawnedObject != null && ____spawnedObject.activeInHierarchy)
                return true;

            var card = gd.GetCard("LORE_COLLECTIBLE_BIG");
            var obj = ObjectSpawner.SpawnObject(ObjectSpawner.Type.Goal, __instance.transform.position, __instance.transform.rotation, null, card);
            ____spawnedObject = obj;
            ____hasSpawnedObject = true;

            if (__instance.followTransformOnSpawn)
            {
                ___m_spring = ____spawnedObject.AddComponent<FollowSpring>();
                ___m_spring.transformToFollow = __instance.followTransformOnSpawn;
                ___m_spring.spring.DampingRatio *= __instance.springDampeningMult;
            }

            return false;
        }
    }
}
