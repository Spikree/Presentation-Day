using UnityEngine;

public class BotSnitch : MonoBehaviour
{
    [Header("Identity")]
    [Tooltip("This bot's own computer. Attacks on THIS desk are always " +
             "reported, whatever the loyalty roll says. Same WorkStation you " +
             "set as Home Station on the EnemyBot component.")]
    [SerializeField] private WorkStation ownStation;

    [Header("Loyalty")]
    [Tooltip("Chance this bot reports a crime against SOMEONE ELSE, rolled " +
             "fresh at the start of every round. 0.5 means about half the " +
             "office will grass on you, and you cannot tell which half.")]
    [Range(0f, 1f)]
    [SerializeField] private float snitchChance = 0.5f;

    [Header("Detection")]
    [Tooltip("How close the player must be for this bot to notice them. " +
             "Usually matches the bot's own Spot Radius.")]
    [SerializeField] private float witnessRadius = 4f;

    [Tooltip("If true, walls and partitions block the bot's view.")]
    [SerializeField] private bool requireLineOfSight = true;

    [Tooltip("Layers that block sight. Set this to your Obstacles layer.")]
    [SerializeField] private LayerMask sightBlockers;

    [Header("Timing")]
    [Tooltip("Seconds the player has to break away before this bot acts. " +
             "Counts down on screen.")]
    [SerializeField] private float witnessSeconds = 3f;

    [Tooltip("Seconds before this bot can watch again after a completed " +
             "countdown. Applies whether or not it reported, so a quiet bot " +
             "behaves identically to a snitch from the outside.")]
    [SerializeField] private float watchCooldown = 15f;

    [Header("Optional")]
    [Tooltip("Shown above the bot while it is watching you — an exclamation " +
             "mark or similar.")]
    [SerializeField] private GameObject alertIndicator;

    private Transform player;
    private bool willSnitch;
    private float watchedFor;
    private float nextWatchTime;
    private WorkStation watchedStation;


    public float SecondsLeft => IsWatching ? witnessSeconds - watchedFor : -1f;

    public bool IsWatching { get; private set; }


    private void Start()
    {
        GameObject found = GameObject.FindGameObjectWithTag("Player");
        if (found != null) player = found.transform;

        willSnitch = Random.value < snitchChance;

        if (BossReportManager.Instance != null &&
            BossReportManager.Instance.LogSnitches)
        {
            Debug.Log($"{name}: {(willSnitch ? "WILL snitch" : "will keep quiet")} " +
                      "about other people's desks", this);
        }

        SetAlert(false);
    }

    private void OnDisable()
    {
        IsWatching = false;
        SetAlert(false);
    }

    private void Update()
    {
        if (player == null) return;
        if (BossReportManager.Instance == null) return;

        if (Time.time < nextWatchTime)
        {
            StopWatching();
            return;
        }

        WorkStation station = BossReportManager.Instance.TamperedStation();

        if (station == null || !CanSeePlayer())
        {
            StopWatching();
            return;
        }

        IsWatching = true;
        watchedStation = station;
        SetAlert(true);

        watchedFor += Time.deltaTime;
        if (watchedFor < witnessSeconds) return;

        Act();
    }

    private void Act()
    {
        bool ownDeskAttacked = ownStation != null && watchedStation == ownStation;

        StopWatching();
        watchedFor = 0f;
        nextWatchTime = Time.time + watchCooldown;

        if (ownDeskAttacked || willSnitch)
        {
            BossReportManager.Instance.Report(name);
        }
    }


    public void ReportNow()
    {
        if (BossReportManager.Instance == null) return;
        if (Time.time < nextWatchTime) return;

        WorkStation station = BossReportManager.Instance.TamperedStation();
        if (station == null) return;           

        bool ownDeskAttacked = ownStation != null && station == ownStation;

        StopWatching();
        nextWatchTime = Time.time + watchCooldown;

        if (ownDeskAttacked || willSnitch)
        {
            BossReportManager.Instance.Report(name);
        }
    }

    private void StopWatching()
    {
        IsWatching = false;
        watchedStation = null;
        watchedFor = 0f;
        SetAlert(false);
    }

    private void SetAlert(bool on)
    {
        if (alertIndicator != null) alertIndicator.SetActive(on);
    }

    private bool CanSeePlayer()
    {
        Vector2 here = transform.position;
        Vector2 toPlayer = (Vector2)player.position - here;

        if (toPlayer.magnitude > witnessRadius) return false;
        if (!requireLineOfSight) return true;

        RaycastHit2D hit = Physics2D.Raycast(
            here, toPlayer.normalized, toPlayer.magnitude, sightBlockers);

        return hit.collider == null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, witnessRadius);
    }
}