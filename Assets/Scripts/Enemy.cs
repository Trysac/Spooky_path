using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] Collider attackRange;

    [Header("Movement Configuration")] 
    [SerializeField] MovementConfiguration movementConfiguration;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runningSpeed = 8f;    
    [SerializeField] Vector3 initialPoint;
    [SerializeField] Vector3 destinationPoint;
    [SerializeField] float patrollingWaitBetweenPoints = 1f;
    [SerializeField] float maxPatrollingWaitBetweenPoints = 1f;

    // Control variables
    private bool isAlive;
    private bool isMoving;
    private bool isRunning;
    private bool isAttacking;
    private bool isPlayerInAttackRange;

    // References
    private Player player;
    private Transform playerTramsform;
    
    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
        initialPoint = transform.position;
        player = GameObject.FindGameObjectWithTag("Player").gameObject.GetComponent<Player>();
        playerTramsform = player.transform;

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
        transform.position 
            = Vector3.MoveTowards(transform.position, destinationPoint, moveSpeed * Time.deltaTime);
        if (transform.position == destinationPoint)
        {
            Vector3 temp = initialPoint;
            initialPoint = destinationPoint;
            destinationPoint = temp;
            StartCoroutine(StopForSeconds(patrollingWaitBetweenPoints));
        }
    }

    private void SurveillanceAndPatrolling()
    {
        transform.position = Vector3.MoveTowards(transform.position, destinationPoint, moveSpeed * Time.deltaTime);

        if (transform.position == destinationPoint)
        {
            Vector3 temp = initialPoint;
            initialPoint = destinationPoint;
            destinationPoint = temp;

            StartCoroutine(StopForSeconds(Random.Range(patrollingWaitBetweenPoints, maxPatrollingWaitBetweenPoints)));
        }
    }

    private void Static()
    {
        Debug.Log("Static");
    }

    private void Pursuit()
    {
        playerTramsform = player.transform;

        if (isPlayerInAttackRange)
        {
            transform.LookAt(playerTramsform);
            transform.Translate(Vector3.forward * runningSpeed * Time.deltaTime);
        }
    }

    private void PatrollingAndPursuit()
    {
        playerTramsform = player.transform;
        if (isPlayerInAttackRange)
        {
            transform.LookAt(playerTramsform);
            transform.Translate(Vector3.forward * runningSpeed * Time.deltaTime);
        }
        else
        {
            transform.position 
                = Vector3.MoveTowards(transform.position, destinationPoint, moveSpeed * Time.deltaTime);

            if (transform.position == destinationPoint)
            {
                Vector3 temp = initialPoint;
                initialPoint = destinationPoint;
                destinationPoint = temp;
                StartCoroutine(StopForSeconds(patrollingWaitBetweenPoints));
            }
        }
    }
    
    IEnumerator StopForSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
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
