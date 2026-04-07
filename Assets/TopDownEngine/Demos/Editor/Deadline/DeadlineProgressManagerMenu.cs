using UnityEditor;

namespace MoreMountains.TopDownEngine
{	
	public static class DeadlineProgressManagerMenu 
	{
		[MenuItem("Tools/More Mountains/Reset all Deadline progress",false,21)]
		/// <summary>
		/// Adds a menu item to reset all progress in the deadline demo
		/// </summary>
		private static void ResetProgress()
		{
			DeadlineProgressManager.Instance.ResetProgress ();
		}
	}
}