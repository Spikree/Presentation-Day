using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;

public class PercentageManager : MonoBehaviour
{
    public static PercentageManager Instance {get; private set;}
    public static float percentage; //stores the score
    public TMP_Text percentText;  //ui text display

    

    void Awake()
    {
              if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;  //allows me to decouple the score from the ui so I can have a two score ui's for ingame and game over

    }

    void Start()
    {
        percentage = 0; // sets % to zero at the beginning of the game
        UpdateScoreUI(); // update the UI with the initial score
    }

    public void AddPercentage(float amount)
    {
        percentage += amount; // adds points to the %
        UpdateScoreUI(); // updates the UI every time the % changes
    }

    
    public void MinusPercentage(float amount)
    {
        percentage -= amount; // adds points to the %
        UpdateScoreUI(); // updates the UI every time the % changes
    }

    void UpdateScoreUI()
    {
        percentText.text = "" + Mathf.FloorToInt(percentage) + "%"; // display the % in the UI

    }



}