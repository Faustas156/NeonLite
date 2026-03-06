using UnityEngine;

namespace NeonLite.Modules.Verification
{
    [Module]
    internal class QualityCheck : MonoBehaviour
    {
        const bool priority = false;
        const bool active = true;

        static bool failed = false;
        const string FAIL = "Quality isn't at {0}. This can happen if you took SuperPotato out without disabling it first";

        static void Activate(bool _)
        {
            NeonLite.holder.AddComponent<QualityCheck>();
            Verifier.OnReset += () => failed = false;
        }

        void Update()
        {
            if (!failed && QualitySettings.GetQualityLevel() != 0)
            {
                failed = true;
                Verifier.SetRunUnverifiable(typeof(QualityCheck), string.Format(FAIL, QualitySettings.names[0]));
            }
        }
    }
}
