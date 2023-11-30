using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameOverMenu : MonoBehaviour
{
    [Header("ScoreHub")] 
    [SerializeField] TextMeshProUGUI  coinQuantityText;
    [SerializeField] TextMeshProUGUI  artifactsQuantityText;
    [SerializeField] TextMeshProUGUI  candysQuantityText;
    [SerializeField] TextMeshProUGUI  generalScoreText;
    [SerializeField] TextMeshProUGUI  levelText;
    
    [Header("Buttons")] 
    [SerializeField] Button mainMenuButton;
    [SerializeField] Button playButton;
    [SerializeField] Button exitButton;
    
    [Header("Target Escenes")] 
    [SerializeField] int mainManeEsceneValue;
    [SerializeField] int firstLevelEsceneValue;

    GameController gameController;
    
    private bool hasPressAnyButton;
    
    // Start is called before the first frame update
    void Start()
    {
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
        UpdateScores();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hasPressAnyButton)
        {
            mainMenuButton.onClick.AddListener(LoadMainMenu);
            playButton.onClick.AddListener(LoadFirstLevel);
            exitButton.onClick.AddListener(ExitGame);
        }
    }
    
    private void LoadMainMenu()
    {
        hasPressAnyButton = true;
        SceneManager.LoadScene(mainManeEsceneValue);
    }
    
    private void LoadFirstLevel()
    {
        hasPressAnyButton = true;
        SceneManager.LoadScene(firstLevelEsceneValue);
    }
    
    private void ExitGame()
    {
        hasPressAnyButton = true;
        Application.Quit();
    }
    
    private void UpdateScores()
    {
        coinQuantityText.text = gameController.Coins.ToString();
        artifactsQuantityText.text = gameController.Artifacts.ToString();
        candysQuantityText.text = gameController.Candys.ToString();
        generalScoreText.text = gameController.GeneralScore.ToString();
        levelText.text = "level " + gameController.Level;
    }
}
