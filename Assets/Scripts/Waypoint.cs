using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class Waypoint : MonoBehaviour
{
    public static readonly List<Waypoint> All = new List<Waypoint>();

    [Tooltip("Waypoints this one connects to.")]
    public List<Waypoint> neighbours = new List<Waypoint>();

    [Tooltip("Half the width of the bot, used when checking whether a link is " +
             "walkable. Raise it if bots clip corners; lower it if valid links " +
             "show as red.")]
    public float clearance = 0.25f;

    private void OnEnable()
    {
        if (!All.Contains(this)) All.Add(this);
    }

    private void OnDisable()
    {
        All.Remove(this);
    }

    public static Waypoint Nearest(Vector2 point)
    {
        Waypoint best = null;
        float bestSqr = float.MaxValue;

        foreach (Waypoint w in All)
        {
            if (w == null) continue;

            float sqr = ((Vector2)w.transform.position - point).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = w;
            }
        }

        return best;
    }

    public static Waypoint NearestVisible(Vector2 point, LayerMask blockers)
    {
        Waypoint best = null;
        float bestSqr = float.MaxValue;

        foreach (Waypoint w in All)
        {
            if (w == null) continue;
            if (IsBlocked(point, w.transform.position, blockers, w.clearance)) continue;

            float sqr = ((Vector2)w.transform.position - point).sqrMagnitude;
            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = w;
            }
        }

        return best != null ? best : Nearest(point);
    }

    public static bool IsBlocked(Vector2 from, Vector2 to, LayerMask blockers, float radius)
    {
        Vector2 offset = to - from;
        float distance = offset.magnitude;

        if (distance < 0.001f) return false;

        return Physics2D.CircleCast(
            from, radius, offset / distance, distance, blockers).collider != null;
    }

    private static IEnumerable<Waypoint> LinkedTo(Waypoint node)
    {
        foreach (Waypoint n in node.neighbours)
        {
            if (n != null) yield return n;
        }

        foreach (Waypoint other in All)
        {
            if (other == null || other == node) continue;
            if (other.neighbours.Contains(node)) yield return other;
        }
    }

    public static HashSet<Waypoint> Reachable(Waypoint start)
    {
        HashSet<Waypoint> seen = new HashSet<Waypoint>();
        if (start == null) return seen;

        Queue<Waypoint> frontier = new Queue<Waypoint>();
        frontier.Enqueue(start);
        seen.Add(start);

        while (frontier.Count > 0)
        {
            Waypoint current = frontier.Dequeue();

            foreach (Waypoint next in LinkedTo(current))
            {
                if (seen.Add(next)) frontier.Enqueue(next);
            }
        }

        return seen;
    }

    public static List<Waypoint> FindPath(Waypoint start, Waypoint goal)
    {
        List<Waypoint> route = new List<Waypoint>();

        if (start == null || goal == null) return route;

        if (start == goal)
        {
            route.Add(goal);
            return route;
        }

        Dictionary<Waypoint, Waypoint> cameFrom = new Dictionary<Waypoint, Waypoint>();
        Queue<Waypoint> frontier = new Queue<Waypoint>();

        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            Waypoint current = frontier.Dequeue();
            if (current == goal) break;

            foreach (Waypoint next in LinkedTo(current))
            {
                if (cameFrom.ContainsKey(next)) continue;
                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        if (!cameFrom.ContainsKey(goal)) return route;

        for (Waypoint node = goal; node != null; node = cameFrom[node])
        {
            route.Add(node);
        }

        route.Reverse();
        return route;
    }

    [ContextMenu("Log Unreachable Nodes")]
    private void LogUnreachable()
    {
        HashSet<Waypoint> reachable = Reachable(this);
        List<string> stranded = new List<string>();

        foreach (Waypoint w in All)
        {
            if (w != null && !reachable.Contains(w)) stranded.Add(w.name);
        }

        if (stranded.Count == 0)
        {
            Debug.Log($"{name}: graph is fully connected — all " +
                      $"{All.Count} nodes reachable.", this);
        }
        else
        {
            Debug.LogWarning(
                $"{name}: {stranded.Count} node(s) NOT reachable from here: " +
                string.Join(", ", stranded), this);
        }
    }


    private void OnDrawGizmos()
    {
        LayerMask obstacles = LayerMask.GetMask("Obstacles");

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);

        foreach (Waypoint n in neighbours)
        {
            if (n == null) continue;

            bool blocked = IsBlocked(
                transform.position, n.transform.position, obstacles, clearance);

            Gizmos.color = blocked ? Color.red : new Color(0f, 1f, 1f, 0.45f);
            Gizmos.DrawLine(transform.position, n.transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        HashSet<Waypoint> reachable = Reachable(this);

        foreach (Waypoint w in All)
        {
            if (w == null || w == this) continue;

            Gizmos.color = reachable.Contains(w) ? Color.green : Color.red;
            Gizmos.DrawSphere(w.transform.position, 0.18f);
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.35f);
    }
}