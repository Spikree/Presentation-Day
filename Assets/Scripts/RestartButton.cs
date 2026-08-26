using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    // Starts the game
    public void onRestartClick()
    {
        SceneManager.LoadScene(1);
        
    }

    // Exits the game
    public void OnExitClick()
    {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
    Application.Quit();
        
    }
}
