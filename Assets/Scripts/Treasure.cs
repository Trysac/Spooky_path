using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Treasure : MonoBehaviour
{
    [Header("General Stats")] 
    [SerializeField] TreasureClassification treasureType;
    [SerializeField] int quantity;
    [SerializeField] int scoreValue = 100;
    [SerializeField] float delayToDestroy = 0.1f;
    
    GameController gameController;
    System.Random random;
    
    private bool wasUsed;
    
    void Start()
    {
        wasUsed = false;
        random = new System.Random();
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            if (!wasUsed)
            {
                wasUsed = true;
                validateTreasureType();
                DestroyAfterDelay();
            }
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
    
    private void DestroyAfterDelay()
    {
        Destroy(gameObject, delayToDestroy);
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