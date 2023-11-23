using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    // Player stats
    [SerializeField] int health = 100;
    [SerializeField] int mana = 100;
    [SerializeField] int stamina = 100;

    //Player score
    [SerializeField] int coins;
    [SerializeField] int artifacts;
    [SerializeField] int candys;
    [SerializeField] int generalScore;




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // Setters and Getters
    public int Coins { get => coins; set => coins = value; }
    public int Artifacts { get => artifacts; set => artifacts = value; }
    public int Candys { get => candys; set => candys = value; }
    public int GeneralScore { get => generalScore; set => generalScore = value; }
    public int Health { get => health; set => health = value; }
    public int Mana { get => mana; set => mana = value; }
    public int Stamina { get => stamina; set => stamina = value; }
}
