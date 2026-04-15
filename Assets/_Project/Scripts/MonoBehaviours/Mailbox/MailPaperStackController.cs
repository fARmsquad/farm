using System.Collections;
using System.Collections.Generic;
using FarmSimVR.Core.Mailbox;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace FarmSimVR.MonoBehaviours.Mailbox
{
    /// <summary>
    /// Displays all mail as a physical stack of papers.
    /// Right arrow slides the front paper to the back.
    /// Left arrow brings the bottom paper to the front.
    /// Space breaks the wax seal on the front paper (marks it as read).
    /// M key toggles the stack open/closed.
    /// </summary>
    public class MailPaperStackController : MonoBehaviour
    {
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private GameObject paperPrefab;
        [SerializeField] private Transform  stackRoot;

        public bool IsOpen { get; private set; }

        private readonly List<GameObject>              _papers = new();
        private readonly Dictionary<GameObject, string> _ids   = new();
        private bool _animating;

        private const float SLIDE_DUR  = 0.18f;
        private const float SLIDE_DIST = 900f;

        // Visual offsets per depth: 0 = front, 1 = second, 2 = third
        private static readonly Vector2[] Offsets   = { Vector2.zero, new(14f, -12f), new(26f, -22f) };
        private static readonly float[]   Rotations = { 0f, -2f, 1.5f };

        private void Awake() => panelRoot?.SetActive(false);

        private void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb[Key.Enter].wasPressedThisFrame) { Toggle(); return; }
            if (!IsOpen) return;

            if (!_animating)
            {
                if      (kb[Key.RightArrow].wasPressedThisFrame) StartCoroutine(SlideToBack());
                else if (kb[Key.LeftArrow].wasPressedThisFrame)  StartCoroutine(BringToFront());
            }

            if (kb[Key.Space].wasPressedThisFrame) BreakSeal();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void Open()
        {
            IsOpen = true;
            panelRoot?.SetActive(true);
            BuildStack();
            FindAnyObjectByType<TownPlayerController>()?.SuspendControl();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }

        public void Close()
        {
            IsOpen = false;
            panelRoot?.SetActive(false);
            ClearStack();
            FindAnyObjectByType<TownPlayerController>()?.ResumeControl();
        }

        // ── Stack ─────────────────────────────────────────────────────────────

        private void BuildStack()
        {
            ClearStack();
            var allMail = MailGeneratorDriver.MailboxService?.AllMail;
            if (allMail == null || allMail.Count == 0) return;

            // Iterate in reverse so allMail[0] ends up at _papers[0] (front)
            for (int i = allMail.Count - 1; i >= 0; i--)
            {
                var paper = Instantiate(paperPrefab, stackRoot);
                paper.SetActive(true);
                PopulatePaper(paper, allMail[i]);
                _papers.Add(paper);
                _ids[paper] = allMail[i].Id;
            }

            ApplyLayout();
        }

        private void PopulatePaper(GameObject paper, MailMessage msg)
        {
            var labels = paper.GetComponentsInChildren<TMP_Text>(true);
            if (labels.Length >= 1) labels[0].text = msg.Sender;
            if (labels.Length >= 2) labels[1].text = msg.Subject;
            if (labels.Length >= 3) labels[2].text = msg.Body;

            var seal = paper.transform.Find("WaxSeal");
            if (seal != null) seal.gameObject.SetActive(!msg.IsRead);
        }

        private void ApplyLayout()
        {
            for (int i = 0; i < _papers.Count; i++)
            {
                // i=0 is front → last sibling so Unity UI renders it on top
                _papers[i].transform.SetSiblingIndex(_papers.Count - 1 - i);

                var rt    = _papers[i].GetComponent<RectTransform>();
                int depth = Mathf.Min(i, Offsets.Length - 1);
                rt.anchoredPosition = Offsets[depth];
                rt.localRotation    = Quaternion.Euler(0f, 0f, Rotations[depth]);

                // Show the front paper plus 2 peeking behind it; hide the rest
                _papers[i].SetActive(i < 3);
            }
        }

        // ── Navigation ────────────────────────────────────────────────────────

        private IEnumerator SlideToBack()
        {
            if (_papers.Count <= 1) yield break;
            _animating = true;

            var front   = _papers[0];
            var frontRt = front.GetComponent<RectTransform>();
            var nextRt  = _papers[1].GetComponent<RectTransform>();

            Vector2 frontStart  = frontRt.anchoredPosition;
            Vector2 nextStart   = nextRt.anchoredPosition;
            float   nextStartRot = NormalizeAngle(_papers[1].transform.localEulerAngles.z);

            _papers[1].SetActive(true);

            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime / SLIDE_DUR, 1f);
                float s = Mathf.SmoothStep(0f, 1f, t);
                frontRt.anchoredPosition = Vector2.Lerp(frontStart, frontStart + new Vector2(SLIDE_DIST, 0f), s);
                nextRt.anchoredPosition  = Vector2.Lerp(nextStart,  Offsets[0], s);
                nextRt.localRotation     = Quaternion.Euler(0f, 0f, Mathf.Lerp(nextStartRot, Rotations[0], s));
                yield return null;
            }

            _papers.RemoveAt(0);
            _papers.Add(front);
            ApplyLayout();
            _animating = false;
        }

        private IEnumerator BringToFront()
        {
            if (_papers.Count <= 1) yield break;
            _animating = true;

            var bottom   = _papers[^1];
            var bottomRt = bottom.GetComponent<RectTransform>();

            _papers.RemoveAt(_papers.Count - 1);
            _papers.Insert(0, bottom);

            // Start off-screen to the right, slide into front position
            bottomRt.anchoredPosition = new Vector2(SLIDE_DIST, 0f);
            bottomRt.localRotation    = Quaternion.identity;
            bottom.transform.SetAsLastSibling();
            bottom.SetActive(true);

            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime / SLIDE_DUR, 1f);
                float s = Mathf.SmoothStep(0f, 1f, t);
                bottomRt.anchoredPosition = Vector2.Lerp(new Vector2(SLIDE_DIST, 0f), Offsets[0], s);
                yield return null;
            }

            ApplyLayout();
            _animating = false;
        }

        // ── Seal ──────────────────────────────────────────────────────────────

        private void BreakSeal()
        {
            if (_papers.Count == 0) return;
            var front = _papers[0];
            var seal  = front.transform.Find("WaxSeal");
            if (seal == null || !seal.gameObject.activeSelf) return;
            if (!_ids.TryGetValue(front, out var id)) return;

            MailGeneratorDriver.MailboxService?.MarkRead(id);
            StartCoroutine(AnimateSeal(seal.gameObject));
        }

        private static IEnumerator AnimateSeal(GameObject seal)
        {
            var rt  = seal.GetComponent<RectTransform>();
            var img = seal.GetComponent<Image>();
            Color baseColor = img.color;

            float t = 0f;
            while (t < 1f)
            {
                t = Mathf.Min(t + Time.deltaTime / 0.28f, 1f);
                float scale = 1f + Mathf.Sin(t * Mathf.PI) * 0.35f;
                rt.localScale = new Vector3(scale, scale, 1f);
                img.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f - t);
                yield return null;
            }

            seal.SetActive(false);
            rt.localScale = Vector3.one;
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private void ClearStack()
        {
            foreach (var p in _papers) Destroy(p);
            _papers.Clear();
            _ids.Clear();
        }

        private static float NormalizeAngle(float a) => a > 180f ? a - 360f : a;
    }
}
