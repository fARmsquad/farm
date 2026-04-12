using UnityEditor;
using UnityEditor.SceneManagement;

namespace FarmSimVR.Editor
{
    /// <summary>
    /// Unity can be configured to always open a fixed scene in Play Mode via
    /// <see cref="EditorSceneManager.playModeStartScene"/>. This project previously forced Horse Taming,
    /// which made every Play run ignore whichever scene was open in the Editor. Clearing the override
    /// restores the default: the active Editor scene is what enters Play Mode.
    /// </summary>
    /// <remarks>
    /// With Enter Play Mode Options (e.g. disabled domain reload), <c>[InitializeOnLoad]</c> static
    /// constructors may not run again for a long time, while the play-mode start scene can stay
    /// serialized in the Editor. We therefore clear the override on every transition into Play Mode
    /// and once on a delayed call after load.
    /// </remarks>
    [InitializeOnLoad]
    public static class PlayModeStartSceneConfigurator
    {
        static PlayModeStartSceneConfigurator()
        {
            EditorApplication.delayCall += ClearPlayModeStartSceneOverride;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
                ClearPlayModeStartSceneOverride();
        }

        private static void ClearPlayModeStartSceneOverride()
        {
            EditorSceneManager.playModeStartScene = null;
        }
    }
}
