using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Potion : MonoBehaviour
{
    [SerializeField] potionTypes potionType;
    [SerializeField] int effectValue;

    GameObject controller;
    GameController gameController;

    // Start is called before the first frame update
    void Start()
    {
        controller = GameObject.Find("GameController");
        gameController = controller.GetComponent<GameController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            switch (potionType)
            {
                case PotionType.Health:
                    // La poción es de salud
                    break;
                case PotionType.Estamina:
                    // La poción es de estamina
                    break;
                case PotionType.Mana:
                    // La poción es de maná
                    break;
                case PotionType.Damage:
                    // La poción es de daño
                    break;
                case PotionType.Speed:
                    // La poción es de velocidad
                    break;
                case PotionType.Defense:
                    // La poción es de defensa
                    break;
                case PotionType.Special:
                    // La poción es especial
                    break;
                default:
                    Debug.Log("La poción no es de un tipo válido");
                    break;
            }

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
