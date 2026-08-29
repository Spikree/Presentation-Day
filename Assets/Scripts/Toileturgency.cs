using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class ToiletUrgency : MonoBehaviour
{
    [Header("Trigger")]
    [Tooltip("The countdown starts once the toilet meter falls to or below this.")]
    [Range(0f, 100f)]
    [SerializeField] private float urgentBelow = 1f;

    [Tooltip("The countdown stops once the meter climbs back above this. Keep " +
             "it above the trigger value so a tiny top-up does not clear it.")]
    [Range(0f, 100f)]
    [SerializeField] private float safeAbove = 20f;

    [Tooltip("Seconds the player has to reach the toilet before losing.")]
    [SerializeField] private float graceSeconds = 10f;

    [Header("UI")]
    [Tooltip("Text that shows the countdown. Leave its GameObject active — this " +
             "script hides and shows it as needed.")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Tooltip("Optional. Anything else to show while the countdown runs — a " +
             "warning panel, a flashing icon, a vignette.")]
    [SerializeField] private GameObject warningVisuals;

    [Tooltip("Shown before the number. e.g. 'TOILET! ' or 'HOLD IT: '.")]
    [SerializeField] private string prefix = "TOILET! ";

    [Header("Outcome")]
    [Tooltip("Scene to load when the player does not make it. Should match the " +
             "defeat scene used elsewhere.")]
    [SerializeField] private string defeatSceneName = "DefeatScene";

    [Tooltip("Name of the persistent root to tear down before loading the " +
             "defeat scene, so the in-game HUD does not draw over it.")]
    [SerializeField] private string persistentRootName = "Scores";

    private bool counting;
    private float timeLeft;
    private bool resolved;

    private void Start()
    {
        ShowUI(false);
    }

    private void Update()
    {
        if (resolved) return;
        if (ToiletManager.Instance == null) return;

        float meter = ToiletManager.toiletneed;

        if (!counting)
        {
            if (meter <= urgentBelow) StartCountdown();
            return;
        }

        if (meter > safeAbove)
        {
            StopCountdown();
            return;
        }

        timeLeft -= Time.deltaTime;

        if (timeLeft <= 0f)
        {
            LoseRound();
            return;
        }

        UpdateText();
    }

    private void StartCountdown()
    {
        counting = true;
        timeLeft = graceSeconds;
        ShowUI(true);
        UpdateText();
    }

    private void StopCountdown()
    {
        counting = false;
        ShowUI(false);
    }

    private void UpdateText()
    {
        if (countdownText == null) return;

        countdownText.text = prefix + Mathf.CeilToInt(timeLeft);
    }

    private void ShowUI(bool visible)
    {
        if (countdownText != null) countdownText.gameObject.SetActive(visible);
        if (warningVisuals != null) warningVisuals.SetActive(visible);
    }

    private void LoseRound()
    {
        resolved = true;
        ShowUI(false);

        GameObject persistent = GameObject.Find(persistentRootName);
        if (persistent != null) Destroy(persistent);

        SceneManager.LoadScene(defeatSceneName);
    }
}