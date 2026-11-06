using UnityEditor;
using UnityEditor.SceneManagement;

namespace SHI.EditorTools
{
    // Opens the main scene and enters play mode without building a player.
    // With the -shiShots <folder> argument, GameController plays a scripted run and saves screenshots.
    public static class PlayCheck
    {
        public static void Run()
        {
            PlayerSettings.runInBackground = true;
            EditorSceneManager.OpenScene("Assets/SHI/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
        }
    }
}
