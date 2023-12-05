using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    [Header("Player atributes")] 
    [SerializeField] int maxHealth = 100;
    [SerializeField] int maxMana = 100;
    [SerializeField] int maxStamina = 100;
    
    [Header("Player stats")] 
    [SerializeField] int health = 100;
    [SerializeField] int mana = 100;
    [SerializeField] int stamina = 100;
    
    [Header("Player score")] 
    [SerializeField] int coins;
    [SerializeField] int artifacts;
    [SerializeField] int candys;
    [SerializeField] int generalScore;
    [SerializeField] int level;
    
    [Header("Load Scenes Configuration")] 
    [SerializeField] float delayBeforeLoad = 3f;
    [SerializeField] private AudioSource audioSource;

    
    // General Flags
    private bool isPlayerAlive;

    private Player Player;
    
    // Singleton patter
    private static GameController _instancia;
    public static GameController Singlenton
    {
        get
        {
            if (_instancia == null)
            {
                _instancia = FindObjectOfType<GameController>();
                if (_instancia == null)
                {
                    GameObject gameObject = new GameObject("GameController");
                    _instancia = gameObject.AddComponent<GameController>();
                }
            }

            return _instancia;
        }
    }
    void Awake()
    {
        if (_instancia != null && _instancia != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instancia = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        isPlayerAlive = true;
        Player = GameObject.Find("Player").GetComponent<Player>();
    }

    // Update is called once per frame
    void Update()
    {
        CheckIfGameOver();
        if (Player != null && !Player.IsAlive)
        {
            isPlayerAlive = false;
            StartCoroutine(LoadSceneWithDelay(8));
        }
    }

    private void CheckIfGameOver()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentSceneIndex.Equals(8))
        {
            audioSource.Stop();
        }
    }
    
    IEnumerator LoadSceneWithDelay(int sceneIndex)
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        LoadScene(sceneIndex);
    }
    
    void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
        StopAllCoroutines();
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
    public int Level { get => level; set => level = value; }
}
