using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// Aims the weapon at the current target
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/AI/Actions/AI Action Aim Weapon At Target 2D")]
	public class AIActionAimWeaponAtTarget2D : AIAction
	{
		public enum AimOrigins { Transform, SpawnPoint }
        
		[Header("Binding")] 
		/// the CharacterHandleWeapon ability this AI action should pilot. If left blank, the system will grab the first one it finds.
		[Tooltip("the CharacterHandleWeapon ability this AI action should pilot. If left blank, the system will grab the first one it finds.")]
		public CharacterHandleWeapon TargetHandleWeaponAbility;

		[Header("Behaviour")] 
		/// the origin we'll take into account when computing the aim direction towards the target
		[Tooltip("the origin we'll take into account when computing the aim direction towards the target")]
		public AimOrigins AimOrigin = AimOrigins.Transform;
		/// if true, the Character will face the target (left/right)
		[Tooltip("if true, the Character will face the target (left/right)")]
		public bool FaceTarget = true;
		/// if true the Character will aim at the target
		[Tooltip("if true the Character will aim at the target")]
		public bool AimAtTarget = true;

		protected CharacterOrientation2D _orientation2D;
		protected Character _character;
		protected WeaponAim _weaponAim;
		protected ProjectileWeapon _projectileWeapon;
		protected Vector3 _weaponAimDirection;

		/// <summary>
		/// On init we grab our CharacterHandleWeapon ability
		/// </summary>
		public override void Initialization()
		{
			if(!ShouldInitialize) return;
			base.Initialization();
			_character = GetComponentInParent<Character>();
			_orientation2D = _character?.FindAbility<CharacterOrientation2D>();
			if (TargetHandleWeaponAbility == null)
			{
				TargetHandleWeaponAbility = _character?.FindAbility<CharacterHandleWeapon>();
			}
		}

		/// <summary>
		/// On PerformAction we face and aim if needed, and we shoot
		/// </summary>
		public override void PerformAction()
		{
			if (_brain.Target == null)
			{
				return;
			}
			TestFaceTarget();
			TestAimAtTarget();
		}

		/// <summary>
		/// Faces the target if required
		/// </summary>
		protected virtual void TestFaceTarget()
		{
			if (!FaceTarget)
			{
				return;
			}

			if (this.transform.position.x > _brain.Target.position.x)
			{
				_orientation2D.FaceDirection(-1);
			}
			else
			{
				_orientation2D.FaceDirection(1);
			}            
		}

		/// <summary>
		/// Aims at the target if required
		/// </summary>
		protected virtual void TestAimAtTarget()
		{
			if (!AimAtTarget)
			{
				return;
			}

			if (TargetHandleWeaponAbility.CurrentWeapon != null)
			{
				if (_weaponAim == null)
				{
					_weaponAim = TargetHandleWeaponAbility.CurrentWeapon.gameObject.MMGetComponentNoAlloc<WeaponAim>();
				}

				if (_weaponAim != null)
				{
					if ((AimOrigin == AimOrigins.SpawnPoint) && (_projectileWeapon != null))
					{
						_projectileWeapon.DetermineSpawnPosition();
						_weaponAimDirection = _brain.Target.position - _projectileWeapon.SpawnPosition;
					}
					else
					{
						_weaponAimDirection = _brain.Target.position - _character.transform.position;
					}                    
				}                
			}
			
			_weaponAim.SetCurrentAim(_weaponAimDirection);
		}

	}
}