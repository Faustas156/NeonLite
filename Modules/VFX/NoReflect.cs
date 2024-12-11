using UnityEngine.Rendering;

namespace NeonLite.Modules.VFX
{
    internal class NoReflect : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool hit = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noReflections", "Disable reflection", "Disable the reflection glares that appear in levels.\n(Requires restart to re-enable.)", false);
            active = setting.SetupForModule(Activate, (_, after) => after);
        }

        static void Activate(bool activate) => active = activate;

        static void OnLevelLoad(LevelData level)
        {
            if (hit || !level || level.type == LevelData.LevelType.Hub)
                return;
            hit = true;
            foreach (var beautify in UnityEngine.Object.FindObjectsOfType<Beautify.Universal.Beautify>())
                beautify.anamorphicFlaresIntensity = new ClampedFloatParameter(0, 0, 0, true);
        }
    }
}
