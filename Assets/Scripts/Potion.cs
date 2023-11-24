using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Potion : MonoBehaviour
{
    [SerializeField] potionTypes potion;
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
            switch (potion)
            {
                case potionTypes.Health:
                    // La poción es de salud
                    break;
                case potionTypes.Estamina:
                    // La poción es de estamina
                    break;
                case potionTypes.Mana:
                    // La poción es de maná
                    break;
                case potionTypes.Damage:
                    // La poción es de daño
                    break;
                case potionTypes.Speed:
                    // La poción es de velocidad
                    break;
                case potionTypes.Defense:
                    // La poción es de defensa
                    break;
                case potionTypes.Special:
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
