using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [Header("Stats Bars")] 
    [SerializeField] Slider healthBar;
    [SerializeField] Slider manaBar;
    [SerializeField] Slider staminaBar;
    
    [Header("Stats Values")] 
    [SerializeField] TextMeshProUGUI  healthText;
    [SerializeField] TextMeshProUGUI  manaText;
    [SerializeField] TextMeshProUGUI  staminaText;
    
    [Header("ScoreHub")] 
    [SerializeField] TextMeshProUGUI  coinQuantityText;
    [SerializeField] TextMeshProUGUI  artifactsQuantityText;
    [SerializeField] TextMeshProUGUI  candysQuantityText;
    [SerializeField] TextMeshProUGUI  generalSocreText;
    
    GameController gameController;
    
    // Start is called before the first frame update
    void Start()
    {
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (gameController.IsPlayerAlive)
        {
            UpdatePlayerStatsValues();
            UpdateScores();
            UpdateMaxValues();
        }
    }
    
    private void UpdatePlayerStatsValues()
    {
        healthText.text = gameController.Health.ToString();
        healthBar.value = gameController.Health;
        
        manaText.text = gameController.Mana.ToString();
        manaBar.value = gameController.Mana;
        
        staminaText.text = gameController.Stamina.ToString();
        staminaBar.value = gameController.Stamina;
    }

    private void UpdateMaxValues()
    {
        healthBar.maxValue = gameController.MaxHealth;
        manaBar.maxValue = gameController.MaxMana;
        staminaBar.maxValue = gameController.MaxStamina;
    }

    private void UpdateScores()
    {
        coinQuantityText.text = gameController.Coins.ToString();
        artifactsQuantityText.text = gameController.Artifacts.ToString();
        candysQuantityText.text = gameController.Candys.ToString();
        generalSocreText.text = gameController.GeneralScore.ToString();
    }
}
