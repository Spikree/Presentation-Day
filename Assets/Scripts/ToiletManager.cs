using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ToiletManager : MonoBehaviour
{
    public static ToiletManager Instance { get; private set; }
    public static float toiletneed; // stores the score
    public TMP_Text toiletText;     // ui text display
    public Image toiletIcon;        // ui image to change color - drag the Icon's Image component here

    [Header("Color Thresholds")]
    public Color fullColor = Color.green;
    public Color warningColor = new Color(1f, 0.65f, 0f); // amber
    public Color criticalColor = Color.red;

    [Range(0, 100)] public float warningThreshold = 60f;   // below this = amber
    [Range(0, 100)] public float criticalThreshold = 25f;  // below this = red

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this; // allows me to decouple the score from the ui so I can have two score ui's for ingame and game over
    }

    void Start()
    {
        toiletneed = 100; // sets % to 100 at the beginning of the game
        UpdateScoreUI();  // update the UI with the initial score
    }

    public void AddPercentage(float amount)
    {
        toiletneed += amount;
        toiletneed = Mathf.Clamp(toiletneed, 0, 100); // keep it in range
        UpdateScoreUI();
    }

    public void MinusPercentage(float amount)
    {
        toiletneed -= amount;
        toiletneed = Mathf.Clamp(toiletneed, 0, 100); // keep it in range
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        toiletText.text = "" + Mathf.FloorToInt(toiletneed) + "%";
        UpdateIconColor();
    }

    void UpdateIconColor()
    {
        if (toiletIcon == null) return;

        if (toiletneed <= criticalThreshold)
            toiletIcon.color = criticalColor;
        else if (toiletneed <= warningThreshold)
            toiletIcon.color = warningColor;
        else
            toiletIcon.color = fullColor;
    }
}

