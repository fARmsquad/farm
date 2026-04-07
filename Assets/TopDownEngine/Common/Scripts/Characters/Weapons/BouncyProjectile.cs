using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using MoreMountains.Feedbacks;

namespace MoreMountains.TopDownEngine
{	
	/// <summary>
	/// Projectile class that will bounce off walls instead of exploding on impact
	/// </summary>
	[AddComponentMenu("TopDown Engine/Weapons/Bouncy Projectile")]
	public class BouncyProjectile : Projectile 
	{
		[Header("Bounciness Tech")]
		/// the length of the raycast used to detect bounces, should be proportionate to the size and speed of your projectile
		[Tooltip("the length of the raycast used to detect bounces, should be proportionate to the size and speed of your projectile")]
		public float BounceRaycastLength = 1f;
		/// the layers you want this projectile to bounce on
		[Tooltip("the layers you want this projectile to bounce on")]
		public LayerMask BounceLayers = LayerManager.ObstaclesLayerMask;
		/// a feedback to trigger at every bounce
		[Tooltip("a feedback to trigger at every bounce")]
		public MMFeedbacks BounceFeedback;

		[Header("Bounciness")]
		/// the min and max amount of bounces (a value will be picked at random between both bounds)
		[Tooltip("the min and max amount of bounces (a value will be picked at random between both bounds)")]
		[MMVector("Min", "Max")]
		public Vector2Int AmountOfBounces = new Vector2Int(10,10);
		/// the min and max speed multiplier to apply at every bounce (a value will be picked at random between both bounds)
		[Tooltip("the min and max speed multiplier to apply at every bounce (a value will be picked at random between both bounds)")]
		[MMVector("Min", "Max")]
		public Vector2 SpeedModifier = Vector2.one;

		[Header("Limit")]
		/// the minimum angle at which the projectile can bounce off the walls, tweak this if you'd like to avoid bounces at too low angles
		[Tooltip("the minimum angle at which the projectile can bounce off the walls, tweak this if you'd like to avoid bounces at too low angles")]
		[Range(0f, 90f)]
		public float MinimumBounceAngle = 10f;
		
		protected const float WALL_AVOIDANCE_DISTANCE = 0.001f;

		protected Rigidbody _rigidbody;
		protected Rigidbody2D _rigidbody2D;
		protected Vector3 _positionLastFrame;
		protected Vector3 _raycastDirection;
		protected Vector3 _reflectedDirection;
		protected int _randomAmountOfBounces;
		protected int _bouncesLeft;
		protected float _randomSpeedModifier;
        
		protected override void OnEnable()
		{
			base.OnEnable();
			SetPositionLastFrame();
		}
		
		/// <summary>
		/// On init we randomize our values, refresh our 2D collider because Unity is weird sometimes
		/// </summary>
		protected override void Initialization()
		{
			base.Initialization();
			_rigidbody = GetComponent<Rigidbody>();
			_rigidbody2D = GetComponent<Rigidbody2D>();
			_randomAmountOfBounces = Random.Range(AmountOfBounces.x, AmountOfBounces.y);
			_randomSpeedModifier = Random.Range(SpeedModifier.x, SpeedModifier.y);
			_bouncesLeft = _randomAmountOfBounces;
			if (_collider2D != null)
			{
				_collider2D.enabled = false;
				_collider2D.enabled = true;
			}            
		}

		protected override void FixedUpdate ()
		{
			_raycastDirection = (this.transform.position - _positionLastFrame).normalized;
			
			if (BoundsBasedOn == WaysToDetermineBounds.Collider)
			{
				RaycastHit hit = MMDebug.Raycast3D(_positionLastFrame, Direction.normalized, (this.transform.position - _positionLastFrame).magnitude, BounceLayers, MMColors.DarkOrange, true);
				EvaluateHit3D(hit);
			}
			else if (BoundsBasedOn == WaysToDetermineBounds.Collider2D)
			{
				 RaycastHit2D hit = MMDebug.RayCast(_positionLastFrame, Direction.normalized, (this.transform.position - _positionLastFrame).magnitude, BounceLayers, MMColors.DarkOrange, true);	
				EvaluateHit2D(hit);
			}
			if (_shouldMove)
			{
				Movement();
			}

			SetPositionLastFrame();
		}

		protected virtual void SetPositionLastFrame()
		{
			_positionLastFrame = this.transform.position;
		}

		/// <summary>
		/// Decides whether or not we should bounce
		/// </summary>
		/// <param name="hit"></param>
		protected virtual void EvaluateHit3D(RaycastHit hit)
		{
			if (hit.collider != null)
			{
				if (_bouncesLeft > 0)
				{
					Bounce3D(hit);
				}
				else
				{
					_health.Kill();
					_damageOnTouch.HitNonDamageableFeedback?.PlayFeedbacks();
				}
			}
		}

		/// <summary>
		/// Decides whether or not we should bounce
		/// </summary>
		protected virtual void EvaluateHit2D(RaycastHit2D hit)
		{
			if (hit)
			{
				if (_bouncesLeft > 0)
				{
					Bounce2D(hit);
				}
				else
				{
					_health.Kill();
					_damageOnTouch.HitNonDamageableFeedback?.PlayFeedbacks();
				}
			}
		}

		/// <summary>
		/// Applies a bounce in 2D
		/// </summary>
		/// <param name="hit"></param>
		protected virtual void Bounce2D(RaycastHit2D hit)
		{
			BounceFeedback?.PlayFeedbacks();
			_reflectedDirection = Vector3.Reflect(_raycastDirection, hit.normal);
			
			float angle = Vector3.Angle(_raycastDirection, _reflectedDirection);

			if ( ((angle < 90) && (angle < MinimumBounceAngle))
				|| ((angle > 90) && (angle > 180 - MinimumBounceAngle)) )
			{
				_reflectedDirection = Vector3.RotateTowards(_reflectedDirection, hit.normal, MinimumBounceAngle * Mathf.Deg2Rad, 0f);
			}

			Direction = _reflectedDirection.normalized;
			this.transform.right = _spawnerIsFacingRight ? _reflectedDirection.normalized : -_reflectedDirection.normalized;
			Speed *= _randomSpeedModifier;
			_bouncesLeft--;

			this.transform.position = hit.point + (Vector2)Direction * WALL_AVOIDANCE_DISTANCE;
			_rigidbody2D.position = this.transform.position;
			SetPositionLastFrame();
		}

		/// <summary>
		/// Applies a bounce in 3D
		/// </summary>
		/// <param name="hit"></param>
		protected virtual void Bounce3D(RaycastHit hit)
		{
			BounceFeedback?.PlayFeedbacks();
			_reflectedDirection = Vector3.Reflect(_raycastDirection, hit.normal);
			
			float angle = Vector3.Angle(_raycastDirection, _reflectedDirection);
			
			if ( ((angle < 90) && (angle < MinimumBounceAngle))
			     || ((angle > 90) && (angle > 180 - MinimumBounceAngle)) )
			{
				_reflectedDirection = Vector3.RotateTowards(_reflectedDirection, hit.normal, MinimumBounceAngle * Mathf.Deg2Rad, 0f);
			}
			
			Direction = _reflectedDirection.normalized;
			this.transform.forward = _spawnerIsFacingRight ? _reflectedDirection.normalized : -_reflectedDirection.normalized;
			Speed *= _randomSpeedModifier;
			_bouncesLeft--;
			
			this.transform.position = hit.point + Direction * WALL_AVOIDANCE_DISTANCE;
			_rigidbody.position = this.transform.position;
			SetPositionLastFrame();
		}
	}	
}