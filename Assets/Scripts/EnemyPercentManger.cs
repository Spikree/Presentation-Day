using UnityEngine;
using TMPro;

public class EnemyPercentManager : MonoBehaviour
{
    public float percentage; // stores the score - now per-instance, not shared
    public TMP_Text percentText; // ui text display

    void Start()
    {
        percentage = 100; // sets % to 100 at the beginning of the game
        UpdateScoreUI(); // update the UI with the initial score
    }

    public void AddPercentage(float amount)
    {
        percentage += amount; // adds points to the %
        UpdateScoreUI(); // updates the UI every time the % changes
    }

    public void MinusPercentage(float amount)
    {
        percentage -= amount; // subtracts points from the %
        UpdateScoreUI(); // updates the UI every time the % changes
    }

    void UpdateScoreUI()
    {
        percentText.text = "" + Mathf.FloorToInt(percentage) + "%"; // display the % in the UI
    }
}