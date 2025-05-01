using HarmonyLib;
using System.Reflection;

namespace NeonLite.Modules.Optimization
{
    // this only covers some of them (some r inlined) but it does well for the most part
    internal class FastQuit : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = true;

        static readonly MethodInfo[] toPatch = [
            Helpers.Method(typeof(UpdateManager), "UnsubscribeFromUpdate"),
            Helpers.Method(typeof(UpdateManager), "UnsubscribeFromLateUpdate"),
            Helpers.Method(typeof(UpdateManager), "UnsubscribeFromFixedUpdate"),
        ];

        static readonly FieldInfo appIsQuitting = AccessTools.Field(typeof(Singleton<UpdateManager>), "applicationIsQuitting");

        static void Activate(bool _)
        {
            foreach (var method in toPatch)
                Patching.AddPatch(method, ProtectInstance, Patching.PatchTarget.Prefix);
        }

        static bool ProtectInstance() => !(bool)appIsQuitting.GetValue(null);

    }
}
