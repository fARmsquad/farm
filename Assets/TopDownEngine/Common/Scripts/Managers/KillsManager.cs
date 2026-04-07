using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
#if MM_TEXTMESHPRO
using TMPro;
#endif

namespace MoreMountains.TopDownEngine
{
	public class KillsManager : MMSingleton<KillsManager>, MMEventListener<MMLifeCycleEvent>
	{
		public enum Modes { Layer, List }
		
		public Modes Mode = Modes.Layer;

		[Header("List Mode")]
		/// a list of Health components on the targets. Once all these targets are dead, OnLastDeath will trigger
		[Tooltip("a list of Health components on the targets. Once all these targets are dead, OnLastDeath will trigger")]
		public List<Health> TargetsList;
		
		[Header("Layer Mode")]
		/// the layer(s) on which the dying Health components should be to be counted - typically Enemies
		[Tooltip("the layer(s) on which the dying Health components should be to be counted - typically Enemies")]
		public LayerMask TargetLayerMask = LayerManager.EnemiesLayerMask;
		/// in Layer mode, if AutoSetKillThreshold is true, the KillThreshold will be automatically computed on start, based on the total number of potential targets in the level matching the target layer mask 
		[Tooltip("in Layer mode, if AutoSetKillThreshold is true, the KillThreshold will be automatically computed on start, based on the total number of potential targets in the level matching the target layer mask")]
		public bool AutoSetKillThreshold = true;

		[Header("Counters")]
		/// The maximum amount of kills needed to trigger OnLastDeath
		[Tooltip("The maximum amount of kills needed to trigger OnLastDeath")]
		public int DeathThreshold = 5;
		/// The amount of deaths remaining to trigger OnLastDeath. Read only value
		[Tooltip("The amount of deaths remaining to trigger OnLastDeath. Read only value")]
		[MMReadOnly]
		public int RemainingDeaths = 0;
		
		[Header("Events")]
		/// An event that gets triggered on every death
		[Tooltip("An event that gets triggered on every death")]
		public UnityEvent OnDeath;
		/// An event that gets triggered when the last death occurs
		[Tooltip("An event that gets triggered when the last death occurs")]
		public UnityEvent OnLastDeath;

		[Header("Text displays")] 
		/// An optional text counter displaying the total amount of deaths required before OnLastDeath triggers
		[Tooltip("An optional text counter displaying the total amount of deaths required before OnLastDeath triggers")]
		public Text TotalCounter;
		/// An optional text counter displaying the remaining amount of deaths before OnLastDeath
		[Tooltip("An optional text counter displaying the remaining amount of deaths before OnLastDeath")]
		public Text RemainingCounter;
		#if MM_TEXTMESHPRO
		/// An optional text counter displaying the total amount of deaths required before OnLastDeath triggers
		[Tooltip("An optional text counter displaying the total amount of deaths required before OnLastDeath triggers")]
		public TMP_Text TotalCounter_TMP;
		/// An optional text counter displaying the remaining amount of deaths before OnLastDeath
		[Tooltip("An optional text counter displaying the remaining amount of deaths before OnLastDeath")]
		public TMP_Text RemainingCounter_TMP;
		#endif
		
		/// <summary>
		/// Statics initialization to support enter play modes
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		protected static void InitializeStatics()
		{
			_instance = null;
		}

		/// <summary>
		/// On start we initialize our remaining deaths counter
		/// </summary>
		protected virtual void Start()
		{
			if (AutoSetKillThreshold)
			{
				ComputeKillThresholdBasedOnTargetLayerMask();
			}
			RefreshRemainingDeaths();
		}

		/// <summary>
		/// Use this method to change the current death threshold. This will also update the remaining death counter
		/// </summary>
		/// <param name="newThreshold"></param>
		public virtual void RefreshRemainingDeaths()
		{
			if (Mode == Modes.List)
			{
				DeathThreshold = TargetsList.Count;
			}
			
			RemainingDeaths = DeathThreshold;

			UpdateTexts();
		}

		/// <summary>
		/// Computes the required death threshold based by counting objects on the selected layer mask
		/// </summary>
		public virtual void ComputeKillThresholdBasedOnTargetLayerMask()
		{
			DeathThreshold = 0;
			Health[] healths = FindObjectsByType<Health>(FindObjectsSortMode.None);
			foreach (Health health in healths)
			{
				if (TargetLayerMask.MMContains(health.gameObject))
				{
					DeathThreshold++;
				}
			}
		}
		
		/// <summary>
		/// When we get a death even
		/// </summary>
		/// <param name="lifeCycleEvent"></param>
		public virtual void OnMMEvent(MMLifeCycleEvent lifeCycleEvent)
		{
			if (lifeCycleEvent.MMLifeCycleEventType != MMLifeCycleEventTypes.Death)
			{
				return;
			}
			
			// we check if we still need to track events
			if (RemainingDeaths <= 0)
			{
				return;
			}
			
			// we check the layer
			if (!TargetLayerMask.MMContains(lifeCycleEvent.AffectedHealth.gameObject.layer))
			{
				return;
			}
			
			// if in List mode, we make sure the dead is part of the kill list
			if (Mode == Modes.List)
			{
				if (!TargetsList.Contains(lifeCycleEvent.AffectedHealth))
				{
					return;
				}
			}
			
			// we trigger our OnDeath event
			OnDeath?.Invoke();
			RemainingDeaths--;

			UpdateTexts();
			
			// if needed, we trigger our last death event
			if (RemainingDeaths <= 0)
			{
				OnLastDeath?.Invoke();
			}
		}

		/// <summary>
		/// Updates the bound texts if necessary
		/// </summary>
		protected virtual void UpdateTexts()
		{
			if (TotalCounter != null)
			{
				TotalCounter.text = DeathThreshold.ToString();
			}

			if (RemainingCounter != null)
			{
				RemainingCounter.text = RemainingDeaths.ToString();
			}
			
			#if MM_TEXTMESHPRO
				if (TotalCounter_TMP != null)
				{
					TotalCounter_TMP.text = DeathThreshold.ToString();
				}

				if (RemainingCounter_TMP != null)
				{
					RemainingCounter_TMP.text = RemainingDeaths.ToString();
				}
			#endif
		}
		
		/// <summary>
		/// OnDisable, we start listening to events.
		/// </summary>
		protected virtual void OnEnable()
		{
			this.MMEventStartListening<MMLifeCycleEvent>();
		}

		/// <summary>
		/// OnDisable, we stop listening to events.
		/// </summary>
		protected virtual void OnDisable()
		{
			this.MMEventStopListening<MMLifeCycleEvent>();
		}
	}
}
