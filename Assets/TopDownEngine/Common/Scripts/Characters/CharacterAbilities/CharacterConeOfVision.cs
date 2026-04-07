using System;
using UnityEngine;
using System.Collections;
using MoreMountains.Tools;
using MoreMountains.InventoryEngine;
using System.Collections.Generic;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// An ability that casts a cone of vision around the character.
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/Abilities/Character Cone of Vision")]
	public class CharacterConeOfVision : TopDownMonoBehaviour
	{
		protected MMConeOfVision _coneOfVision;
		protected MMConeOfVision2D _coneOfVision2D;
		
		protected CharacterOrientation2D _characterOrientation2D;
		protected CharacterOrientation3D _characterOrientation3D;
		
		protected bool _orientation3D = false;
		protected bool _orientation2D = false;

		/// <summary>
		/// On awake, we grab our components
		/// </summary>
		protected virtual void Awake()
		{
			_characterOrientation2D = this.gameObject.GetComponentInParent<CharacterOrientation2D>();
			_characterOrientation3D = this.gameObject.GetComponentInParent<CharacterOrientation3D>();
			_orientation2D = (_characterOrientation2D != null);
			_orientation3D = (_characterOrientation3D != null);
			_coneOfVision = this.gameObject.GetComponent<MMConeOfVision>();
			_coneOfVision2D = this.gameObject.GetComponent<MMConeOfVision2D>();
		}

		/// <summary>
		/// On update, we update our cone of vision
		/// </summary>
		protected virtual void Update()
		{
			UpdateDirection();   
		}

		/// <summary>
		/// Sends the character orientation's angle to the cone of vision
		/// </summary>
		protected virtual void UpdateDirection()
		{
			if (!_orientation2D && !_orientation3D)
			{
				_coneOfVision.SetDirectionAndAngles(this.transform.forward, this.transform.eulerAngles);   
				return;
			}
			if (_orientation2D)
			{
				Vector3 direction = Vector3.zero;
				Vector3 modelAngles = Vector3.zero;
				switch (_characterOrientation2D.CurrentFacingDirection)
				{
					case Character.FacingDirections.West:
						direction = new Vector3(0, 180, 0);
						modelAngles = direction;
						break;
					case Character.FacingDirections.East:
						direction = new Vector3(0, 0, 0);
						modelAngles = direction;
						break;
					case Character.FacingDirections.North:
						direction = new Vector3(0, 90, 0);
						modelAngles = direction;
						break;
					case Character.FacingDirections.South:
						direction = new Vector3(0, -90, 0);
						modelAngles = direction;
						break;
				}
				_coneOfVision2D.SetDirectionAndAngles(direction, modelAngles);
				return;
			}
			if (_orientation3D)
			{
				_coneOfVision.SetDirectionAndAngles(_characterOrientation3D.ModelDirection, _characterOrientation3D.ModelAngles);
				return;
			}
		}
	}
}