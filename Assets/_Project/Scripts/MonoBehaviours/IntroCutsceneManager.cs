using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace FarmSimVR.MonoBehaviours
{
    public class IntroCutsceneManager : MonoBehaviour
    {
        [SerializeField] private Camera mainCam;
        [SerializeField] private CanvasGroup fadeOverlay;
        [SerializeField] private Text subtitleText;
        [SerializeField] private string nextScene = "FarmMain";

        [SerializeField] private Transform shot1Rectangle;
        [SerializeField] private Transform shot2BigRectangle;
        [SerializeField] private Transform shot3Circle;

        private IEnumerator Start()
        {
            fadeOverlay.alpha = 1f;
            subtitleText.text = "";
            yield return new WaitForSeconds(0.2f);

            yield return PlayShot1();
            yield return PlayShot2();
            yield return PlayShot3();

            SceneManager.LoadScene(nextScene);
        }

        // Shot 1 — slow zoom toward a flat rectangle. Text: "person sleeping"
        private IEnumerator PlayShot1()
        {
            Vector3 startPos = shot1Rectangle.position + new Vector3(0f, 2.5f, -10f);
            Vector3 endPos   = shot1Rectangle.position + new Vector3(0f, 1.2f, -4f);
            Quaternion startRot = Quaternion.Euler(14f, 0f, 0f);
            Quaternion endRot   = Quaternion.Euler(8f,  0f, 0f);

            mainCam.transform.SetPositionAndRotation(startPos, startRot);
            yield return FadeIn("person sleeping", 1.2f);
            yield return MoveCamera(startPos, endPos, startRot, endRot, 5f);
            yield return FadeOut(1f);
        }

        // Shot 2 — orbit around big rectangle. Text: "the town couldn't sleep"
        private IEnumerator PlayShot2()
        {
            Vector3 center = shot2BigRectangle.position;
            float radius = 13f;
            float height = 3.5f;

            Vector3 startPos = center + new Vector3(0f, height, -radius);
            mainCam.transform.position = startPos;
            mainCam.transform.LookAt(center + Vector3.up);

            yield return FadeIn("the town couldn't sleep", 1.2f);
            yield return OrbitCamera(center, radius, height, 190f, 6.5f);
            yield return FadeOut(1f);
        }

        // Shot 3 — low flyover dolly-in to circle. Text: "you must tame the chicken"
        private IEnumerator PlayShot3()
        {
            Vector3 startPos = shot3Circle.position + new Vector3(0f, 6f, -20f);
            Vector3 endPos   = shot3Circle.position + new Vector3(0f, 0.8f, -3.5f);
            Quaternion startRot = Quaternion.Euler(18f, 0f, 0f);
            Quaternion endRot   = Quaternion.Euler(4f,  0f, 0f);

            mainCam.transform.SetPositionAndRotation(startPos, startRot);
            yield return FadeIn("you must tame the chicken", 1.2f);
            yield return MoveCamera(startPos, endPos, startRot, endRot, 5.5f);
            yield return FadeOut(1f);
        }

        // Helpers

        private IEnumerator FadeIn(string subtitle, float duration)
        {
            subtitleText.text = subtitle;
            yield return Fade(1f, 0f, duration);
        }

        private IEnumerator FadeOut(float duration)
        {
            yield return Fade(0f, 1f, duration);
            subtitleText.text = "";
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                fadeOverlay.alpha = Mathf.Lerp(from, to, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            fadeOverlay.alpha = to;
        }

        private IEnumerator MoveCamera(Vector3 from, Vector3 to, Quaternion fromRot, Quaternion toRot, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                float t = Smoothstep(elapsed / duration);
                mainCam.transform.SetPositionAndRotation(
                    Vector3.Lerp(from, to, t),
                    Quaternion.Slerp(fromRot, toRot, t));
                elapsed += Time.deltaTime;
                yield return null;
            }
            mainCam.transform.SetPositionAndRotation(to, toRot);
        }

        private IEnumerator OrbitCamera(Vector3 center, float radius, float height, float totalDegrees, float duration)
        {
            float elapsed = 0f;
            float startAngle = 180f;
            while (elapsed < duration)
            {
                float t = Smoothstep(elapsed / duration);
                float angle = (startAngle + totalDegrees * t) * Mathf.Deg2Rad;
                Vector3 pos = center + new Vector3(Mathf.Sin(angle) * radius, height, Mathf.Cos(angle) * radius);
                mainCam.transform.position = pos;
                mainCam.transform.LookAt(center + Vector3.up);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }

        private static float Smoothstep(float t) => t * t * (3f - 2f * t);
    }
}
