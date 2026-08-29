using UnityEngine;

public class OfficeNeedsDrainer : MonoBehaviour

{

[SerializeField] private float percentPerSecond = 0.5f;

    void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            ToiletManager.Instance.MinusPercentage(percentPerSecond * Time.deltaTime);
        }

    }
}