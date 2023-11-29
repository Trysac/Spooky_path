using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("General Stats")] 
    [SerializeField] EnemyType enemyType;
    [SerializeField] int health = 100;
    [SerializeField] int scoreValue = 100;
    [SerializeField] float delayToDestroy = 0.1f;

    [Header("Attack Configuration")] 
    [SerializeField] int attackDamge = 10;
    [SerializeField] float delayBetweenAttacks = 0.1f;

    [Header("Movement Configuration")] 
    [SerializeField] MovementConfiguration movementConfiguration;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runningSpeed = 8f;

    // Control variables
    private bool isAlive;
    private bool isMoving;
    private bool isRunning;
    private bool isAttacking;
    
    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlive)
        {
            DetermineMovementPattern();
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (other.collider.tag.Equals("Player")) {
            Debug.Log("DEAD");
            other.gameObject.GetComponent<Player>().IsAlive = false;
        }
    }

    private void DetermineMovementPattern()
    {
        switch (movementConfiguration)
        {
            case MovementConfiguration.Patrol:
                Patrol();
                break;
            case MovementConfiguration.SurveillanceAndPatrolling:
                SurveillanceAndPatrolling();
                break;
            case MovementConfiguration.PatrollingAndPursuit:
                PatrollingAndPursuit();
                break;
            case MovementConfiguration.Pursuit:
                Pursuit();
                break;
            default:
                Static();
                break;
        }
    }

    private void Patrol()
    {
        Debug.Log("Patrolling");
    }

    private void SurveillanceAndPatrolling()
    {
        Debug.Log("Surveillance and Patrolling");
    }

    private void Static()
    {
        Debug.Log("Static");
    }

    private void Pursuit()
    {
        Debug.Log("Pursuit");
    }

    private void PatrollingAndPursuit()
    {
        Debug.Log("Patrolling and Pursuit");
    }
}

public enum EnemyType
{
    BasicSkeleton,
    CommanderSkeleton,
    MageSkeleton,
    ArcherSkeleton,
    Special,
    Boss
}

public enum MovementConfiguration
{
    Patrol,
    SurveillanceAndPatrolling,
    Static,
    Pursuit,
    PatrollingAndPursuit
}
