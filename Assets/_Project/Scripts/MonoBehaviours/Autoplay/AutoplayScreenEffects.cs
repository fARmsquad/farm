using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayScreenEffects : AutoplayBase
    {
        private void Awake()
        {
            specId = "INT-001";
            specTitle = "Screen Effects";
            totalSteps = 7;
        }

        protected override IEnumerator RunDemo()
        {
            var fx = ScreenEffects.Instance;
            if (fx == null) { currentLabel = "ScreenEffects not found!"; yield break; }

            Step("Fade to black");
            bool done = false;
            fx.FadeToBlack(1.2f, () => done = true);
            yield return new WaitUntil(() => done);
            yield return Wait(0.5f);

            Step("Fade from black");
            done = false;
            fx.FadeFromBlack(1.2f, () => done = true);
            yield return new WaitUntil(() => done);
            yield return Wait(0.8f);

            Step("Screen shake");
            done = false;
            fx.ScreenShake(0.5f, 1f, () => done = true);
            yield return new WaitUntil(() => done);
            yield return Wait(0.8f);

            Step("Show letterbox");
            done = false;
            fx.ShowLetterbox(1f, 0.6f, () => done = true);
            yield return new WaitUntil(() => done);
            yield return Wait(1.5f);

            Step("Hide letterbox");
            done = false;
            fx.HideLetterbox(0.6f, () => done = true);
            yield return new WaitUntil(() => done);
            yield return Wait(0.8f);

            Step("Objective popup");
            done = false;
            fx.ShowObjective("Explore the farmhouse", () => done = true);
            yield return new WaitUntil(() => done);
            yield return Wait(0.8f);

            Step("Mission passed banner");
            done = false;
            fx.ShowMissionPassed("MISSION PASSED", () => done = true);
            yield return new WaitUntil(() => done);
        }
    }
}
