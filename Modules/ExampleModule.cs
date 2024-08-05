using UnityEngine;


namespace NeonLite.Modules
{
    // This is an example template module that does nothing but print things to log every few seconds.
    // Make sure to remove the wrapped debug when you copy this!
#if DEBUG
    internal class ExampleModule : MonoBehaviour, IModule
    {
#pragma warning disable CS0414
        static ExampleModule instance;
        float printTimer = 0;

        // A "true" priority means it'll start before the low priority mods (before the main menu loads.)
        // This uses a holder, so Activate gets called later.
        const bool priority = false;
        static bool active = false;

        // This will be called once at the start of the game.
        // All mods will be setup at the same time, no matter their priority.
        static void Setup()
        {
            NeonLite.Logger.Msg("ExampleModule Setup!");
            // This is how you would create a toggle setting using the Settings framework.
            // An empty string as the first argument means it goes into the main category.
            var setting = Settings.Add(Settings.h, "", "exampleMod", "Example Module Toggle", "This is a hidden toggle for the example module.", true);
            setting.IsHidden = true; //! REMOVE ME!
            setting.OnEntryValueChanged.Subscribe((_, after) => Activate(after));
            active = setting.Value;
        }

        // Activate will be called either at the start of the game or on mod menu setup depending on the priority.
        // It may be called because of a setting, passed with a bool that says whether or not to activate it.
        // Here is where you should handle Harmony (un)patching and component addition and destruction.
        static void Activate(bool activate)
        {
            NeonLite.Logger.Msg($"ExampleModule Activate {activate}!");
            if (activate)
                instance = NeonLite.holder.AddComponent<ExampleModule>();
            else if (!instance)
                NeonLite.Logger.Warning("ExampleModule was told to deactivate but it hasn't been activated!");
            else
                Destroy(instance);

            active = activate;
        }

        // This will print the level when the level finishes loading, but not before the staging screen finishes.
        static void OnLevelLoad(LevelData level)
        {
            NeonLite.Logger.Msg($"ExampleModule OnLevelLoad {level.levelID}!");
        }

        void Update()
        {
            printTimer -= Time.unscaledDeltaTime;
            if (printTimer <= 0)
            {
                NeonLite.Logger.Msg("ExampleModule tick!");
                printTimer = 10;
            }
        }
    }
#endif
}
