using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Treasure : MonoBehaviour
{
    [SerializeField] TreasureClassification treasureType;
    [SerializeField] int quantity;
    [SerializeField] int scoreValue = 100;
    
    GameObject controller;
    GameController gameController;
    
    private System.Random random;
    
    void Start()
    {
        random = new System.Random();
        controller = GameObject.Find("GameController");
        gameController = controller.GetComponent<GameController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            validateTreasureType();
        }
    }

    private void validateTreasureType()
    {
        switch (treasureType)
        {
            case TreasureClassification.Coin:
            case TreasureClassification.PileOfCoins:
            case TreasureClassification.CoinsChest:
            case TreasureClassification.CoinsBox:
                gameController.Coins += quantity;
                gameController.GeneralScore += scoreValue;
                break;
            case TreasureClassification.Artifact:
                gameController.Artifacts += quantity;
                gameController.GeneralScore += scoreValue;
                break;
            case TreasureClassification.HiddenStash:
                // Es un escondite
                break;
            case TreasureClassification.GeneralChest:
                // Es un cofre general
                break;
            case TreasureClassification.CandyCorn:
                gameController.Candys += quantity;
                gameController.GeneralScore += scoreValue;
                break;
            case TreasureClassification.Collectible:
                gameController.Candys += quantity;
                gameController.GeneralScore += scoreValue;
                break;
            case TreasureClassification.Key:
                // Es una llave
                break;
            case TreasureClassification.BunchOfKeys:
                // Es un manojo de llaves
                break;
        }
    }
}

public enum TreasureClassification
{
    Coin,
    PileOfCoins,
    CoinsChest,
    CoinsBox,
    Artifact,
    HiddenStash,
    GeneralChest,
    CandyCorn,
    Collectible,
    Key,
    BunchOfKeys
}