using UnityEngine;
using UnityEngine.SceneManagement;

public class MainSceneManager : MonoBehaviour
{

    private float delaySeconds = 300f;

    private float timer;
    private bool hasSwitched;

    private void Update()
    {
        if (hasSwitched) return;

        timer += Time.deltaTime;

        if (timer >= delaySeconds)
        {
            hasSwitched = true;
            SceneManager.LoadScene(2);
        }
    }
}