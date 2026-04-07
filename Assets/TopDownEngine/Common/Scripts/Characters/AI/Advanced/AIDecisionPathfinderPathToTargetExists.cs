using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// This decision will return true if there's no path to the current brain Target
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Pathfinder Path To Target Exists")]
	public class AIDecisionPathfinderPathToTargetExists : AIDecision
	{
		protected CharacterPathfinder3D _characterPathfinder3D;
		
		/// <summary>
		/// On init we grab our pathfinder ability
		/// </summary>
		public override void Initialization()
		{
			base.Initialization();
			_characterPathfinder3D = this.gameObject.GetComponentInParent<Character>()?.FindAbility<CharacterPathfinder3D>();
		}
		
		/// <summary>
		/// We return true on Decide
		/// </summary>
		/// <returns></returns>
		public override bool Decide()
		{
			if (_brain.Target == null)
			{
				return false;
			}

			bool pathIsComplete = _characterPathfinder3D.PathExists(this.transform.position, _brain.Target.position);
			
			return pathIsComplete;
		}
	}
}