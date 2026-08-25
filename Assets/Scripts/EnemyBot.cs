using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyBot : MonoBehaviour
{
    public enum State { Working, Traveling, Sabotaging, Returning, Fleeing }

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
    [SerializeField] private float repairThreshold = 50f;

    [Tooltip("Seconds spent working at its own desk before looking for a victim.")]
    [SerializeField] private float workDuration = 6f;

    [Tooltip("Seconds spent sabotaging before heading home of its own accord.")]
    [SerializeField] private float sabotageDuration = 5f;

    [Tooltip("Seconds to lie low at its own desk after being caught, before " +
             "trying again. Stops a bot bouncing straight back to the scene.")]
    [SerializeField] private float cooldownAfterCaught = 8f;

    [Header("Rates")]
    [Tooltip("Percent per second drained from the target while sabotaging.")]
    [SerializeField] private float sabotagePerSecond = 5f;

    [Tooltip("Percent per second this bot regains while working at home.")]
    [SerializeField] private float recoverPerSecond = 3f;

    [Header("Stuck Recovery")]
    [Tooltip("How often to check whether the bot is actually making progress.")]
    [SerializeField] private float stuckCheckInterval = 0.5f;

    [Tooltip("Distance it must cover per check to count as moving.")]
    [SerializeField] private float minProgress = 0.1f;

    [Header("Diagnostics")]
    [Tooltip("Logs every route the bot builds to the Console. Turn off once " +
             "the navigation is behaving.")]
    [SerializeField] private bool logRoutes;

    [Header("Optional")]
    [Tooltip("Same Horizontal / Vertical / IsMoving parameters as the player.")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private State state = State.Working;
    private Vector2 moveDirection;

    private float workTimer;
    private float sabotageTimer;
    private WorkStation currentTarget;

    private List<Waypoint> route = new List<Waypoint>();
    private int routeIndex;

    private Vector2 lastCheckPosition;
    private float stuckTimer;

    private float OwnProgress => homeStation != null ? homeStation.Progress : 100f;

    private Transform TargetPoint =>
        currentTarget != null ? currentTarget.ApproachPoint : null;

    private bool IsUpToNoGood =>
        state == State.Traveling || state == State.Sabotaging;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (player == null)
        {
            GameObject found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }

        workTimer = workDuration;
        lastCheckPosition = rb.position;
    }

    private void Update()
    {
        if (IsUpToNoGood && CanSeePlayer())
        {
            Caught();
        }

        switch (state)
        {
            case State.Working:    TickWorking();    break;
            case State.Traveling:  TickTraveling();  break;
            case State.Sabotaging: TickSabotaging(); break;
            case State.Returning:  TickReturning();  break;
            case State.Fleeing:    TickFleeing();    break;
        }

        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        float speed = (state == State.Fleeing) ? fleeSpeed : moveSpeed;
        rb.MovePosition(rb.position + moveDirection * speed * Time.fixedDeltaTime);
    }

    private void Caught()
    {
        currentTarget = null;
        EnterState(State.Fleeing);
    }

    private void EnterState(State next)
    {
        state = next;

        stuckTimer = 0f;
        lastCheckPosition = rb.position;

        switch (next)
        {
            case State.Traveling:
                BuildRoute(TargetPoint);
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

        workTimer -= Time.deltaTime;
        if (workTimer > 0f) return;

        // Behind on own work? Stay put and repair rather than sabotaging others.
        if (OwnProgress < repairThreshold)
        {
            workTimer = workDuration;
            return;
        }

        currentTarget = ChooseTarget();

        if (currentTarget == null)
        {
            // Everyone is at their desk, or nobody is worth hitting. Keep
            // working and check again shortly.
            workTimer = workDuration;
            return;
        }

        EnterState(State.Traveling);
    }

    private void TickTraveling()
    {
        CheckStuck();

        if (TargetPoint == null)
        {
            EnterState(State.Working);
            return;
        }

        // cubicle owner came back while we were on our way abandon the trip quietly
        if (currentTarget.IsGuarded)
        {
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

        // The cubicle owner walked in, then run
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
            if (station == homeStation) continue;
            if (station.Progress <= 1f) continue;
            if (station.IsGuarded) continue;

            viable.Add(station);
        }

        if (viable.Count == 0) return null;

        return viable[Random.Range(0, viable.Count)];
    }

    // Navigation

    private void BuildRoute(Transform destination)
    {
        route.Clear();
        routeIndex = 0;

        if (destination == null) return;

        Waypoint from = Waypoint.NearestVisible(rb.position, sightBlockers);
        Waypoint to   = Waypoint.NearestVisible(destination.position, sightBlockers);

        if (from == null || to == null)
        {
            if (logRoutes)
            {
                Debug.LogWarning($"{name}: could not find waypoints. " +
                                 "Bot will walk in a straight line.", this);
            }
            return;
        }

        route = Waypoint.FindPath(from, to);

        if (!logRoutes) return;

        if (route.Count == 0)
        {
            Debug.LogWarning(
                $"{name}: NO ROUTE from {from.name} to {to.name} — the graph is " +
                "disconnected between those two nodes.", this);
        }
        else
        {
            Debug.Log($"{name}: route to {destination.name} via {route.Count} nodes",
                      this);
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
                bool headingHome = state == State.Fleeing || state == State.Returning;
                BuildRoute(headingHome ? homeDesk : TargetPoint);
            }
        }

        stuckTimer = 0f;
        lastCheckPosition = rb.position;
    }

    // Detection

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