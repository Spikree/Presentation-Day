using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainSceneManager : MonoBehaviour
{
    private float delaySeconds = 300f;
    [SerializeField] private TextMeshProUGUI timerText1;
    [SerializeField] private TextMeshProUGUI timerText2;
    [SerializeField] private TextMeshProUGUI timerText3;
    [SerializeField] private TextMeshProUGUI timerText4;
    [SerializeField] private TextMeshProUGUI timerText5;
    [SerializeField] private TextMeshProUGUI timerText6;
    [SerializeField] private TextMeshProUGUI timerText7;
    [SerializeField] private TextMeshProUGUI timerText8;



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
            return;
        } 

        float timeRemaining = delaySeconds - timer;
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText1.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText2.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText3.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText4.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText5.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText6.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText7.text = string.Format("{0:00}:{1:00}", minutes, seconds);
        timerText8.text = string.Format("{0:00}:{1:00}", minutes, seconds);

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