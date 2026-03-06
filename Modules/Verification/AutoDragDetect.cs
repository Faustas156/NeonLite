using UnityEngine.InputSystem;

namespace NeonLite.Modules.Verification
{
    [Module]
    internal static class AutoDragDetect
    {
        const bool priority = false;
        const bool active = true;

        // after COUNT clicks if space between clicks are under CHECK then compare inbetweens with THREs
        const int AUTOCLICK_COUNT = 7;
        const float AUTOCLICK_CHECK = 1000 / 11.0f;
        const float AUTOCLICK_THRES = 2.0f;
        const float AUTOCLICK_HOLDS = 35.0f;
        const string AUTOCLICK_FAIL = "Clicks were too consistent ({0} clicks >{1:F0}ms with average inconsistency of {2:F0}ms and {3:F0}ms holds)";
        static bool autoclickFailed = false;
        // after COUNT clicks check for space between clicks under CHECK
        const int DRAGCLICK_COUNT = 4;
        const float DRAGCLICK_CHECK = 1000 / 17.0f;
        const string DRAGCLICK_FAIL = "Clicks were too fast ({0} clicks, {1:F0}ms fastest {2:F0}ms slowest {3:F2}ms average)";
        static bool dragclickFailed = false;

        static void Activate(bool _)
        {
            GameInput instance = Singleton<GameInput>.Instance;

            instance.Controls.Gameplay.FireCard.started += c => InputStart(c, 0);
            instance.Controls.Gameplay.FireCardAlt.started += c => InputStart(c, 1);

            instance.Controls.Gameplay.FireCard.canceled += c => InputRelease(c, 0);
            instance.Controls.Gameplay.FireCardAlt.canceled += c => InputRelease(c, 1);

            Verifier.OnReset += OnReset;
        }

        static void OnReset()
        {
            autoclickFailed = false;
            dragclickFailed = false;
            autoQueueS[0].Clear();
            autoQueueS[1].Clear();
            autoQueueR[0].Clear();
            autoQueueR[1].Clear();
            dragQueue[0].Clear();
            dragQueue[1].Clear();
        }

        static readonly Queue<double>[] autoQueueS = [new(AUTOCLICK_COUNT + 1), new(AUTOCLICK_COUNT + 1)];
        static readonly Queue<double>[] autoQueueR = [new(AUTOCLICK_COUNT + 1), new(AUTOCLICK_COUNT + 1)];
        static readonly Queue<double>[] dragQueue = [new(DRAGCLICK_COUNT + 1), new(DRAGCLICK_COUNT + 1)];

        static bool CycleQueue(this Queue<double> queue, double add, int count)
        {
            queue.Enqueue(add);
            if (queue.Count > count)
                queue.Dequeue();
            return queue.Count == count;
        }

        static readonly double[] autoHold = new double[AUTOCLICK_COUNT];
        static readonly double[] autoComp = new double[AUTOCLICK_COUNT - 1];
        static readonly double[] autoCons = new double[AUTOCLICK_COUNT - 2];

        static void CheckAutoQueue(Queue<double> start, Queue<double> release)
        {
            double last = 0;
            int i = 0;
            foreach ((var s, var r) in start.Zip(release, (x, y) => (x, y)))
            {
                autoHold[i] = r - s;
                if (i++ == 0)
                {
                    last = s;
                    continue;
                }

                var diff = s - last;
                if (diff > AUTOCLICK_CHECK)
                    return;
                last = s;
                autoComp[i - 2] = diff;
            }

            NeonLite.Logger.DebugMsg(string.Format("AUTO1 MIN {0:F2} MAX {1:F2} AVG {2:F2} {3:F2}", autoComp.Min(), autoComp.Max(), autoComp.Average(), AUTOCLICK_CHECK));

            // the fact we're here at all means it didn't pass the speed check
            for (i = 0; i < AUTOCLICK_COUNT - 2; ++i)
                autoCons[i] = autoComp[i] - autoComp[i + 1];

            if (Math.Abs(autoCons.Average()) > AUTOCLICK_THRES)
                return;

            NeonLite.Logger.DebugMsg(string.Format("AUTO2 MIN {0:F2} MAX {1:F2} AVG {2:F2} {3:F2}", autoCons.Min(), autoCons.Max(), autoCons.Average(), AUTOCLICK_THRES));

            // didn't pass the consistency check
            if (autoHold.Average() > AUTOCLICK_HOLDS)
                return;

            NeonLite.Logger.DebugMsg(string.Format("AUTO3 MIN {0:F2} MAX {1:F2} AVG {2:F2} {3:F2}", autoHold.Min(), autoHold.Max(), autoHold.Average(), AUTOCLICK_HOLDS));

            // we failed
            autoclickFailed = true;
            Verifier.SetRunUnverifiable(typeof(AutoDragDetect), string.Format(AUTOCLICK_FAIL, AUTOCLICK_COUNT, autoComp.Max(), autoCons.Average(), autoHold.Average()));
        }

        static readonly double[] dragComp = new double[DRAGCLICK_COUNT - 1];
        static void CheckDragQueue(Queue<double> queue)
        {
            double last = 0;
            int i = 0;
            foreach (var v in queue)
            {
                if (i++ == 0)
                {
                    last = v;
                    continue;
                }

                var diff = v - last;

                if (diff > DRAGCLICK_CHECK)
                    return;
                last = v;
                dragComp[i - 2] = diff;
            }

            // if we're here we failed
            dragclickFailed = true;
            Verifier.SetRunUnverifiable(typeof(AutoDragDetect), string.Format(DRAGCLICK_FAIL, DRAGCLICK_COUNT, dragComp.Min(), dragComp.Max(), dragComp.Average()));
        }

#if DEBUG
        static readonly double[] holddiff = new double[2];
#endif

        static void InputStart(InputAction.CallbackContext context, int which)
        {
            var auto = autoQueueS[which];
            var drag = dragQueue[which];

            var ms = context.time * 1000;

#if DEBUG
            NeonLite.Logger.DebugMsg($"INPUTSTART {ms}");
            holddiff[which] = ms;
#endif
            auto.CycleQueue(ms, AUTOCLICK_COUNT);
            if (!dragclickFailed && drag.CycleQueue(ms, DRAGCLICK_COUNT))
                CheckDragQueue(drag);
        }

        static void InputRelease(InputAction.CallbackContext context, int which)
        {
            var auto = autoQueueR[which];

            var ms = context.time * 1000;
#if DEBUG
            NeonLite.Logger.DebugMsg($"INPUTRELEASE {ms} {ms - holddiff[which]}");
#endif

            if (!autoclickFailed && auto.CycleQueue(ms, AUTOCLICK_COUNT))
                CheckAutoQueue(autoQueueS[which], auto);
        }
    }
}
