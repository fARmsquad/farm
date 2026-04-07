using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;
using MoreMountains.FeedbacksForThirdParty;

namespace MoreMountains.TopDownEngine
{
	public class AutoBindAutoFocus : TopDownMonoBehaviour, MMEventListener<MMCameraEvent>
	{
		/// the AutoFocus component on the camera
		public virtual MMAutoFocus AutoFocus { get; set; }
		
		#if MM_URP
		public virtual MMAutoFocus_URP AutoFocusURP { get; set; }
		#endif

		protected virtual void Start()
		{
			AutoFocus = FindAnyObjectByType<MMAutoFocus>();
			#if MM_URP
			AutoFocusURP = FindAnyObjectByType<MMAutoFocus_URP>();
			#endif
		}
        
		public virtual void OnMMEvent(MMCameraEvent cameraEvent)
		{
			switch (cameraEvent.EventType)
			{
				case MMCameraEventTypes.StartFollowing:
					AutoBindAutoFocusToCamera();
					break;
				case MMCameraEventTypes.RefreshAutoFocus:
					AutoBindAutoFocusToCamera();
					break;
			}
		}
		
		protected virtual void AutoBindAutoFocusToCamera()
		{
			if (AutoFocus == null)
			{
				AutoFocus = FindAnyObjectByType<MMAutoFocus>();
			}
			if (AutoFocus != null)
			{
				AutoFocus.FocusTargets = new Transform[1];
				AutoFocus.FocusTargets[0] = LevelManager.Instance.Players[0].transform; 
			}
			
			#if MM_URP
			if (AutoFocusURP == null)
			{
				AutoFocusURP = FindAnyObjectByType<MMAutoFocus_URP>();
			}
			if (AutoFocusURP != null)
			{
				AutoFocusURP.FocusTargets = new Transform[1];
				AutoFocusURP.FocusTargets[0] = LevelManager.Instance.Players[0].transform; 
			}
			#endif
		}

		protected virtual void OnEnable()
		{
			this.MMEventStartListening<MMCameraEvent>();
		}

		protected virtual void OnDisable()
		{
			this.MMEventStopListening<MMCameraEvent>();
		}
	}
}