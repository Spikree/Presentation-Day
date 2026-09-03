using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class BossReportManager : MonoBehaviour
{
    public static BossReportManager Instance { get; private set; }

    [Header("Detection")]
    [Tooltip("How close the player must be to a rival desk to count as " +
             "tampering with it. Should roughly match the reach of the " +
             "EnemyComputer trigger they stand on.")]
    [SerializeField] private float tamperRadius = 1.5f;

    [Header("Consequences")]
    [Tooltip("How many reports before the player is fired. 2 means one warning " +
             "then out.")]
    [SerializeField] private int strikesAllowed = 2;

    [Tooltip("Scene loaded when the player is fired.")]
    [SerializeField] private string defeatSceneName = "DefeatScene";

    [Tooltip("Persistent root to tear down before loading the defeat scene, so " +
             "the in-game HUD does not draw over it.")]
    [SerializeField] private string persistentRootName = "Scores";

    [Header("UI")]
    [Tooltip("Text used for the warning message. Its GameObject is shown and " +
             "hidden by this script.")]
    [SerializeField] private TextMeshProUGUI warningText;

    [Tooltip("How long the warning stays on screen.")]
    [SerializeField] private float warningSeconds = 4f;

    [Tooltip("Shown on the first report. {0} is replaced by the reporter's name.")]
    [SerializeField] private string firstWarning =
        "{0} saw you. The boss has been told.\nOne more and you're fired.";

    [Header("Debug")]
    [Tooltip("Logs which bots rolled as snitches this round. Leave OFF when " +
             "actually playing — knowing ruins the whole mechanic.")]
    [SerializeField] private bool logSnitches;

    public int Strikes { get; private set; }

    public bool LogSnitches => logSnitches;

    private Transform player;
    private readonly List<WorkStation> rivalStations = new List<WorkStation>();
    private float warningHideTime;
    private bool fired;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null) player = found.transform;

        foreach (WorkStation station in
                 FindObjectsByType<WorkStation>(FindObjectsSortMode.None))
        {
            if (station != null && !station.isPlayerStation)
            {
                rivalStations.Add(station);
            }
        }

        if (warningText != null) warningText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (warningText != null &&
            warningText.gameObject.activeSelf &&
            Time.time >= warningHideTime)
        {
            warningText.gameObject.SetActive(false);
        }
    }

    public WorkStation TamperedStation()
    {
        if (player == null) return null;

        float radiusSqr = tamperRadius * tamperRadius;
        WorkStation closest = null;
        float closestSqr = float.MaxValue;

        foreach (WorkStation station in rivalStations)
        {
            if (station == null) continue;

            Vector2 offset = (Vector2)station.transform.position
                             - (Vector2)player.position;

            float sqr = offset.sqrMagnitude;
            if (sqr > radiusSqr) continue;

            if (sqr < closestSqr)
            {
                closestSqr = sqr;
                closest = station;
            }
        }

        return closest;
    }

    public void Report(string reporterName)
    {
        if (fired) return;

        Strikes++;

        if (Strikes >= strikesAllowed)
        {
            Fire();
            return;
        }

        ShowWarning(string.Format(firstWarning, reporterName));
    }

    private void ShowWarning(string message)
    {
        if (warningText == null) return;

        warningText.text = message;
        warningText.gameObject.SetActive(true);
        warningHideTime = Time.time + warningSeconds;
    }

    private void Fire()
    {
        fired = true;
        
        GameObject persistent = GameObject.Find(persistentRootName);
        if (persistent != null) Destroy(persistent);

        SceneManager.LoadScene(defeatSceneName);
    }
}