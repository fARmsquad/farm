using System.Collections;
using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// Add this component to a WeaponAim, and it'll automatically handle switching its weapon aim control mode to mouse if mouse becomes active.
	/// If you then touch any of the gamepad axis again, it'll switch back aim control to it.
	/// The WeaponAim control mode needs to be initially set to a gamepad control mode
	/// </summary>
	[AddComponentMenu("TopDown Engine/Weapons/Weapon Aim Mouse Override")]
	public class WeaponAimMouseOverride : MonoBehaviour
	{
		[Header("Behavior")]
		[MMInformation("Add this component to a WeaponAim, and it'll automatically handle switching its weapon aim control mode to mouse if mouse becomes active. " +
		               "If you then touch any of the gamepad axis again, it'll switch back aim control to it. " +
		               "The WeaponAim control mode needs to be initially set to a gamepad control mode", 
						MoreMountains.Tools.MMInformationAttribute.InformationType.Info, false)]
		
		/// if this is true, mouse position will be evaluated, and if it differs from the one last frame, we'll switch to mouse control mode
		[Tooltip("if this is true, mouse position will be evaluated, and if it differs from the one last frame, we'll switch to mouse control mode")]
		public bool CheckMouse = true;
		/// if this is true, the primary axis will be evaluated, and if it differs from the one last frame, we'll switch back to the initial control mode
		[Tooltip("if this is true, the primary axis will be evaluated, and if it differs from the one last frame, we'll switch back to the initial control mode")]
		public bool CheckPrimaryAxis = true;
		/// if this is true, the secondary axis will be evaluated, and if it differs from the one last frame, we'll switch back to the initial control mode
		[Tooltip("if this is true, the secondary axis will be evaluated, and if it differs from the one last frame, we'll switch back to the initial control mode")]
		public bool CheckSecondaryAxis = true;
		
		protected WeaponAim _weaponAim;
		protected Vector2 _primaryAxisInput;
		protected Vector2 _primaryAxisInputLastFrame;
		protected Vector2 _secondaryAxisInput;
		protected Vector2 _secondaryAxisInputLastFrame;
		protected Vector2 _mouseInput;
		protected Vector2 _mouseInputLastFrame;
		protected WeaponAim.AimControls _initialAimControl;

		/// <summary>
		/// On Awake we store our WeaponAim component and grab our initial aim control mode 
		/// </summary>
		protected virtual void Awake()
		{
			_weaponAim = this.gameObject.GetComponent<WeaponAim>();
			GetInitialAimControl();
		}

		/// <summary>
		/// Sets the current aim control mode as the initial one, that the component will switch back to when going back from mouse mode
		/// </summary>
		public virtual void GetInitialAimControl()
		{
			_initialAimControl = _weaponAim.AimControl;
			if (_weaponAim.AimControl == WeaponAim.AimControls.Mouse)
			{
				Debug.LogWarning(this.gameObject + " : this component requires that you set its associated WeaponAim to a control mode other than Mouse.");
			}
		}

		/// <summary>
		/// On update, checks mouse and axis, and stores last frame's data
		/// </summary>
		protected virtual void Update()
		{
			CheckMouseInput();
			CheckAxisInput();
			StoreLastFrameData();
		}

		/// <summary>
		/// We store our current input data to be able to compare against it next frame
		/// </summary>
		protected virtual void StoreLastFrameData()
		{
			_mouseInputLastFrame = _mouseInput;
			_primaryAxisInputLastFrame = _primaryAxisInput;
			_secondaryAxisInputLastFrame = _secondaryAxisInput;
		}

		/// <summary>
		/// Checks if mouse input has changed, switches to mouse control if that's the case
		/// </summary>
		protected virtual void CheckMouseInput()
		{
			if (!CheckMouse)
			{
				return;
			}

			_mouseInput = _weaponAim.TargetWeapon.Owner.LinkedInputManager.MousePosition;
			if (_mouseInput != _mouseInputLastFrame)
			{
				SwitchToMouse();
			}
		}

		/// <summary>
		/// Checks if axis input has changed, switches back to initial control mode if that's the case
		/// </summary>
		protected virtual void CheckAxisInput()
		{
			if (CheckPrimaryAxis)
			{
				_primaryAxisInput = _weaponAim.TargetWeapon.Owner.LinkedInputManager.PrimaryMovement;
				if (_primaryAxisInput != _primaryAxisInputLastFrame)
				{
					SwitchToInitialControlMode();
				}
			}

			if (CheckSecondaryAxis)
			{
				_secondaryAxisInput = _weaponAim.TargetWeapon.Owner.LinkedInputManager.SecondaryMovement;
				if (_secondaryAxisInput != _secondaryAxisInputLastFrame)
				{
					SwitchToInitialControlMode();
				}
			}
		}
		
		/// <summary>
		/// Changes aim control mode to mouse
		/// </summary>
		public virtual void SwitchToMouse()
		{
			_weaponAim.AimControl = WeaponAim.AimControls.Mouse;
		}

		/// <summary>
		/// Changes aim control mouse to the initial mode
		/// </summary>
		public virtual void SwitchToInitialControlMode()
		{
			_weaponAim.AimControl = _initialAimControl;
		}
	}	
}

