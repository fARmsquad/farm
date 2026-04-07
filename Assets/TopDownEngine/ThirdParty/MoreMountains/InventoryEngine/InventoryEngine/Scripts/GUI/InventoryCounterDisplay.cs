using System.Collections.Generic;
using MoreMountains.Tools;
using UnityEngine;
using UnityEngine.UI;
#if MM_UGUI2
using TMPro;
#endif

namespace MoreMountains.InventoryEngine
{
	/// <summary>
	/// Use this class to display the total quantity of one or more given items in one or more given inventories
	/// </summary>
	public class InventoryCounterDisplay : MonoBehaviour, MMEventListener<MMInventoryEvent>
	{
		[Header("Items & Inventories")]
		/// a list of the items to count
		public List<InventoryItem> Item;
		/// the inventories in which to count the items
		public List<Inventory> TargetInventories;

		[Header("Display")] 
		/// the text UI to update with the total quantity of the item(s) in the target inventories
		public Text TargetText;
		#if MM_UGUI2
		public TMP_Text TargetTMP;
		#endif
		/// the format to apply to the text
		public string DisplayFormat = "0";

		/// <summary>
		/// A public method used to update the target text with the total quantity of the item(s) in the target inventories
		/// </summary>
		public void UpdateText()
		{
			TargetText.text = ComputeQuantity().ToString(DisplayFormat);
			#if MM_UGUI2
			if (TargetTMP != null)
			{
				TargetTMP.text = ComputeQuantity().ToString(DisplayFormat);	
			}
			#endif
		}
		
		/// <summary>
		/// Handles inventory events, updates the text if needed
		/// </summary>
		/// <param name="inventoryEvent"></param>
		public virtual void OnMMEvent(MMInventoryEvent inventoryEvent)
		{
			if (!ShouldUpdate(inventoryEvent.TargetInventoryName))
			{
				return;
			}
			
			switch (inventoryEvent.InventoryEventType)
			{
				case MMInventoryEventType.ContentChanged:
					UpdateText();
					break;
				
				case MMInventoryEventType.InventoryLoaded:
					UpdateText();
					break;
			}
		}

		/// <summary>
		/// Computes the quantity of the item in the target inventories
		/// </summary>
		/// <returns></returns>
		public virtual int ComputeQuantity()
		{
			int count = 0;
			foreach (Inventory inventory in TargetInventories)
			{
				foreach (InventoryItem item in Item)
				{
					count += inventory.GetQuantity(item.ItemID);
				}
			}
			return count;
		}

		/// <summary>
		/// Returns true if the inventory name passed in parameters is one of the target inventories, false otherwise
		/// </summary>
		/// <param name="inventoryName"></param>
		/// <returns></returns>
		public virtual bool ShouldUpdate(string inventoryName)
		{
			bool shouldUpdate = false;
			foreach (Inventory inventory in TargetInventories)
			{
				if (inventory.name == inventoryName)
				{
					shouldUpdate = true;
				}
			}
			return shouldUpdate;
		}
		
		/// <summary>
		/// On Enable, we start listening for MMInventoryEvents
		/// </summary>
		protected virtual void OnEnable()
		{
			this.MMEventStartListening<MMInventoryEvent>();
		}

		/// <summary>
		/// On Disable, we stop listening for MMInventoryEvents
		/// </summary>
		protected virtual void OnDisable()
		{
			this.MMEventStopListening<MMInventoryEvent>();
		}
	}	
}

