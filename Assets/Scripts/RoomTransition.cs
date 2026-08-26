using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoomTransition : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("Scene to load additively. Leave empty if the destination is " +
             "already in the current scene.")]
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

        GameObject spawn = GameObject.Find(spawnPointName);

        if (spawn == null)
        {
            Debug.LogError(
                $"{name}: no GameObject called '{spawnPointName}' found. " +
                "Check the name matches and that the scene loaded.", this);

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
        GameObject turnOff = GameObject.Find(cameraToDisable);
        GameObject turnOn  = GameObject.Find(cameraToEnable);

        if (turnOn != null)  turnOn.SetActive(true);
        if (turnOff != null) turnOff.SetActive(false);

        if (turnOn == null)
        {
            Debug.LogWarning(
                $"{name}: no camera called '{cameraToEnable}' found — the view " +
                "will not follow the player into the new room.", this);
        }
    }
}