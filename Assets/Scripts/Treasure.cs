using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Treasure : MonoBehaviour
{
    [SerializeField] TreasureClassification treasureType;
    [SerializeField] int value;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            Debug.Log("Treasure");
            // Accion a desencadenar
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
    candyCorn,
    Collectible,
    Key,
    BunchOfKeys
}