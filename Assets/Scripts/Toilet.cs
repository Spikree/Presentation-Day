using UnityEngine;

public class Toilet : MonoBehaviour
{
 
private float percentPerSecond = 5f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ToiletManager.Instance.AddPercentage(percentPerSecond * Time.deltaTime);
        }
    }
}
