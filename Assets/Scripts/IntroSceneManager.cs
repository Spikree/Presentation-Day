using UnityEngine;
using UnityEngine.SceneManagement;

public class IntroSceneManager : MonoBehaviour
{

    private float delaySeconds = 42f;

    private float timer;
    private bool hasSwitched;

    private void Update()
    {
        if (hasSwitched) return;

        timer += Time.deltaTime;

        if (timer >= delaySeconds)
        {
            hasSwitched = true;
            SceneManager.LoadScene(1);
        }
    }
}