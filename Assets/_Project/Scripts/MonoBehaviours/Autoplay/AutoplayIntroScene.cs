using System.Collections;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    /// <summary>
    /// Orchestrates the full intro flow within WorldMain:
    /// 1. Cinematic sequence (5 panels, ~70s)
    /// 2. Gameplay: player chases El Pollo Loco in the pen
    /// 3. On catch: mission complete, auto-save
    ///
    /// Attach to WorldMain scene. Disables FirstPersonExplorer during
    /// cinematic, re-enables for gameplay, spawns El Pollo + chicks
    /// in the pen when gameplay begins.
    /// </summary>
    public class AutoplayIntroScene : MonoBehaviour
    {
        // Pen center and radius match WorldSceneBuilder
        private static readonly Vector3 PenCenter = new(72f, 0.5f, 24f);
        private const float PenRadius = 8f;

        private ElPolloController elPollo;
        private ChaosMeter chaosMeter;
        private Transform playerTransform;
        private bool gameplayActive;

        private IEnumerator Start()
        {
            yield return null;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Disable player movement during cinematic
            var player = FindAnyObjectByType<FirstPersonExplorer>();
            if (player != null)
            {
                playerTransform = player.transform;
                player.enabled = false;
            }

            // Build and play cinematic
            var sequence = IntroSequenceBuilder.Build();

            var sequencer = FindAnyObjectByType<CinematicSequencer>();
            if (sequencer == null)
            {
                Destroy(sequence);
                Debug.LogError("[IntroScene] No CinematicSequencer found.");
                yield break;
            }

            // Set up camera paths on the cinematic camera
            var cineCam = FindAnyObjectByType<CinematicCamera>();
            if (cineCam != null)
                cineCam.EnableCinematicCamera();

            // Activate skip prompt
            var skip = FindAnyObjectByType<SkipPrompt>();
            void HandleSkip()
            {
                if (sequencer.IsPlaying)
                    sequencer.Skip();
            }

            if (skip != null)
            {
                skip.OnSkipRequested.AddListener(HandleSkip);
                skip.Activate();
            }

            // Play cinematic
            bool cinematicDone = false;
            sequencer.OnSequenceComplete.AddListener(() => cinematicDone = true);
            sequencer.Play(sequence);

            while (!cinematicDone)
                yield return null;

            // ── Cinematic done — start gameplay ──
            if (skip != null)
            {
                skip.OnSkipRequested.RemoveListener(HandleSkip);
                skip.Deactivate();
            }
            Destroy(sequence);

            // Switch back to gameplay camera
            if (cineCam != null)
                cineCam.EnableGameplayCamera();

            // Position player at pen gate
            if (playerTransform != null)
            {
                float gateY = Terrain.activeTerrain != null
                    ? Terrain.activeTerrain.SampleHeight(new Vector3(64f, 0, 24f)) + 1.5f
                    : 1.5f;
                playerTransform.position = new Vector3(64f, gateY, 24f);
                playerTransform.rotation = Quaternion.Euler(0, 90, 0); // face pen
            }

            // Spawn El Pollo Loco in the pen
            SpawnElPollo();

            // Spawn baby chicks
            SpawnChicks(4);

            // Create chaos meter
            var meterGo = new GameObject("ChaosMeter");
            chaosMeter = meterGo.AddComponent<ChaosMeter>();

            // Wire El Pollo to mission complete
            var missionMgr = MissionManager.Instance ?? FindAnyObjectByType<MissionManager>();
            if (elPollo != null && missionMgr != null)
            {
                elPollo.OnCaught.AddListener(() =>
                {
                    missionMgr.CompleteMission();
                    gameplayActive = false;
                    Debug.Log("[IntroScene] El Pollo Loco caught! Mission complete.");
                    AutoSave.SaveIntroComplete(6.25f);
                });
            }

            gameplayActive = true;
        }

        private void Update()
        {
            if (!gameplayActive || elPollo == null || chaosMeter == null) return;

            // Fill chaos meter based on player proximity to El Pollo
            if (playerTransform != null)
            {
                float dist = Vector3.Distance(playerTransform.position, elPollo.transform.position);
                float proximityFill = dist < 3f ? 0.15f : dist < 6f ? 0.05f : 0f;
                chaosMeter.AddFill(proximityFill * Time.deltaTime);

                // Drive El Pollo phase from meter
                elPollo.UpdateFromChaosMeter(chaosMeter.CurrentFill);

                // Dodge when player gets close during Dodge phase
                if (dist < 3f && elPollo.CurrentPhase == ElPolloPhase.Dodge)
                    elPollo.DodgeAwayFrom(playerTransform.position);
            }

            // Catch attempt — press E when close and tired
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame && elPollo.IsCatchable)
            {
                float dist = playerTransform != null
                    ? Vector3.Distance(playerTransform.position, elPollo.transform.position)
                    : float.MaxValue;
                if (dist < 2.5f)
                    elPollo.Catch();
            }
        }

        private void SpawnElPollo()
        {
            var go = new GameObject("ElPolloLoco");
            float y = Terrain.activeTerrain != null
                ? Terrain.activeTerrain.SampleHeight(PenCenter) + 0.5f
                : 0.5f;
            go.transform.position = new Vector3(PenCenter.x, y, PenCenter.z);

            elPollo = go.AddComponent<ElPolloController>();

            // Placeholder visual — white capsule (replaced with chicken.glb in INT-014)
            var vis = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            vis.name = "PolloVisual";
            vis.transform.SetParent(go.transform);
            vis.transform.localPosition = Vector3.zero;
            vis.transform.localScale = new Vector3(0.5f, 0.6f, 0.5f);
            vis.GetComponent<Renderer>().material.color = Color.white;

            // Red comb on top
            var comb = GameObject.CreatePrimitive(PrimitiveType.Cube);
            comb.name = "Comb";
            comb.transform.SetParent(go.transform);
            comb.transform.localPosition = new Vector3(0, 0.7f, 0.15f);
            comb.transform.localScale = new Vector3(0.15f, 0.25f, 0.08f);
            comb.GetComponent<Renderer>().material.color = Color.red;
        }

        private void SpawnChicks(int count)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 rnd = Random.insideUnitCircle * (PenRadius * 0.6f);
                Vector3 pos = PenCenter + new Vector3(rnd.x, 0, rnd.y);
                float y = Terrain.activeTerrain != null
                    ? Terrain.activeTerrain.SampleHeight(pos) + 0.15f
                    : 0.15f;
                pos.y = y;

                var go = new GameObject($"BabyChick_{i}");
                go.transform.position = pos;
                go.AddComponent<BabyChick>();
            }
        }
    }
}
