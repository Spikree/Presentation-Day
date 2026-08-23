using UnityEngine;

public class PlayerComputer : MonoBehaviour

{

private float percentPerSecond = 5f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PercentageManager.Instance.AddPercentage(percentPerSecond * Time.deltaTime);
        }

       
    }
}