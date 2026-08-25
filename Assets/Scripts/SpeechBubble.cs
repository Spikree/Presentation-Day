using UnityEngine;
using TMPro;

public class SpeechBubble : MonoBehaviour
{
    [SerializeField] private TMP_Text bubbleText;

    private void Reset()
    {
       
        if (bubbleText == null)
            bubbleText = GetComponentInChildren<TMP_Text>();
    }

    public void SetLine(string text)
    {
        bubbleText.SetText(text);
    }
}