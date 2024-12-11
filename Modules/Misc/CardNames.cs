using HarmonyLib;
using I2.Loc;
using MelonLoader;
using System.Reflection;

namespace NeonLite.Modules.Misc
{
    // ORIGINAL CODE BY PUPPYPOWERTOOLS AUTHOR HECATE/PANDORAS FOX
    // refactored though
    internal class CardNames : IModule
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false;

        static MelonPreferences_Entry<string> elevate;
        static MelonPreferences_Entry<string> purify;
        static MelonPreferences_Entry<string> godspeed;
        static MelonPreferences_Entry<string> stomp;
        static MelonPreferences_Entry<string> fireball;
        static MelonPreferences_Entry<string> dominion;
        static MelonPreferences_Entry<string> boof;
        static MelonPreferences_Entry<string> ammo;
        static MelonPreferences_Entry<string> health;

        static void Setup()
        {
            var setting = Settings.Add(Settings.h, "Cards", "enabled", "Card Customizations", "Lets you customize the text on each card. Changes require full level restart.", false);
            active = setting.SetupForModule(Activate, (_, after) => after);

            elevate = Settings.Add(Settings.h, "Cards", "elevate", "Elevate Text", null, "Elevate");
            purify = Settings.Add(Settings.h, "Cards", "purify", "Purify Text", null, "Purify");
            godspeed = Settings.Add(Settings.h, "Cards", "godspeed", "Godspeed Text", null, "Godspeed");
            stomp = Settings.Add(Settings.h, "Cards", "stomp", "Stomp Text", null, "Stomp");
            fireball = Settings.Add(Settings.h, "Cards", "fireball", "Fireball Text", null, "Fireball");
            dominion = Settings.Add(Settings.h, "Cards", "dominion", "Dominion Text", null, "Dominion");
            boof = Settings.Add(Settings.h, "Cards", "boof", "Book of Life Text", null, "Book of Life");
            ammo = Settings.Add(Settings.h, "Cards", "ammo", "Ammo Text", null, "Ammo");
            health = Settings.Add(Settings.h, "Cards", "health", "Health Text", null, "Health");
        }

        static readonly MethodInfo original = AccessTools.Method(typeof(LocalizationManager), "GetTranslation");
        static void Activate(bool activate)
        {
            if (activate)
                Patching.AddPatch(original, PreLocalize, Patching.PatchTarget.Prefix);
            else
                Patching.RemovePatch(original, PreLocalize);

            active = activate;
        }

        static bool PreLocalize(string Term, ref string __result)
        {
            __result = Term switch
            {
                "Interface/DISCARD_ELEVATE" => elevate.Value,
                "Interface/DISCARD_PURIFY" => purify.Value,
                "Interface/DISCARD_GODSPEED" => godspeed.Value,
                "Interface/DISCARD_STOMP" => stomp.Value,
                "Interface/DISCARD_FIREBALL" => fireball.Value,
                "Interface/DISCARD_DOMINION" => dominion.Value,
                "Interface/DISCARD_BOOKOFLIFE" => boof.Value,
                "Interface/DISCARD_HEALTH" => health.Value,
                "Interface/DISCARD_AMMO" => ammo.Value,
                _ => null
            };
            return __result == null;
        }

    }
}
