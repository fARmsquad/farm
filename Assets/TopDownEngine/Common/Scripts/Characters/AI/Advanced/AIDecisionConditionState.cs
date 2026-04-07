using MoreMountains.Tools;
using UnityEngine;

namespace MoreMountains.TopDownEngine
{
	/// <summary>
	/// This decision will return true if the character is in the specified condition state
	/// </summary>
	[AddComponentMenu("TopDown Engine/Character/AI/Decisions/AI Decision Condition State")]
	public class AIDecisionConditionState : AIDecision
	{
		public CharacterStates.CharacterConditions ConditionState = CharacterStates.CharacterConditions.Stunned;
		protected Character _character;

		/// <summary>
		/// On init we grab our Character component
		/// </summary>
		public override void Initialization()
		{
			_character = this.gameObject.GetComponentInParent<Character>();
		}

		/// <summary>
		/// On Decide we check what state we're in
		/// </summary>
		/// <returns></returns>
		public override bool Decide()
		{
			return (_character.ConditionState.CurrentState == ConditionState);
		}
	}
}