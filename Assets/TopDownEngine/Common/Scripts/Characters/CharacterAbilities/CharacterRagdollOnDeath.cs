using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using System.Collections.Generic;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// Add this to a character and it'll trigger its MMRagdoller to ragdoll on death
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/Abilities/Character Ragdoll on Death")]
	public class CharacterRagdollOnDeath : TopDownMonoBehaviour
	{
		[Header("Binding")]
		/// The MMRagdoller for this character
		[Tooltip("the MMRagdoller for this character")]
		public MMRagdoller Ragdoller;
		/// A list of optional objects to disable on death
		[Tooltip("A list of optional objects to disable on death")]
		public List<GameObject> ObjectsToDisableOnDeath;
		/// A list of optional monos to disable on death
		[Tooltip("A list of optional monos to disable on death")]
		public List<MonoBehaviour> MonosToDisableOnDeath;

		[Header("Force")]
		/// the force by which the impact will be multiplied
		[Tooltip("the force by which the impact will be multiplied")]
		public float ForceMultiplier = 10000f;

		[Header("Test")]
		/// A test button to trigger the ragdoll from the inspector
		[MMInspectorButton("Ragdoll")]
		[Tooltip("A test button to trigger the ragdoll from the inspector")]
		public bool RagdollButton;
		/// A test button to reset the ragdoll from the inspector
		[MMInspectorButton("ResetRagdoll")]
		[Tooltip("A test button to reset the ragdoll from the inspector")]
		public bool ResetRagdollButton;
        
		protected TopDownController _controller;
		protected Health _health;
		protected Transform _initialParent;
		protected Vector3 _initialPosition;
		protected Quaternion _initialRotation;
		protected Character _character;
        
		/// <summary>
		/// On Awake we initialize our component
		/// </summary>
		protected virtual void Start()
		{
			Initialization();
		}

		/// <summary>
		/// Grabs our health and controller
		/// </summary>
		protected virtual void Initialization()
		{
			if (_health == null)
			{
				GrabHealth();
			}
			_controller = this.gameObject.GetComponent<TopDownController>();
			_initialParent = Ragdoller.transform.parent;
			_initialPosition = Ragdoller.transform.localPosition;
			_initialRotation = Ragdoller.transform.localRotation;
		}

		protected virtual void GrabHealth()
		{
			_character = this.gameObject.GetComponentInParent<Character>();
			_health = (_character != null) ? _character.CharacterHealth : this.gameObject.GetComponent<Health>();
			if (_health != null)
			{
				_health.OnDeath += OnDeath;
				_health.OnRevive += OnRevive;
			}
		}

		/// <summary>
		/// When we get a OnDeath event, we ragdoll
		/// </summary>
		protected virtual void OnDeath()
		{
			Ragdoll();
		}

		protected virtual void OnRevive()
		{
			this.transform.position = Ragdoller.GetPosition();
			ResetRagdoll();
		}

		/// <summary>
		/// Disables the specified objects and monos and triggers the ragdoll
		/// </summary>
		protected virtual void Ragdoll()
		{
			foreach (GameObject go in ObjectsToDisableOnDeath)
			{
				go.SetActive(false);
			}
			foreach (MonoBehaviour mono in MonosToDisableOnDeath)
			{
				mono.enabled = false;
			}
			Ragdoller.Ragdolling = true;
			Ragdoller.transform.SetParent(null);
			Ragdoller.MainRigidbody.AddForce(_controller.AppliedImpact.normalized * ForceMultiplier, ForceMode.Acceleration);
		}

		public virtual void ResetRagdoll()
		{
			Ragdoller.AllowBlending = false;
			
			foreach (GameObject go in ObjectsToDisableOnDeath)
			{
				go.SetActive(true);
			}
			foreach (MonoBehaviour mono in MonosToDisableOnDeath)
			{
				mono.enabled = true;
			}
			
			Ragdoller.transform.SetParent(_initialParent);
			Ragdoller.Ragdolling = false;
			Ragdoller.transform.localPosition = _initialPosition;
			Ragdoller.transform.localRotation = _initialRotation;
		}
        
		/// <summary>
		/// OnDestroy we stop listening to OnDeath events
		/// </summary>
		protected virtual void OnDestroy()
		{
			if (_health != null)
			{
				_health.OnDeath -= OnDeath;
				_health.OnRevive -= OnRevive;
			}
		}
	}
}