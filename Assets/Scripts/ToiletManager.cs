using UnityEngine;
using TMPro;
using UnityEngine.SocialPlatforms.Impl;

public class ToiletManager : MonoBehaviour
{
    public static ToiletManager Instance {get; private set;}
    public static float toiletneed; //stores the score
    public TMP_Text toiletText;  //ui text display

    

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
        toiletneed = 100; // sets % to 100 at the beginning of the game
        UpdateScoreUI(); // update the UI with the initial score
    }

    public void AddPercentage(float amount)
    {
        toiletneed += amount; // adds points to the %
        UpdateScoreUI(); // updates the UI every time the % changes
    }

    
    public void MinusPercentage(float amount)
    {
        toiletneed -= amount; // adds points to the %
        UpdateScoreUI(); // updates the UI every time the % changes
    }

    void UpdateScoreUI()
    {
        toiletText.text = "" + Mathf.FloorToInt(toiletneed) + "%"; // display the % in the UI

    }



}