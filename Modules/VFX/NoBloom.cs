using UnityEngine.Rendering;

namespace NeonLite.Modules.VFX
{
    internal class NoBloom : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static bool hit = false;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "VFX", "noBloom", "Disable bloom", "Disable the bloom effect.\n(Requires restart to re-enable.)", false);
            active = setting.SetupForModule(Activate, static (_, after) => after);
        }

        static void Activate(bool activate) => active = activate;

        static void OnLevelLoad(LevelData level)
        {
            if (hit || !level || level.type == LevelData.LevelType.Hub)
                return;
            hit = true;
            foreach (var beautify in UnityEngine.Object.FindObjectsOfType<Beautify.Universal.Beautify>())
                beautify.bloomIntensity = new ClampedFloatParameter(0, 0, 0, true);
        }
    }
}
