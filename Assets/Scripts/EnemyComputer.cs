using UnityEngine;

public class EnemyComputer : MonoBehaviour

{

private float percentPerSecond = 5f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            EnemyPercentManager.Instance.MinusPercentage(percentPerSecond * Time.deltaTime);
        }

        else
        {
            EnemyPercentManager.Instance.AddPercentage(percentPerSecond * Time.deltaTime);
        }
    }
}