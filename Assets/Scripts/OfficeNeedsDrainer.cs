using UnityEngine;

public class OfficeNeedsDrainer : MonoBehaviour

{

private float percentPerSecond = 2f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ToiletManager.Instance.MinusPercentage(percentPerSecond * Time.deltaTime);
        }

    }
}