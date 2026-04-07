using System;
using MoreMountains.Tools;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// This action lets your AI agent dash when performed
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Dash")]
	public class AIActionDash : AIAction
	{
		/// the direction the dash should occur in
		public enum Modes { TowardsTarget, AwayFromTarget, None }

		[Header("Dash")]
		/// the direction the dash should occur in
		[Tooltip("the direction the dash should occur in")]
		public Modes Mode = Modes.TowardsTarget;
		/// whether or not the dash mode should be setup to Script automatically on the dash ability
		[Tooltip("whether or not the dash mode should be setup to Script automatically on the dash ability")] 
		public bool AutoSetupDashMode = true;
		
		protected CharacterDash2D _characterDash2D;
		protected CharacterDash3D _characterDash3D;
		
		/// <summary>
		/// On initialization we grab our dash ability and auto set it up if needed
		/// </summary>
		public override void Initialization()
		{
			if(!ShouldInitialize) return;
			base.Initialization();
			_characterDash2D = this.gameObject.GetComponentInParent<CharacterDash2D>();
			_characterDash3D = this.gameObject.GetComponentInParent<CharacterDash3D>();
			if (AutoSetupDashMode)
			{
				if (_characterDash2D != null)
				{
					_characterDash2D.DashMode = CharacterDash2D.DashModes.Script;
				}
				if (_characterDash3D != null)
				{
					_characterDash3D.DashMode = CharacterDash3D.DashModes.Script;
				}
			}
		}
		
		/// <summary>
		/// On PerformAction we set the dash direction and start the dash
		/// </summary>
		public override void PerformAction()
		{
			if (_characterDash2D != null)
			{
				if (_brain.Target != null)
				{
					switch (Mode)
					{
						case Modes.TowardsTarget:
							_characterDash2D.DashDirection = (_brain.Target.transform.position - this.transform.position).normalized;
							break;
						case Modes.AwayFromTarget:
							_characterDash2D.DashDirection = (this.transform.position - _brain.Target.transform.position).normalized;
							break;
					}	
				}
				_characterDash2D.DashStart();
			}
			else if (_characterDash3D != null)
			{
				if (_brain.Target != null)
				{
					switch (Mode)
					{
						case Modes.TowardsTarget:
							_characterDash3D.DashDirection = (_brain.Target.transform.position - this.transform.position).normalized;
							break;
						case Modes.AwayFromTarget:
							_characterDash3D.DashDirection = (this.transform.position - _brain.Target.transform.position).normalized;
							break;
					}	
				}
				_characterDash3D.DashStart();
			}
		}
	}
}