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
            LoadOutcomeScene();
        }
    }


    private void LoadOutcomeScene()
    {
        EnemyPercentManager[] enemies = FindObjectsByType<EnemyPercentManager>(FindObjectsInactive.Exclude);

        float playerPercentage = PercentageManager.percentage;
        bool playerBeatsAll = true;

        foreach (var e in enemies)
        {
            if (e.percentage >= playerPercentage)
            {
                playerBeatsAll = false;
                break;
            }
        }

        if (playerBeatsAll)
        {
            SceneManager.LoadScene(3); // player wins — higher than every enemy
        }
        else
        {
            SceneManager.LoadScene(2); // at least one enemy is equal or higher
        }
    }
}