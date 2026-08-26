using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public class RoomTransition : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Scene to load additively. Leave EMPTY if the destination scene " +
             "is already loaded — e.g. the door going back to the office.")]
    [SerializeField] private string sceneToLoad = "ToiletScene";

    [Tooltip("Name of the empty GameObject to drop the player on. Must be " +
             "unique across every loaded scene.")]
    [SerializeField] private string spawnPointName = "ToiletSpawn";

    [Header("Cameras")]
    [Tooltip("Name of the CinemachineCamera GameObject to switch ON.")]
    [SerializeField] private string cameraToEnable = "ToiletCamera";

    [Tooltip("Name of the CinemachineCamera GameObject to switch OFF.")]
    [SerializeField] private string cameraToDisable = "CinemachineCamera";

    [Header("Timing")]
    [Tooltip("Seconds before any doorway can fire again. Stops the arrival " +
             "doorway instantly sending the player back where they came from.")]
    [SerializeField] private float retriggerDelay = 1f;

    private static float nextAllowedTime;
    private static bool busy;

    private void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (busy || Time.time < nextAllowedTime) return;

        StartCoroutine(Transition(other.transform));
    }

    private IEnumerator Transition(Transform player)
    {
        busy = true;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            Scene destination = SceneManager.GetSceneByName(sceneToLoad);

            if (!destination.isLoaded)
            {
                yield return SceneManager.LoadSceneAsync(
                    sceneToLoad, LoadSceneMode.Additive);
            }
        }

        GameObject spawn = FindIncludingInactive(spawnPointName);

        if (spawn == null)
        {
            Debug.LogError(
                $"{name}: no GameObject called '{spawnPointName}' found. Check " +
                "the name matches exactly and that the scene loaded.", this);

            busy = false;
            yield break;
        }

        MovePlayer(player, spawn.transform.position);
        SwitchCameras();

        nextAllowedTime = Time.time + retriggerDelay;
        busy = false;
    }

    private void MovePlayer(Transform player, Vector3 destination)
    {
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();

        if (body != null)
        {
            body.linearVelocity = Vector2.zero;
            body.position = destination;
        }

        player.position = destination;
    }

    private void SwitchCameras()
    {
        GameObject turnOn  = FindIncludingInactive(cameraToEnable);
        GameObject turnOff = FindIncludingInactive(cameraToDisable);

        if (turnOn != null)  turnOn.SetActive(true);
        if (turnOff != null) turnOff.SetActive(false);

        if (turnOn == null && !string.IsNullOrEmpty(cameraToEnable))
        {
            Debug.LogWarning(
                $"{name}: no camera called '{cameraToEnable}' found — the view " +
                "will not follow the player into the new room.", this);
        }
    }


    private static GameObject FindIncludingInactive(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name != targetName) continue;
            if (!t.gameObject.scene.IsValid()) continue;   // it's an asset, not in a scene

            return t.gameObject;
        }

        return null;
    }
}