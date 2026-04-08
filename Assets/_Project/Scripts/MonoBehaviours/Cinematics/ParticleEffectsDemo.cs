using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using FarmSimVR.MonoBehaviours.Debugging;

namespace FarmSimVR.MonoBehaviours.Cinematics
{
    /// <summary>
    /// Debug overlay for spawning and testing runtime particle systems.
    /// Toggle with Shift+P. Number keys trigger actions while the panel is active.
    /// </summary>
    public class ParticleEffectsDemo : MonoBehaviour
    {
        private readonly List<ParticleController> spawnedControllers = new List<ParticleController>();
        private static readonly Key Panel = Key.P;

        private void Update()
        {
            if (!DebugPanelShortcuts.UpdateToggle(Panel)) return;

            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit1)) OnSpawnFireflies();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit2)) OnSpawnChimneySmoke();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit3)) OnSpawnDewSparkle();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit4)) OnSpawnDustMotes();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit5)) OnStopAll();
            if (DebugPanelShortcuts.WasActionPressed(Panel, Key.Digit6)) OnRemoveAll();
        }

        private void OnGUI()
        {
            if (!DebugPanelShortcuts.IsPanelActive(Panel)) return;

            float w = 280f;
            float h = 260f;
            float x = (Screen.width - w) / 2f;
            float y = Screen.height - h - 10f;
            float btnH = 28f;
            float pad = 3f;

            GUI.Box(new Rect(x, y, w, h), "Particle Effects (Shift+P)");
            float cy = y + 22f;

            GUI.Label(new Rect(x + 4, cy, w - 8, 20f), $"Spawned systems: {spawnedControllers.Count}");
            cy += 24f;

            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[1] Spawn Fireflies"))      OnSpawnFireflies();     cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[2] Spawn Chimney Smoke"))   OnSpawnChimneySmoke();  cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[3] Spawn Dew Sparkle"))     OnSpawnDewSparkle();    cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[4] Spawn Dust Motes"))      OnSpawnDustMotes();     cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[5] Stop All Particles"))    OnStopAll();            cy += btnH + pad;
            if (GUI.Button(new Rect(x + 4, cy, w - 8, btnH), "[6] Remove All Particles"))  OnRemoveAll();
        }

        // ── Spawn helpers ───────────────────────────────────────────

        private void OnSpawnFireflies()
        {
            Vector3 pos = TerrainPos(60f, 1f, 30f);

            var go = new GameObject("Fireflies");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles = 10;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 4f);
            main.startSpeed = 0.3f;
            main.startSize = 0.15f;
            main.startColor = new Color(1f, 0.9f, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 3f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 3f;
            shape.angle = 60f;

            // Color over lifetime: yellow → transparent
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, 0.9f, 0.3f), 0f), new GradientColorKey(new Color(1f, 0.9f, 0.3f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            var pc = go.AddComponent<ParticleController>();
            spawnedControllers.Add(pc);
            Debug.Log("[Particles] Spawned Fireflies");
        }

        private void OnSpawnChimneySmoke()
        {
            Vector3 pos = TerrainPos(72f, 8f, 20f);

            var go = new GameObject("ChimneySmoke");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles = 20;
            main.startLifetime = 4f;
            main.startSpeed = 0.8f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.6f, 0.6f, 0.6f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.radius = 0.3f;
            shape.angle = 15f;
            // Cone points upward by default (local Y)

            // Size over lifetime: grow
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(1f, 2f)
            ));

            // Color over lifetime: grey → transparent
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0f), new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 1f) },
                new[] { new GradientAlphaKey(0.6f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            col.color = new ParticleSystem.MinMaxGradient(gradient);

            var pc = go.AddComponent<ParticleController>();
            spawnedControllers.Add(pc);
            Debug.Log("[Particles] Spawned Chimney Smoke");
        }

        private void OnSpawnDewSparkle()
        {
            Vector3 pos = TerrainPos(55f, 0.1f, 25f);

            var go = new GameObject("DewSparkle");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles = 15;
            main.startLifetime = 0.3f;
            main.startSpeed = 0f;
            main.startSize = 0.08f;
            main.startColor = Color.white;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 40f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(4f, 0.02f, 4f);

            var pc = go.AddComponent<ParticleController>();
            spawnedControllers.Add(pc);
            Debug.Log("[Particles] Spawned Dew Sparkle");
        }

        private void OnSpawnDustMotes()
        {
            Vector3 pos = TerrainPos(60f, 3f, 30f);

            var go = new GameObject("DustMotes");
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.maxParticles = 20;
            main.startLifetime = 8f;
            main.startSpeed = 0.05f;
            main.startSize = 0.04f;
            main.startColor = new Color(1f, 1f, 1f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 2.5f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5f, 3f, 5f);

            var pc = go.AddComponent<ParticleController>();
            spawnedControllers.Add(pc);
            Debug.Log("[Particles] Spawned Dust Motes");
        }

        // ── Bulk actions ────────────────────────────────────────────

        private void OnStopAll()
        {
            foreach (var pc in spawnedControllers)
                if (pc != null) pc.Stop();
            Debug.Log("[Particles] Stopped all particle systems");
        }

        private void OnRemoveAll()
        {
            foreach (var pc in spawnedControllers)
                if (pc != null) Destroy(pc.gameObject);
            spawnedControllers.Clear();
            Debug.Log("[Particles] Removed all particle systems");
        }

        // ── Terrain utility ─────────────────────────────────────────

        /// <summary>
        /// Returns a world position with terrain-relative Y offset.
        /// Falls back to yOffset above world origin if no terrain exists.
        /// </summary>
        private static Vector3 TerrainPos(float x, float yOffset, float z)
        {
            float terrainY = Terrain.activeTerrain != null
                ? Terrain.activeTerrain.SampleHeight(new Vector3(x, 0f, z))
                : 0f;
            return new Vector3(x, terrainY + yOffset, z);
        }
    }
}
