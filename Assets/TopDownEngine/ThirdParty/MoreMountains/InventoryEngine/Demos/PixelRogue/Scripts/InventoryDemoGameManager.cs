using UnityEngine;
using MoreMountains.Tools;

namespace MoreMountains.InventoryEngine
{
	/// <summary>
	/// An example of a game manager, the only significant part being how we trigger in a single place the load of all inventories, in the Start method.
	/// </summary>
	public class InventoryDemoGameManager : MMSingleton<InventoryDemoGameManager> 
	{
		public virtual InventoryDemoCharacter Player { get; protected set; }
		
		/// <summary>
		/// Statics initialization to support enter play modes
		/// </summary>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		protected static void InitializeStatics()
		{
			_instance = null;
		}

		protected override void Awake () 
		{
			base.Awake ();
			Player = GameObject.FindGameObjectWithTag("Player").GetComponent<InventoryDemoCharacter>()	;
		}

		/// <summary>
		/// On start, we trigger our load event, which will be caught by inventories so they try to load saved content
		/// </summary>
		protected virtual void Start()
		{
			MMGameEvent.Trigger("Load");
		}
	}
}