using UnityEngine;
using TMPro;

public class SpottedCountdown : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("Text that shows the countdown. Its GameObject is shown and " +
             "hidden by this script.")]
    [SerializeField] private TextMeshProUGUI countdownText;

    [Tooltip("Optional. Anything else to show while you are being watched — a " +
             "red vignette, an eye icon, a border flash.")]
    [SerializeField] private GameObject watchedVisuals;

    [Tooltip("Shown before the number.")]
    [SerializeField] private string prefix = "SEEN! ";

    [Tooltip("Decimal places on the number. 1 reads more urgently than 0.")]
    [Range(0, 2)]
    [SerializeField] private int decimals = 1;

    private BotSnitch[] snitches;

    private void Start()
    {
        // Cached once bots are not created or destroyed mid-round, they only
        // hide themselves when they go to the toilet.
        snitches = FindObjectsByType<BotSnitch>(FindObjectsSortMode.None);

        Show(false);
    }

    private void Update()
    {
        float mostUrgent = float.MaxValue;

        foreach (BotSnitch snitch in snitches)
        {
            if (snitch == null || !snitch.IsWatching) continue;

            float left = snitch.SecondsLeft;
            if (left >= 0f && left < mostUrgent) mostUrgent = left;
        }

        if (mostUrgent == float.MaxValue)
        {
            Show(false);
            return;
        }

        Show(true);

        if (countdownText != null)
        {
            countdownText.text = prefix + mostUrgent.ToString("F" + decimals);
        }
    }

    private void Show(bool visible)
    {
        if (countdownText != null &&
            countdownText.gameObject.activeSelf != visible)
        {
            countdownText.gameObject.SetActive(visible);
        }

        if (watchedVisuals != null &&
            watchedVisuals.activeSelf != visible)
        {
            watchedVisuals.SetActive(visible);
        }
    }
}