using TMPro;

namespace NeonLite.Modules.UI.Status
{
#if DEBUG
    [Module]
    internal static class DevText
    {
#pragma warning disable CS0414
        const bool priority = false;
        const bool active = true;

        static void Setup() => StatusText.OnTextReady += SetText;

        static void SetText()
        {
            var text = StatusText.i.MakeText("dev", "NL3 DEV BUILD", -1000);
            text.color = new(1, 0.5f, 0.5f);
            text.alpha = .7f;
            text.fontSize = 24;
            text.fontStyle = FontStyles.Bold;
        }
    }
#endif
}
