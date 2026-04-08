using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FarmSimVR.MonoBehaviours.Cinematics;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    public class AutoplayParticles : AutoplayBase
    {
        private readonly List<ParticleController> spawnedControllers = new List<ParticleController>();

        private void Awake()
        {
            specId = "INT-010";
            specTitle = "Environment Particle Systems";
            totalSteps = 5;
        }

        protected override IEnumerator RunDemo()
        {
            Step("Spawning fireflies");
            SpawnFireflies();
            yield return Wait(3f);

            Step("Spawning chimney smoke");
            SpawnChimneySmoke();
            yield return Wait(3f);

            Step("Spawning dew sparkle");
            SpawnDewSparkle();
            yield return Wait(3f);

            Step("Setting intensity to 50%");
            foreach (var pc in spawnedControllers)
                if (pc != null) pc.SetIntensity(0.5f);
            Debug.Log("[AutoplayParticles] All controllers set to 50% intensity.");
            yield return Wait(2f);

            Step("Stopping all particles");
            foreach (var pc in spawnedControllers)
                if (pc != null) pc.Stop();
            Debug.Log("[AutoplayParticles] All particle systems stopped.");
            yield return Wait(2f);
        }

        private void SpawnFireflies()
        {
            var go = new GameObject("Fireflies");
            go.transform.position = new Vector3(5f, 2f, 5f);
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
            Debug.Log("[AutoplayParticles] Spawned Fireflies at (5,2,5).");
        }

        private void SpawnChimneySmoke()
        {
            var go = new GameObject("ChimneySmoke");
            go.transform.position = new Vector3(0f, 8f, 0f);
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

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f),
                new Keyframe(1f, 2f)
            ));

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
            Debug.Log("[AutoplayParticles] Spawned Chimney Smoke at (0,8,0).");
        }

        private void SpawnDewSparkle()
        {
            var go = new GameObject("DewSparkle");
            go.transform.position = new Vector3(-3f, 0.1f, 3f);
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
            Debug.Log("[AutoplayParticles] Spawned Dew Sparkle at (-3,0.1,3).");
        }
    }
}
