using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

public class Enemy : MonoBehaviour
{
    [Header("General Stats")] 
    [SerializeField] EnemyType enemyType;
    [SerializeField] int health = 100;
    [SerializeField] int scoreValue = 100;
    [SerializeField] float delayToDestroy = 3f;

    [Header("Attack Configuration")] 
    [SerializeField] int attackDamge = 10;
    [SerializeField] float delayBetweenAttacks = 0.1f;
    [SerializeField] Collider attackRange;
    [SerializeField] float delayToStopAttacking = 0.5f;

    [Header("Movement Configuration")] 
    [SerializeField] MovementConfiguration movementConfiguration;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runningSpeed = 8f;    
    [SerializeField] Vector3 initialPoint;
    [SerializeField] Vector3 destinationPoint;
    [SerializeField] float patrollingWaitBetweenPoints = 1f;
    [SerializeField] float maxPatrollingWaitBetweenPoints = 1f;
    
    [Header("GameObjects References")] 
    [SerializeField] Animator animator;
    [SerializeField] Transform model3D;
    [SerializeField] Rigidbody rigidbody;
    
    // Control variables
    private bool isAlive;
    private bool isAlreadyDead;
    private bool isMoving;
    private bool isRunning;
    private bool isAttacking;
    private bool isAlreadyAttacking;
    private bool isPlayerInAttackRange;

    // References
    private Player player;
    private Transform playerTramsform;
    GameController gameController;
    
    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
        animator.SetBool("isAlive",isAlive);
        initialPoint = transform.position;
        player = GameObject.FindGameObjectWithTag("Player").gameObject.GetComponent<Player>();
        playerTramsform = player.transform;
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
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
        if (other.collider.tag.Equals("Player"))
        {
            Attack();
                
            Debug.Log("DEAD");
            other.gameObject.GetComponent<Player>().IsAlive = false;
        }
    }
    
    private void OnCollisionExit(Collision other)
    {
        if (other.collider.tag.Equals("Player")) {
            StartCoroutine(StopAttackingAfterSeconds(delayToStopAttacking));
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            if (!isPlayerInAttackRange)
            {
                isPlayerInAttackRange = true;
            }
        }
    }
    
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            if (isPlayerInAttackRange)
            {
                isPlayerInAttackRange = false;
            }
        }
    }
    
    private void Rotate()
    {
        if (isMoving || isRunning)
        {
            Vector3 movementDirection;

            switch (movementConfiguration)
            {
                case MovementConfiguration.Patrol:
                case MovementConfiguration.SurveillanceAndPatrolling:
                case MovementConfiguration.PatrollingAndPursuit:
                    movementDirection = (destinationPoint - transform.position).normalized;
                    break;
                case MovementConfiguration.Pursuit:
                    playerTramsform = player.transform;
                    movementDirection = (playerTramsform.position - transform.position).normalized;
                    break;
                default:
                    movementDirection = Vector3.zero;
                    break;
            }

            if (movementDirection != Vector3.zero)
            {
                Quaternion toRotation = Quaternion.LookRotation(movementDirection, Vector3.up);
                model3D.transform.rotation = Quaternion.RotateTowards(model3D.transform.rotation, toRotation, Time.deltaTime);
            }
        }
    }

    private void DetermineMovementPattern()
    {
        Rotate();
        switch (movementConfiguration)
        {
            case MovementConfiguration.Patrol:
                Patrol();
                isMoving = true;
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
        Walk();
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
        Walk();
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
        return;
    }

    private void Pursuit()
    {
        playerTramsform = player.transform;

        if (isPlayerInAttackRange)
        {
            StartRunning();
            transform.LookAt(playerTramsform);
            transform.Translate(Vector3.forward * runningSpeed * Time.deltaTime);
        }
    }

    private void PatrollingAndPursuit()
    {
        playerTramsform = player.transform;
        if (isPlayerInAttackRange)
        {
            StartRunning();
            transform.LookAt(playerTramsform);
            transform.Translate(Vector3.forward * runningSpeed * Time.deltaTime);
        }
        else
        {
            Walk();
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
    
    // Set all moving variables to false
    private void StopMovement()
    {
        isMoving = false;
        isRunning = false;
        animator.SetBool("isRunning",isRunning);
        animator.SetBool("isMoving",isMoving);
    }
    
    private void StartRunning()
    {
        isMoving = true;
        isRunning = true;
        animator.SetBool("isRunning",isRunning);
        animator.SetBool("isMoving",isMoving);
    }
    
    private void Walk()
    {
        isMoving = true;
        animator.SetBool("isMoving",isMoving);
    }
    
    private void Attack()
    {
        StopMovement();
        isAttacking = true;
        animator.SetBool("isAttacking",isAttacking);
    }
    private void StopAttack()
    {
        Walk();
        isAttacking = false;
        animator.SetBool("isAttacking",isAttacking);
    }
    
    public void Die()
    {
        if (!isAlreadyDead)
        {
            isAlreadyDead = true; 
            isAlive = false;
            StopMovement();
            gameController.GeneralScore += scoreValue;
            animator.SetBool("isAlive", isAlive);
            // AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            // audioSource.clip = deathSound;
            // audioSource.Play();
            Destroy(gameObject, delayToDestroy);
        }
    }
    
    IEnumerator StopForSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        StopAttack();
    }
    
    IEnumerator StopAttackingAfterSeconds(float seconds)
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
