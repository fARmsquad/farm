using System.Collections;
using FarmSimVR.MonoBehaviours.Cinematics;
using UnityEngine;

namespace FarmSimVR.MonoBehaviours.Autoplay
{
    /// <summary>
    /// Autoplay controller for the Town scene.
    /// Automatically walks the player toward the target NPC, triggers an LLM
    /// conversation, then hands control to TownPlayerController for free exploration.
    /// </summary>
    public class TownInteractionAutoplay : AutoplayBase
    {
        private const float WALK_SPEED      = 3.5f;
        private const float ARRIVAL_DISTANCE = 2.5f;
        private const float TURN_SPEED      = 8f;

        [SerializeField] private Transform playerTransform;
        [SerializeField] private NPCController targetNpc;
        [SerializeField] private LLMConversationController conversationController;
        [SerializeField] private TownPlayerController playerController;

        private CharacterController _characterController;
        private float _verticalVelocity;

        protected override IEnumerator RunDemo()
        {
            specId     = "TOWN-001";
            specTitle  = "Town Interaction – Autoplay";
            totalSteps = 4;

            Step("Spotting the town person...");
            yield return TurnTowardNpc(1.2f);

            Step("Walking over to say hello...");
            yield return WalkToNpc();

            Step("Pressing [E] to interact...");
            yield return new WaitForSeconds(0.6f);

            if (targetNpc != null)
                targetNpc.TriggerInteraction();

            Step("Having a chat...");
            yield return WaitForConversationEnd();
            yield return new WaitForSeconds(0.8f);
        }

        /// <summary>
        /// Called by AutoplayBase when the demo finishes.
        /// Enables free-roam player control.
        /// </summary>
        protected override void OnDemoComplete()
        {
            if (playerController != null)
                playerController.EnableControl();
        }

        // ── Walk helpers ─────────────────────────────────────────────────────

        private IEnumerator TurnTowardNpc(float duration)
        {
            if (playerTransform == null || targetNpc == null) yield break;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                FaceTarget(targetNpc.transform.position);
                yield return null;
            }
        }

        private IEnumerator WalkToNpc()
        {
            if (playerTransform == null || targetNpc == null) yield break;

            _characterController = playerTransform.GetComponent<CharacterController>();

            while (true)
            {
                Vector3 toNpc = targetNpc.transform.position - playerTransform.position;
                toNpc.y = 0f;

                if (toNpc.magnitude <= ARRIVAL_DISTANCE) break;

                FaceTarget(targetNpc.transform.position);
                MoveForward();
                yield return null;
            }

            if (_characterController != null)
                _characterController.Move(Vector3.zero);
        }

        private void FaceTarget(Vector3 worldTarget)
        {
            if (playerTransform == null) return;

            Vector3 dir = worldTarget - playerTransform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) return;

            Quaternion target = Quaternion.LookRotation(dir);
            playerTransform.rotation = Quaternion.Slerp(
                playerTransform.rotation, target, TURN_SPEED * Time.deltaTime);
        }

        private void MoveForward()
        {
            if (playerTransform == null) return;

            if (_characterController != null && _characterController.isGrounded)
                _verticalVelocity = -2f;
            else
                _verticalVelocity += Physics.gravity.y * Time.deltaTime;

            Vector3 velocity = playerTransform.forward * WALK_SPEED;
            velocity.y = _verticalVelocity;

            if (_characterController != null)
                _characterController.Move(velocity * Time.deltaTime);
            else
                playerTransform.position += velocity * Time.deltaTime;
        }

        private IEnumerator WaitForConversationEnd()
        {
            if (conversationController == null) { yield return new WaitForSeconds(5f); yield break; }
            yield return null;
            while (conversationController.IsInConversation) yield return null;
        }
    }
}
