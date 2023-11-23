using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Potion : MonoBehaviour
{
    [SerializeField] potionTypes potionType;
    [SerializeField] int effectValue;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            Debug.Log("Potion");
            // Accion a desencadenar
        }
    }
}

public enum potionTypes
{
    Health,
    Estamina,
    Mana,
    Damage,
    Speed,
    Defense,
    Special
}
