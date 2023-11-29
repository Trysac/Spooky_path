using UnityEngine;

public class Potion : MonoBehaviour
{
    [Header("General Stats")] 
    [SerializeField] potionTypes potion;
    [SerializeField] int effectValue;
    [SerializeField] float delayToDestroy = 0.1f;
    
    GameController gameController;
    System.Random random;

    private bool wasUsed;
    
    // Start is called before the first frame update
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
                ValidateType();
                DestroyAfterDelay();
            }
            
        }
    }

    private void ValidateType()
    {
        switch (potion)
            {
                case potionTypes.Health:
                    if (gameController.Health + effectValue > gameController.MaxHealth)
                    {
                        gameController.Health = gameController.MaxHealth;
                    }
                    else
                    {
                        gameController.Health += effectValue;
                    }
                    break;
                case potionTypes.Stamina:
                    if (gameController.Stamina + effectValue > gameController.MaxStamina)
                    {
                        gameController.Stamina = gameController.MaxStamina;
                    }
                    else
                    {
                        gameController.Stamina += effectValue;
                    }
                    break;
                case potionTypes.Mana:
                    if (gameController.Mana + effectValue > gameController.MaxMana)
                    {
                        gameController.Mana = gameController.MaxMana;
                    }
                    else
                    {
                        gameController.Mana += effectValue;
                    }
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
                case potionTypes.Elixir:
                    int luck = random.Next(0, 6);
                    switch (luck)
                    {   
                        case 1:
                            gameController.MaxHealth += effectValue;
                            gameController.Health += effectValue;
                            break;
                        case 2:
                            gameController.MaxMana += effectValue;
                            gameController.MaxMana += effectValue;
                            break;
                        case 3:
                            gameController.MaxStamina += effectValue;
                            gameController.Stamina += effectValue;
                            break;
                    }
                    break;
                default:
                    Debug.Log("La poción no es de un tipo válido");
                    break;
            }
    }

    private void DestroyAfterDelay()
    {
        Destroy(gameObject, delayToDestroy);
    }
}

public enum potionTypes
{
    Health,
    Stamina,
    Mana,
    Damage,
    Speed,
    Defense,
    Special,
    Elixir   // Increase player atributes
}
