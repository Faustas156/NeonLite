
using System.Collections;
using System.Reflection;
using MelonLoader;
using MelonPrefManager.UI;
using UnityEngine;
using UnityEngine.UI;

// remember this is here if neonlite ever gets other optional dependencies!
[assembly: MelonOptionalDependencies("MelonPreferencesManager")]

namespace NeonLite.Modules.UI
{
    [Module(100)]
    internal static class ColoredMPM
    {
#pragma warning disable CS0414
        const bool priority = true;
        static bool active = false; // make this a setting mayybe? NOTE: forced off for +13

        static void Setup()
        {
            if (!Helpers.CheckModActive(false, "MelonPreferencesManager"))
                active = false; // preferences manager doesn't exist so lets just not do ANYTHING
        }

        static FieldInfo _catinfo_cat;
        static FieldInfo _catinfo_button;
        // ugh
        static PropertyInfo _buttonref_button;


        static readonly FieldInfo currentcat = Helpers.Field<UIManager>("_currentCategory");
        static IDictionary catinfoDict;

        static void Activate(bool _)
        {
            catinfoDict = (IDictionary)Helpers.Field<UIManager>("_categoryInfos").GetValue(null);
            var catinfo = typeof(UIManager).Assembly.GetType(
                typeof(UIManager).Namespace + ".CategoryInfo"
            );
            _catinfo_cat = Helpers.Field(catinfo, "RefCategory");
            _catinfo_button = Helpers.Field(catinfo, "listButton");
            _buttonref_button = _catinfo_button.FieldType.GetProperty("Component");

            Patching.AddPatch(typeof(UIManager), "SetupCategories", SetupCatPostfix, Patching.PatchTarget.Postfix);
            Patching.AddPatch(typeof(UIManager), "SetActiveCategory", PostActiveCat, Patching.PatchTarget.Postfix);
            Patching.AddPatch(typeof(UIManager), "UnsetActiveCategory", PreUnsetCat, Patching.PatchTarget.Prefix);
            Patching.AddPatch(typeof(UIManager), "UnsetActiveCategory", PostUnsetCat, Patching.PatchTarget.Postfix);
        }

        static Assembly GetCatAssembly(MelonPreferences_Category cat, ref int index)
        {
            // set index 15 if it's a hidden one even if we don't have it
            var hidden = cat.IsHidden || cat.Entries.All(x => x.IsHidden);
            if (hidden)
                index = 15;
            else
                index = 0;
            foreach (var kv in Settings.catHolders)
            {
                if (kv.Value.ContainsValue(cat) && Settings.holderOwners.TryGetValue(kv.Key, out var asm))
                {
                    if (index != -1) {
                        if (!hidden) {
                            index = kv.Value.Values
                                .Where(x => !(x.IsHidden || x.Entries.All(x => x.IsHidden)))
                                .OrderBy(x => x.DisplayName)
                                .Select((x, i) => (x, i))
                                .Where(t => t.x == cat)
                                .Select(t => t.i)
                                .DefaultIfEmpty(15)
                                .FirstOrDefault();
                        }
                    }

                    return asm;
                }
            }
            return null;
        }

        static readonly Color defaultcolor = new(0.15f, 0.15f, 0.15f, 1);
        static Color GetCatColor(MelonPreferences_Category cat, ref int index)
        {
            var asm = GetCatAssembly(cat, ref index);
            if (asm == null)
                return default;
            var colorat = MelonUtils.PullAttributeFromAssembly<MelonColorAttribute>(asm);
            if (colorat == null)
                return default;
            var ret = new Color32(colorat.DrawingColor.R, colorat.DrawingColor.G, colorat.DrawingColor.B, 255); ;
            if (ret == Color.white) // if they didn't touch it from nwcommon
                return default;
            return ret;
        }

        const float BASE_V = 0.45f;

        static IEnumerator SetupCatPostfix(IEnumerator __result)
        {
            while (__result.MoveNext())
                yield return __result.Current;

            foreach (var catb in catinfoDict.Values)
            {
                var cat = (MelonPreferences_Category)_catinfo_cat.GetValue(catb);

                int i = 0;
                float v = BASE_V;
                var color = GetCatColor(cat, ref i);
                if (color == default)
                    v = 0.3f;

                Color.RGBToHSV(color, out var h, out var s, out _);
                ColorBlock cb = new()
                {
                    normalColor = Color.HSVToRGB(h, s * (1 - 0.02f * i), v * (1 - 0.04f * i)),
                    highlightedColor = Color.HSVToRGB(h, Mathf.Clamp01(s - 0.1f), v + 0.1f),
                    pressedColor = Color.HSVToRGB(h, s, v - 0.3f),
                    colorMultiplier = 1
                };
                var bref = _catinfo_button.GetValue(catb);
                var button = (Button)_buttonref_button.GetValue(bref);

                button.colors = cb;
            }
        }

        static void PostActiveCat(string categoryIdentifier)
        {
            if (!catinfoDict.Contains(categoryIdentifier))
                return;

            var catb = catinfoDict[categoryIdentifier];
            var cat = (MelonPreferences_Category)_catinfo_cat.GetValue(catb);

            int i = -1;
            float v = BASE_V;
            var color = GetCatColor(cat, ref i);
            if (color == default)
                v = 0.3f;

            var bref = _catinfo_button.GetValue(catb);
            var button = (Button)_buttonref_button.GetValue(bref);
            Color.RGBToHSV(color, out var h, out var s, out _);
            ColorBlock cb = button.colors;
            cb.normalColor = Color.HSVToRGB(h, s, v + 0.2f);
            button.colors = cb;
        }

        static object preCurrentCat;

        static void PreUnsetCat() => preCurrentCat = currentcat.GetValue(null);

        static void PostUnsetCat()
        {
            if (preCurrentCat == null)
                return;

            var cat = (MelonPreferences_Category)_catinfo_cat.GetValue(preCurrentCat);

            int i = 0;
            float v = BASE_V;
            var color = GetCatColor(cat, ref i);
            if (color == default)
                v = 0.3f;

            var bref = _catinfo_button.GetValue(preCurrentCat);
            var button = (Button)_buttonref_button.GetValue(bref);
            Color.RGBToHSV(color, out var h, out var s, out _);
            ColorBlock cb = button.colors;
            cb.normalColor = Color.HSVToRGB(h, s * (1 - 0.02f * i), v * (1 - 0.04f * i));
            button.colors = cb;
        }
    }
}
