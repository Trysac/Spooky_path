using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Player atributes
    [SerializeField] int maxHealth = 100;
    [SerializeField] int maxMana = 100;
    [SerializeField] int maxStamina = 100;
    
    // Player stats
    [SerializeField] int health = 100;
    [SerializeField] int mana = 100;
    [SerializeField] int stamina = 100;

    // Player score
    [SerializeField] int coins;
    [SerializeField] int artifacts;
    [SerializeField] int candys;
    [SerializeField] int generalScore;

    // General Flags
    private bool isPlayerAlive;

    private Player Player;

    // Start is called before the first frame update
    void Start()
    {
        isPlayerAlive = true;
        Player = GameObject.Find("Player").GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!Player.IsAlive)
        {
            isPlayerAlive = false;
        }
    }

    // Setters and Getters - Properties
    public int Coins { get => coins; set => coins = value; }
    public int Artifacts { get => artifacts; set => artifacts = value; }
    public int Candys { get => candys; set => candys = value; }
    public int GeneralScore { get => generalScore; set => generalScore = value; }
    public int Health { get => health; set => health = value; }
    public int Mana { get => mana; set => mana = value; }
    public int Stamina { get => stamina; set => stamina = value; }
    public int MaxHealth { get => maxHealth; set => maxHealth = value; }
    public int MaxMana { get => maxMana; set => maxMana = value; }
    public int MaxStamina { get => maxStamina; set => maxStamina = value; }
    public bool IsPlayerAlive { get => isPlayerAlive; set => isPlayerAlive = value; }
}
