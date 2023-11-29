using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] EnemyType enemyType;
    [SerializeField] int health = 100;
    [SerializeField] int scoreValue = 100;
    
    [SerializeField] MovementConfiguration movementConfiguration;
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runningSpeed = 8f;

    [SerializeField] float delayBetweenAttacks = 0.1f;
    [SerializeField] float delayToDestroy = 0.1f;
    
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
                // Manejar la configuración de movimiento de patrullaje
                break;
            case MovementConfiguration.SurveillanceAndPatrolling:
                // Manejar la configuración de movimiento de vigilancia y patrullaje
                break;
            case MovementConfiguration.PatrollingAndPursuit:
                // Manejar la configuración de movimiento estático
                break;
            case MovementConfiguration.Pursuit:
                // Manejar la configuración de movimiento de persecución
                break;
            default:
                // This will be for statics or patterns without implemented functionality
                break;
        }
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
