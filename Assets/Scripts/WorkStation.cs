using UnityEngine;

public class WorkStation : MonoBehaviour
{
    [Tooltip("Tick this on the player's computer. Its progress is read " +
             "from the PercentageManager singleton.")]
    public bool isPlayerStation;

    [Tooltip("For a bot's desk: that bot's EnemyPercentManager. Leave empty " +
             "when Is Player Station is ticked.")]
    public EnemyPercentManager botWork;

    [Tooltip("Whoever owns this desk — the Player for the player's machine, or " +
             "the EnemyBot for a bot's machine. Rivals will not sabotage this " +
             "desk while its owner is standing nearby.")]
    public Transform owner;

    [Tooltip("Where a bot should stand to use this machine. Drag in the " +
             "waypoint beside the desk. Since a bot cant reach the computer " +
             "it can only reach the way point near it ")]
    public Transform approachPoint;

    [Tooltip("How close the owner has to be for this desk to count as guarded.")]
    public float guardRadius = 4f;

    [Tooltip("Free text, shown in the Inspector purely to make wiring easier " +
             "to check. e.g. 'Player' or 'Bot 2'.")]
    public string ownerLabel = "";

    public Transform ApproachPoint => approachPoint != null ? approachPoint : transform;

    public float Progress
    {
        get
        {
            if (isPlayerStation) return PercentageManager.percentage;
            return botWork != null ? botWork.percentage : 0f;
        }
    }

    public bool IsValid => isPlayerStation || botWork != null;


    public bool IsGuarded
    {
        get
        {
            if (owner == null) return false;

            float distance = Vector2.Distance(owner.position, transform.position);
            return distance <= guardRadius;
        }
    }

    public void Drain(float amount)
    {
        if (isPlayerStation)
        {
            if (PercentageManager.Instance != null)
                PercentageManager.Instance.MinusPercentage(amount);
        }
        else if (botWork != null)
        {
            botWork.MinusPercentage(amount);
        }
    }

    public void Restore(float amount)
    {
        if (isPlayerStation)
        {
            if (PercentageManager.Instance != null)
                PercentageManager.Instance.AddPercentage(amount);
        }
        else if (botWork != null)
        {
            botWork.AddPercentage(amount);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (approachPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, approachPoint.position);
            Gizmos.DrawWireSphere(approachPoint.position, 0.25f);
        }

        Gizmos.color = IsGuarded ? Color.red : new Color(1f, 1f, 1f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, guardRadius);
    }
}