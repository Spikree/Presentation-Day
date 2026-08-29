using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BotNeed
{
    [Tooltip("Shown in logs only. e.g. 'Toilet' or 'Hunger'.")]
    public string label = "Toilet";

    [Tooltip("Where the bot walks to before disappearing. Use a marker in open " +
             "floor near the door, NOT the door itself — a bot cannot reach a " +
             "transform buried inside a wall collider and will jam trying.")]
    public Transform doorway;

    [Tooltip("How fast this need runs down, in points per second.")]
    public float drainPerSecond = 1.5f;

    [Tooltip("The bot leaves once the need drops below this.")]
    [Range(0f, 100f)]
    public float threshold = 30f;

    [Tooltip("Shortest time spent out of the office.")]
    public float minAwaySeconds = 5f;

    [Tooltip("Longest time spent out of the office.")]
    public float maxAwaySeconds = 12f;

    [Tooltip("Each bot's threshold is nudged by up to this much at startup so " +
             "they don't all leave at the same moment. Keep it small — a large " +
             "value can push the threshold out of reach entirely.")]
    public float thresholdJitter = 10f;

    /// <summary>Current level, 0-100. Full when the bot gets back.</summary>
    [HideInInspector] public float value = 100f;

    /// <summary>Threshold after startup jitter is applied.</summary>
    [HideInInspector] public float actualThreshold;

    public bool IsUrgent => value <= actualThreshold;
}

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBot : MonoBehaviour
{
    public enum State { Working, Traveling, Sabotaging, Returning, Fleeing, LeavingOffice, Away }

    private static readonly HashSet<WorkStation> claimedTargets
        = new HashSet<WorkStation>();

    [Header("References")]
    [Tooltip("Where this bot stands when working. Usually a waypoint inside its own pod.")]
    [SerializeField] private Transform homeDesk;

    [Tooltip("This bot's own computer — the WorkStation it repairs.")]
    [SerializeField] private WorkStation homeStation;

    [Tooltip("Every computer this bot is allowed to sabotage. Add the player's " +
             "and the other bots' — but NOT its own.")]
    [SerializeField] private List<WorkStation> targets = new List<WorkStation>();

    [Tooltip("Leave empty to auto-find by the 'Player' tag on Start.")]
    [SerializeField] private Transform player;

    [Header("Needs")]
    [Tooltip("Things that pull this bot out of the office. Add one for the " +
             "toilet and one for the kitchen — they behave identically.")]
    [SerializeField] private List<BotNeed> needs = new List<BotNeed>();

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float fleeSpeed = 4f;

    [Tooltip("How close counts as 'arrived' at a waypoint or a destination.")]
    [SerializeField] private float arriveDistance = 0.4f;

    [Header("Detection")]
    [Tooltip("How close the player must be for this bot to notice them.")]
    [SerializeField] private float spotRadius = 3f;

    [Tooltip("If true, walls and partitions block the bot's view.")]
    [SerializeField] private bool requireLineOfSight = true;

    [Tooltip("Layers that block sight AND movement. Set this to your Obstacles layer.")]
    [SerializeField] private LayerMask sightBlockers;

    [Header("Behaviour")]
    [Tooltip("If this bot's own progress falls below this, it stays home and " +
             "repairs instead of going out to sabotage.")]
    [Range(0f, 100f)]
    [SerializeField] private float repairThreshold = 40f;

    [Tooltip("Seconds spent working at its own desk before looking for a victim.")]
    [SerializeField] private float workDuration = 6f;

    [Tooltip("Seconds spent sabotaging before heading home of its own accord.")]
    [SerializeField] private float sabotageDuration = 5f;

    [Tooltip("Seconds to lie low at its own desk after being caught.")]
    [SerializeField] private float cooldownAfterCaught = 8f;

    [Header("Rates")]
    [SerializeField] private float sabotagePerSecond = 5f;
    [SerializeField] private float recoverPerSecond = 3f;

    [Header("Stuck Recovery")]
    [SerializeField] private float stuckCheckInterval = 0.5f;
    [SerializeField] private float minProgress = 0.1f;

    [Header("Diagnostics")]
    [Tooltip("Logs routes and need trips to the Console.")]
    [SerializeField] private bool logRoutes;

    [Header("Optional")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private State state = State.Working;
    private Vector2 moveDirection;

    private float workTimer;
    private float sabotageTimer;
    private float awayTimer;
    private WorkStation currentTarget;
    private BotNeed currentNeed;

    private List<Waypoint> route = new List<Waypoint>();
    private int routeIndex;

    private Vector2 lastCheckPosition;
    private float stuckTimer;

    private SpriteRenderer[] renderers;
    private Collider2D[] colliders;

    private float OwnProgress => homeStation != null ? homeStation.Progress : 100f;

    private Transform TargetPoint =>
        currentTarget != null ? currentTarget.ApproachPoint : null;


    private bool IsUpToNoGood =>
        state == State.Traveling || state == State.Sabotaging;


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();

        renderers = GetComponentsInChildren<SpriteRenderer>();
        colliders = GetComponentsInChildren<Collider2D>();

        claimedTargets.RemoveWhere(s => s == null);
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        foreach (BotNeed need in needs)
        {
            need.value = Random.Range(70f, 100f);
            need.actualThreshold = Mathf.Clamp(
                need.threshold + Random.Range(-need.thresholdJitter, need.thresholdJitter),
                5f, 95f);
        }

        workTimer = workDuration;
        lastCheckPosition = rb.position;
    }

    private void OnDisable()
    {
        ReleaseTarget();
    }

    private void Update()
    {
        TickNeeds();

        if (IsUpToNoGood && CanSeePlayer())
        {
            Caught();
        }

        switch (state)
        {
            case State.Working:       TickWorking();       break;
            case State.Traveling:     TickTraveling();     break;
            case State.Sabotaging:    TickSabotaging();    break;
            case State.Returning:     TickReturning();     break;
            case State.Fleeing:       TickFleeing();       break;
            case State.LeavingOffice: TickLeavingOffice(); break;
            case State.Away:          TickAway();          break;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        float speed = (state == State.Fleeing) ? fleeSpeed : moveSpeed;
        rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
    }


    private void ClaimTarget(WorkStation station)
    {
        if (station != null) claimedTargets.Add(station);
    }

    private void ReleaseTarget()
    {
        if (currentTarget != null) claimedTargets.Remove(currentTarget);
    }

    private void TickNeeds()
    {
        if (state == State.Away) return;

        foreach (BotNeed need in needs)
        {
            need.value = Mathf.Max(0f, need.value - need.drainPerSecond * Time.deltaTime);
        }
    }

    private BotNeed MostUrgentNeed()
    {
        BotNeed worst = null;

        foreach (BotNeed need in needs)
        {
            if (need.doorway == null) continue;
            if (!need.IsUrgent) continue;

            if (worst == null || need.value < worst.value) worst = need;
        }

        return worst;
    }

    private void SetVisible(bool visible)
    {
        foreach (SpriteRenderer r in renderers) if (r != null) r.enabled = visible;
        foreach (Collider2D c in colliders)     if (c != null) c.enabled = visible;
    }


    private void Caught()
    {
        ReleaseTarget();
        currentTarget = null;
        EnterState(State.Fleeing);
    }

    private void EnterState(State next)
    {
        if (next != State.Traveling && next != State.Sabotaging)
        {
            ReleaseTarget();
        }

        state = next;

        stuckTimer = 0f;
        lastCheckPosition = rb.position;

        switch (next)
        {
            case State.Traveling:
                BuildRoute(TargetPoint);
                break;

            case State.LeavingOffice:
                BuildRoute(currentNeed != null ? currentNeed.doorway : null);
                break;

            case State.Returning:
            case State.Fleeing:
                BuildRoute(homeDesk);
                break;

            case State.Sabotaging:
                sabotageTimer = sabotageDuration;
                route.Clear();
                routeIndex = 0;
                break;

            case State.Away:
                awayTimer = Random.Range(
                    currentNeed.minAwaySeconds, currentNeed.maxAwaySeconds);
                moveDirection = Vector2.zero;
                SetVisible(false);
                route.Clear();
                routeIndex = 0;

                if (logRoutes)
                {
                    Debug.Log($"{name}: off to the {currentNeed.label} for " +
                              $"{awayTimer:F1}s", this);
                }
                break;

            default:                       // Working
                workTimer = workDuration;
                route.Clear();
                routeIndex = 0;
                break;
        }
    }

    private void TickWorking()
    {
        moveDirection = Vector2.zero;

        if (homeStation != null)
        {
            homeStation.Restore(recoverPerSecond * Time.deltaTime);
        }

        BotNeed urgent = MostUrgentNeed();
        if (urgent != null)
        {
            currentNeed = urgent;
            EnterState(State.LeavingOffice);
            return;
        }

        workTimer -= Time.deltaTime;
        if (workTimer > 0f) return;

        if (OwnProgress < repairThreshold)
        {
            workTimer = workDuration;
            return;
        }

        currentTarget = ChooseTarget();

        if (currentTarget == null)
        {
            workTimer = workDuration;
            return;
        }

        ClaimTarget(currentTarget);
        EnterState(State.Traveling);
    }

    private void TickLeavingOffice()
    {
        CheckStuck();

        if (currentNeed == null || currentNeed.doorway == null)
        {
            EnterState(State.Working);
            return;
        }

        if (FollowRoute(currentNeed.doorway.position))
        {
            EnterState(State.Away);
        }
    }

    private void TickAway()
    {
        moveDirection = Vector2.zero;

        awayTimer -= Time.deltaTime;
        if (awayTimer > 0f) return;

        currentNeed.value = 100f;      // sorted
        currentNeed = null;

        SetVisible(true);
        EnterState(State.Returning);
    }

    private void TickTraveling()
    {
        CheckStuck();

        if (TargetPoint == null)
        {
            EnterState(State.Working);
            return;
        }

        if (currentTarget.IsGuarded)
        {
            ReleaseTarget();
            currentTarget = null;
            EnterState(State.Returning);
            return;
        }

        if (FollowRoute(TargetPoint.position))
        {
            EnterState(State.Sabotaging);
        }
    }

    private void TickSabotaging()
    {
        moveDirection = Vector2.zero;

        if (currentTarget == null)
        {
            EnterState(State.Returning);
            return;
        }

        if (currentTarget.IsGuarded)
        {
            Caught();
            return;
        }

        currentTarget.Drain(sabotagePerSecond * Time.deltaTime);

        sabotageTimer -= Time.deltaTime;

        bool timeUp         = sabotageTimer <= 0f;
        bool targetFinished = currentTarget.Progress <= 1f;
        bool needsToRepair  = OwnProgress < repairThreshold;

        if (timeUp || targetFinished || needsToRepair)
        {
            EnterState(State.Returning);
        }
    }

    private void TickReturning()
    {
        CheckStuck();

        if (homeDesk == null)
        {
            EnterState(State.Working);
            return;
        }

        if (FollowRoute(homeDesk.position))
        {
            EnterState(State.Working);
        }
    }

    private void TickFleeing()
    {
        CheckStuck();

        if (homeDesk == null)
        {
            moveDirection = Vector2.zero;
            return;
        }

        if (FollowRoute(homeDesk.position))
        {
            EnterState(State.Working);
            workTimer = cooldownAfterCaught;
        }
    }


    private WorkStation ChooseTarget()
    {
        List<WorkStation> viable = new List<WorkStation>();

        foreach (WorkStation station in targets)
        {
            if (station == null || !station.IsValid) continue;
            if (station == homeStation) continue;              // never attack itself
            if (station.Progress <= 1f) continue;              // already flattened
            if (station.IsGuarded) continue;                   // owner is right there
            if (claimedTargets.Contains(station)) continue;    // another bot has it

            viable.Add(station);
        }

        if (viable.Count == 0) return null;

        return viable[Random.Range(0, viable.Count)];
    }

    private void BuildRoute(Transform destination)
    {
        route.Clear();
        routeIndex = 0;

        if (destination == null) return;

        Waypoint from = Waypoint.NearestVisible(rb.position, sightBlockers);
        Waypoint to   = Waypoint.NearestVisible(destination.position, sightBlockers);

        if (from == null || to == null) return;

        route = Waypoint.FindPath(from, to);

        if (logRoutes && route.Count == 0)
        {
            Debug.LogWarning(
                $"{name}: NO ROUTE from {from.name} to {to.name}.", this);
        }
    }

    private bool FollowRoute(Vector3 finalDestination)
    {
        if (routeIndex < route.Count)
        {
            Waypoint node = route[routeIndex];

            if (node == null || StepToward(node.transform.position))
            {
                routeIndex++;
            }

            return false;
        }

        return StepToward(finalDestination);
    }

    private bool StepToward(Vector3 destination)
    {
        Vector2 offset = (Vector2)destination - rb.position;

        if (offset.magnitude <= arriveDistance)
        {
            moveDirection = Vector2.zero;
            return true;
        }

        moveDirection = offset.normalized;
        return false;
    }

    private void CheckStuck()
    {
        if (moveDirection == Vector2.zero)
        {
            stuckTimer = 0f;
            lastCheckPosition = rb.position;
            return;
        }

        stuckTimer += Time.deltaTime;
        if (stuckTimer < stuckCheckInterval) return;

        if ((rb.position - lastCheckPosition).magnitude < minProgress)
        {
            routeIndex++;

            if (routeIndex >= route.Count)
            {
                Transform destination;

                switch (state)
                {
                    case State.LeavingOffice:
                        destination = currentNeed != null ? currentNeed.doorway : null;
                        break;
                    case State.Fleeing:
                    case State.Returning:
                        destination = homeDesk;
                        break;
                    default:
                        destination = TargetPoint;
                        break;
                }

                BuildRoute(destination);
            }
        }

        stuckTimer = 0f;
        lastCheckPosition = rb.position;
    }


    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector2 toPlayer = (Vector2)player.position - rb.position;
        if (toPlayer.magnitude > spotRadius) return false;

        if (!requireLineOfSight) return true;

        RaycastHit2D hit = Physics2D.Raycast(
            rb.position, toPlayer.normalized, toPlayer.magnitude, sightBlockers);

        return hit.collider == null;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        animator.SetFloat("Horizontal", moveDirection.x);
        animator.SetFloat("Vertical", moveDirection.y);
        animator.SetBool("IsMoving", moveDirection.sqrMagnitude > 0.01f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, spotRadius);

        Gizmos.color = Color.magenta;
        for (int i = routeIndex; i < route.Count; i++)
        {
            if (route[i] == null) continue;

            Vector3 previous = (i == routeIndex)
                ? transform.position
                : route[i - 1].transform.position;

            Gizmos.DrawLine(previous, route[i].transform.position);
        }
    }
}