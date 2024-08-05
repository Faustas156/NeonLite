using HarmonyLib;
using System.Collections.Generic;

using UB = IManagedUpdateBehaviour;
using FB = IManagedFixedUpdateBehaviour;
using LB = IManagedLateUpdateBehaviour;
using UnityEngine;

namespace NeonLite.Modules.Optimization
{
    // due to patching basically the entire class this isn't a module and instead is a regular patch class
    //[HarmonyPatch(typeof(UpdateManager))]
    internal class UpdateManRewrite 
    {
        [HarmonyPrefix]
        [HarmonyPatch("SubscribeToUpdate_Internal")]
        static bool SubscribeToUpdate_Internal(UB behaviour, bool ____updateActive, HashSet<UB> ____updateBehavioursToAddHashSet, HashSet<UB> ____updateBehaviourHashSet, List<UB> ____updateBehaviours, HashSet<UB> ____updateBehavioursToRemoveHashSet)
        {
            if (behaviour == null)
                return false;

            if (____updateActive)
                ____updateBehavioursToAddHashSet.Add(behaviour);
            else if (____updateBehaviourHashSet.Add(behaviour))
                ____updateBehaviours.Add(behaviour);
            ____updateBehavioursToRemoveHashSet.Remove(behaviour);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SubscribeToLateUpdate_Internal")]
        static bool SubscribeToLateUpdate_Internal(LB behaviour, bool ____lateUpdateActive, HashSet<LB> ____lateUpdateBehavioursToAddHashSet, HashSet<LB> ____lateUpdateBehaviourHashSet, List<LB> ____lateUpdateBehaviours, HashSet<LB> ____lateUpdateBehavioursToRemoveHashSet)
        {
            if (behaviour == null)
                return false;

            if (____lateUpdateActive)
                ____lateUpdateBehavioursToAddHashSet.Add(behaviour);
            else if (____lateUpdateBehaviourHashSet.Add(behaviour))
                ____lateUpdateBehaviours.Add(behaviour);
            ____lateUpdateBehavioursToRemoveHashSet.Remove(behaviour);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("SubscribeToFixedUpdate_Internal")]
        static bool SubscribeToFixedUpdate_Internal(FB behaviour, bool ____fixedUpdateActive, HashSet<FB> ____fixedUpdateBehavioursToAddHashSet, HashSet<FB> ____fixedUpdateBehaviourHashSet, List<FB> ____fixedUpdateBehaviours, HashSet<FB> ____fixedUpdateBehavioursToRemoveHashSet)
        {
            if (behaviour == null)
                return false;

            if (____fixedUpdateActive)
                ____fixedUpdateBehavioursToAddHashSet.Add(behaviour);
            else if (____fixedUpdateBehaviourHashSet.Add(behaviour))
                ____fixedUpdateBehaviours.Add(behaviour);
            ____fixedUpdateBehavioursToRemoveHashSet.Remove(behaviour);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UnsubscribeFromUpdate_Internal")]
        static bool UnsubscribeFromUpdate_Internal(UB behaviour, HashSet<UB> ____updateBehavioursToAddHashSet, HashSet<UB> ____updateBehaviourHashSet, HashSet<UB> ____updateBehavioursToRemoveHashSet)
        {
            if (____updateBehaviourHashSet.Contains(behaviour))
                ____updateBehavioursToRemoveHashSet.Add(behaviour);
            ____updateBehavioursToAddHashSet.Remove(behaviour);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UnsubscribeFromLateUpdate_Internal")]
        static bool UnsubscribeFromLateUpdate_Internal(LB behaviour, HashSet<LB> ____lateUpdateBehavioursToAddHashSet, HashSet<LB> ____lateUpdateBehaviourHashSet, HashSet<LB> ____lateUpdateBehavioursToRemoveHashSet)
        {
            if (____lateUpdateBehaviourHashSet.Contains(behaviour))
                ____lateUpdateBehavioursToRemoveHashSet.Add(behaviour);
            ____lateUpdateBehavioursToAddHashSet.Remove(behaviour);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("UnsubscribeFromFixedUpdate_Internal")]
        static bool UnsubscribeFromFixedUpdate_Internal(FB behaviour, HashSet<FB> ____fixedUpdateBehavioursToAddHashSet, HashSet<FB> ____fixedUpdateBehaviourHashSet, HashSet<FB> ____fixedUpdateBehavioursToRemoveHashSet)
        {
            if (____fixedUpdateBehaviourHashSet.Contains(behaviour))
                ____fixedUpdateBehavioursToRemoveHashSet.Add(behaviour);
            ____fixedUpdateBehavioursToAddHashSet.Remove(behaviour);

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("Update")]
        static bool Update(ref bool ____updateActive, HashSet<UB> ____updateBehavioursToAddHashSet, HashSet<UB> ____updateBehaviourHashSet, List<UB> ____updateBehaviours, HashSet<UB> ____updateBehavioursToRemoveHashSet)
        {
            ____updateActive = true;

            foreach (var behavior in ____updateBehavioursToAddHashSet)
            {
                if (____updateBehaviourHashSet.Add(behavior))
                    ____updateBehaviours.Add(behavior);
            }
            ____updateBehavioursToAddHashSet.Clear();

            foreach (var behavior in ____updateBehavioursToRemoveHashSet)
            {
                if (____updateBehaviourHashSet.Remove(behavior))
                    ____updateBehaviours.Remove(behavior);
            }
            ____updateBehavioursToRemoveHashSet.Clear();
            float deltaTime = Time.deltaTime;

            foreach (var behavior in ____updateBehaviours)
                behavior.OnUpdate(deltaTime);

            ____updateActive = false;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("LateUpdate")]
        static bool LateUpdate(ref bool ____lateUpdateActive, HashSet<LB> ____lateUpdateBehavioursToAddHashSet, HashSet<LB> ____lateUpdateBehaviourHashSet, List<LB> ____lateUpdateBehaviours, HashSet<LB> ____lateUpdateBehavioursToRemoveHashSet)
        {
            ____lateUpdateActive = true;

            foreach (var behavior in ____lateUpdateBehavioursToAddHashSet)
            {
                if (____lateUpdateBehaviourHashSet.Add(behavior))
                    ____lateUpdateBehaviours.Add(behavior);
            }
            ____lateUpdateBehavioursToAddHashSet.Clear();

            foreach (var behavior in ____lateUpdateBehavioursToRemoveHashSet)
            {
                if (____lateUpdateBehaviourHashSet.Remove(behavior))
                    ____lateUpdateBehaviours.Remove(behavior);
            }
            ____lateUpdateBehavioursToRemoveHashSet.Clear();

            foreach (var behavior in ____lateUpdateBehaviours)
                behavior.OnLateUpdate();

            ____lateUpdateActive = false;

            return false;
        }

        [HarmonyPrefix]
        [HarmonyPatch("FixedUpdate")]
        static bool FixedUpdate(ref bool ____fixedUpdateActive, HashSet<FB> ____fixedUpdateBehavioursToAddHashSet, HashSet<FB> ____fixedUpdateBehaviourHashSet, List<FB> ____fixedUpdateBehaviours, HashSet<FB> ____fixedUpdateBehavioursToRemoveHashSet)
        {
            ____fixedUpdateActive = true;

            foreach (var behavior in ____fixedUpdateBehavioursToAddHashSet)
            {
                if (____fixedUpdateBehaviourHashSet.Add(behavior))
                    ____fixedUpdateBehaviours.Add(behavior);
            }
            ____fixedUpdateBehavioursToAddHashSet.Clear();

            foreach (var behavior in ____fixedUpdateBehavioursToRemoveHashSet)
            {
                if (____fixedUpdateBehaviourHashSet.Remove(behavior))
                    ____fixedUpdateBehaviours.Remove(behavior);
            }
            ____fixedUpdateBehavioursToRemoveHashSet.Clear();
            float deltaTime = Time.fixedDeltaTime;

            foreach (var behavior in ____fixedUpdateBehaviours)
                behavior.OnFixedUpdate(deltaTime);

            ____fixedUpdateActive = false;

            return false;
        }
    }
}
