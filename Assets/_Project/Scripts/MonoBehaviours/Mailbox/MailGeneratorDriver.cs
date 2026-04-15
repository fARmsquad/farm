using System.Collections.Generic;
using FarmSimVR.Core.Mailbox;
using FarmSimVR.MonoBehaviours.Cinematics;
using FarmSimVR.MonoBehaviours.Farming;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Mailbox
{
    /// <summary>
    /// Owns the MailboxService singleton and triggers LLM mail generation on each new day.
    /// Also generates a batch on startup for day 0.
    /// </summary>
    public class MailGeneratorDriver : MonoBehaviour
    {
        [SerializeField] private MailLLMClient llmClient;

        public static MailboxService MailboxService { get; private set; }

        private void Awake()
        {
            MailboxService = new MailboxService();
        }

        [Header("Debug")]
        [SerializeField] private bool seedTestMailOnStart = false;

        private void Start()
        {
            if (FarmDayClockDriver.Instance != null)
                FarmDayClockDriver.Instance.Clock.OnNewDay += OnNewDay;

            if (seedTestMailOnStart)
                SeedTestMail();

            GenerateMail(0);
        }

        private void SeedTestMail()
        {
            MailboxService.AddMail(new MailMessage(
                "Valley Seeds Co.",
                "BIGGEST SEEDS IN THE VALLEY!!!",
                "Dear Valued Farmer, our seeds are so big that other seeds feel embarrassed just sitting next to them. Order today and receive a free hat (hat not included).",
                MailType.Junk));

            MailboxService.AddMail(new MailMessage(
                "A Friendly Stranger",
                "Hello from the road",
                "I passed by your farm on my travels and wanted to say — the flowers by your fence looked simply wonderful. Keep it up. The valley is lucky to have you.",
                MailType.Real,
                new MailAttachment("seed_sunflower", 3)));
        }

        private void OnDestroy()
        {
            if (FarmDayClockDriver.Instance != null)
                FarmDayClockDriver.Instance.Clock.OnNewDay -= OnNewDay;
        }

        private void OnNewDay(int dayCount) => GenerateMail(dayCount);

        private void GenerateMail(int dayNumber)
        {
            if (llmClient == null)
            {
                Debug.LogWarning("[MailGeneratorDriver] No MailLLMClient assigned — skipping generation.");
                return;
            }

            var npcNames = new List<string>();
            foreach (var npc in FindObjectsByType<NPCController>(FindObjectsSortMode.None))
                npcNames.Add(npc.NpcName);

            StartCoroutine(llmClient.GenerateMail(
                dayNumber,
                npcNames,
                onComplete: messages =>
                {
                    foreach (var m in messages)
                        MailboxService.AddMail(m);
                    Debug.Log($"[MailGeneratorDriver] {messages.Count} letters delivered for day {dayNumber + 1}.");
                },
                onError: err => Debug.LogWarning($"[MailGeneratorDriver] Generation failed: {err}")
            ));
        }
    }
}
