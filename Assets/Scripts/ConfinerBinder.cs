using UnityEngine;

public class ConfinerBinder : MonoBehaviour
{
    [Tooltip("Name of the GameObject holding the bounding Collider 2D.")]
    [SerializeField] private string boundsObjectName = "CameraBounds_Toilet";

    [Tooltip("Re-check every time this camera is enabled")]
    [SerializeField] private bool rebindOnEnable = true;

    private void OnEnable()
    {
        if (rebindOnEnable) Bind();
    }

    private void Bind()
    {
        GameObject boundsObject = FindIncludingInactive(boundsObjectName);

        if (boundsObject == null)
        {
            Debug.LogWarning(
                $"{name}: no GameObject called '{boundsObjectName}' found yet. ", this);
            return;
        }

        Collider2D bounds = boundsObject.GetComponent<Collider2D>();

        if (bounds == null)
        {
            Debug.LogError(
                $"{name}: '{boundsObjectName}' has no Collider2D to confine to.",
                this);
            return;
        }

        Component confiner = null;

        foreach (Component c in GetComponents<Component>())
        {
            if (c == null) continue;
            if (c.GetType().Name != "CinemachineConfiner2D") continue;

            confiner = c;
            break;
        }

        if (confiner == null)
        {
            Debug.LogError(
                $"{name}: no Cinemachine Confiner 2D on this object. Add one " +
                "before using ConfinerBinder.", this);
            return;
        }

        var property = confiner.GetType().GetProperty("BoundingShape2D")
                       ?? confiner.GetType().GetProperty("m_BoundingShape2D");

        var field = confiner.GetType().GetField("BoundingShape2D")
                    ?? confiner.GetType().GetField("m_BoundingShape2D");

        if (property != null)
        {
            property.SetValue(confiner, bounds);
        }
        else if (field != null)
        {
            field.SetValue(confiner, bounds);
        }
        else
        {
            Debug.LogError(
                $"{name}: could not find the Confiner's bounding shape property.",
                this);
            return;
        }

        var invalidate = confiner.GetType().GetMethod("InvalidateBoundingShapeCache")
                         ?? confiner.GetType().GetMethod("InvalidateCache");

        invalidate?.Invoke(confiner, null);
    }

    private static GameObject FindIncludingInactive(string targetName)
    {
        if (string.IsNullOrEmpty(targetName)) return null;

        foreach (Transform t in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (t.name != targetName) continue;
            if (!t.gameObject.scene.IsValid()) continue;

            return t.gameObject;
        }

        return null;
    }
}