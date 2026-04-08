using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Debug overlay for intro props and ambient NPCs (Shift+I).
    /// </summary>
    public class IntroPropsDemo : MonoBehaviour
    {
        private static readonly Key Panel = Key.I;
        private readonly List<GameObject> spawnedProps = new List<GameObject>();

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) OnAttachLantern();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) OnSpawnChicks();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) OnLaunchBoot();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) OnRunCat();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5)) OnRemoveAllProps();
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 300f;
            float h = 220f;
            float x = Screen.width - w - 10f;
            float y = Screen.height - h - 10f;
            float btnH = 28f;
            float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Intro Props (Shift+I)");
            float cy = y + 22f;

            GUI.Label(new Rect(x + 4, cy, w - 8, 20f), $"Spawned Props: {spawnedProps.Count}");
            cy += 24f;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[1] Attach Lantern to Player")) OnAttachLantern();
            cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[2] Spawn Baby Chicks (4)")) OnSpawnChicks();
            cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[3] Launch Boot")) OnLaunchBoot();
            cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[4] Run Cat Across Roof")) OnRunCat();
            cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[5] Remove All Props")) OnRemoveAllProps();
        }

        private void OnAttachLantern()
        {
            var player = FindAnyObjectByType<FirstPersonExplorer>();
            if (player == null)
            {
                Debug.LogWarning("[IntroPropsDemo] FirstPersonExplorer not found.");
                return;
            }

            var lanternGo = new GameObject("Lantern");
            lanternGo.transform.SetParent(player.transform, false);
            lanternGo.AddComponent<LanternHolder>();
            spawnedProps.Add(lanternGo);
            Debug.Log("[IntroPropsDemo] Lantern attached to player.");
        }

        private void OnSpawnChicks()
        {
            Vector3 basePos = new Vector3(72f, 0f, 15f);

            for (int i = 0; i < 4; i++)
            {
                Vector3 offset = new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f));
                Vector3 pos = basePos + offset;

                if (Terrain.activeTerrain != null)
                    pos.y = Terrain.activeTerrain.SampleHeight(pos);

                var chickGo = new GameObject($"BabyChick_{i}");
                chickGo.transform.position = pos;
                chickGo.AddComponent<BabyChick>();
                spawnedProps.Add(chickGo);
            }

            Debug.Log("[IntroPropsDemo] Spawned 4 baby chicks near the farm pen.");
        }

        private void OnLaunchBoot()
        {
            Vector3 startPos = new Vector3(65f, 0f, 20f);
            Vector3 targetPos = new Vector3(72f, 0f, 24f);

            if (Terrain.activeTerrain != null)
            {
                startPos.y = Terrain.activeTerrain.SampleHeight(startPos) + 6f;
                targetPos.y = Terrain.activeTerrain.SampleHeight(targetPos);
            }

            var bootGo = new GameObject("BootProjectile");
            var boot = bootGo.AddComponent<BootProjectile>();
            boot.Launch(startPos, targetPos);
            spawnedProps.Add(bootGo);
            Debug.Log("[IntroPropsDemo] Boot launched!");
        }

        private void OnRunCat()
        {
            Vector3 startPos = new Vector3(68f, 0f, 18f);
            Vector3 endPos = new Vector3(78f, 0f, 18f);

            if (Terrain.activeTerrain != null)
            {
                startPos.y = Terrain.activeTerrain.SampleHeight(startPos) + 8f;
                endPos.y = Terrain.activeTerrain.SampleHeight(endPos) + 8f;
            }

            var runnerGo = new GameObject("RooftopCat");
            var runner = runnerGo.AddComponent<RooftopRunner>();
            runner.Run(startPos, endPos);
            spawnedProps.Add(runnerGo);
            Debug.Log("[IntroPropsDemo] Cat running across roof!");
        }

        private void OnRemoveAllProps()
        {
            int count = spawnedProps.Count;
            foreach (var prop in spawnedProps)
            {
                if (prop != null) Destroy(prop);
            }
            spawnedProps.Clear();
            Debug.Log($"[IntroPropsDemo] Removed {count} props.");
        }
    }
}
