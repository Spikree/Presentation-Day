using UnityEngine;

public class EnemyComputer : MonoBehaviour
{
    public EnemyPercentManager percentManager; // drag this enemy's PercentManager object here in the Inspector
    private float percentPerSecond = 5f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            percentManager.MinusPercentage(percentPerSecond * Time.deltaTime);
        }

         if (other.CompareTag("Enemy"))
        {
            percentManager.AddPercentage(percentPerSecond * Time.deltaTime);
        }
    }
}