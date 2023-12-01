using System;
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement Configuration")] 
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float rotationSpeed = 720f;
    
    [Header("Running Configuration")] 
    [SerializeField] float runningSpeed = 8f;
    [SerializeField] float staminaDecrementSpeed = 1;
    [SerializeField] float staminaRecoverySpeed = 2;
    
    [Header("Jumping Configuration")] 
    [SerializeField] float jumpForce = 250f;
    [SerializeField] int maxJump = 2;
    
    [Header("GameObjects References")] 
    [SerializeField] BoxCollider boxCollider;
    [SerializeField] Rigidbody rigidbody;
    [SerializeField] Animator animator;
    [SerializeField] Transform model3D;
    [SerializeField] EffectsManager effectsManager;
    
    private bool isAlive;
    private bool isAlreadyDead;
    
    private bool isMoving;
    private bool isRunning;
    
    private bool isJumping;
    private bool isOnTheGround;
    private int jumps;
    
    private bool isAttacking;
    private bool isAlreadyAttacking;

    GameController gameController;

    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
        isAlreadyDead = false;
        animator.SetBool("isAlive",isAlive);
        isOnTheGround = true;
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlive)
        {
            checkAttack();
            Move();
            IncreaseStamina();
            Jump();
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.transform.tag.Equals("Floor"))
        {
            isOnTheGround = true;
            isJumping = false;
            animator.SetBool("isJumping",isJumping);
            jumps = 0;
        }
    }

    /// <summary>
    /// Genereal Player actions (Functions):
    /// 1. Move 
    ///     1.1 Walk
    ///     1.2 Run
    /// 2. Jump
    /// 3. Attack
    ///     3.1 Basic
    /// 4. 
    /// </summary>

    // Validate if the player is moving, and call the respective funtion of walk() or run()
    private void Move()
    {
        if (rigidbody.velocity.magnitude > 0) {
            if (!isMoving)
            {
                isMoving = true;
                animator.SetBool("isMoving",isMoving);
            }
            if (Input.GetKey(KeyCode.LeftShift) | Input.GetKey(KeyCode.RightShift))
            {
                if (!isRunning)
                {
                    isRunning = true;
                    animator.SetBool("isRunning",isRunning);
                }
            }
            else
            {
                isRunning = false;
                animator.SetBool("isRunning",isRunning);
            }
        }
        else
        {
            StopMovement();
        }
        

        if (Math.Abs(Input.GetAxis("Horizontal"))  > 0 | Math.Abs(Input.GetAxis("Vertical")) > 0)
        {
            if (isRunning)
            {
                Run();
            }
            else
            {
                Walk();
            }
            RotateCharacter();
        }
    }

    private void Walk()
    {
        // Get User Input
        float zInput = Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed;
        float xInput = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        // character move
        transform.Translate(xInput, 0, zInput);
    }

    private void Run()
    {
        // Get User Input
        float zInput = Input.GetAxis("Horizontal") * Time.deltaTime * runningSpeed;
        float xInput = Input.GetAxis("Vertical") * Time.deltaTime * runningSpeed;
        
        // character move
        transform.Translate(xInput, 0, zInput);
        DecreaseStamina();
    }
    
    private void DecreaseStamina()
    {
        float decrementAmount = staminaDecrementSpeed * Time.deltaTime;
        // Ensure that stamina doesn't go negative
        gameController.Stamina 
            = Mathf.Max(0, gameController.Stamina - Mathf.RoundToInt(decrementAmount)); 
    }
    
    private void IncreaseStamina()
    {
        float recoveryAmount = staminaRecoverySpeed * Time.deltaTime;
        if (!isRunning)
        {
            gameController.Stamina 
                = Mathf.Min(gameController.MaxStamina, gameController.Stamina + Mathf.RoundToInt(recoveryAmount)); 
            
            // if (isMoving | isJumping | isAttacking)
            // {
            //     gameController.Stamina 
            //         = Mathf.Min(gameController.MaxStamina, gameController.Stamina + Mathf.RoundToInt(recoveryAmount)); 
            // }
            // else
            // {
            //     gameController.Stamina 
            //         = Mathf.Min(gameController.MaxStamina, gameController.Stamina + Mathf.RoundToInt(recoveryAmount)); 
            // }
        }
    }
    
    private void RotateCharacter()
    {
        if (isMoving | isRunning)
        {
            Vector3 movementDirection = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            if (movementDirection != Vector3.zero)
            {
                Quaternion toRotation;
                if (Math.Abs(Input.GetAxis("Horizontal"))  > 0)
                {
                    if (Input.GetAxis("Horizontal")  > 0)
                    {
                        toRotation = Quaternion.Euler(0f,270,0f);
                    }
                    else
                    {
                        toRotation = Quaternion.Euler(0f,90,0f);
                    }
                }
                else
                {
                    if (Input.GetAxis("Vertical")  > 0)
                    {
                        toRotation = Quaternion.Euler(0f,0,0f);
                    }
                    else
                    {
                        toRotation = Quaternion.Euler(0f,180,0f);
                    }
                }

                model3D.transform.rotation = Quaternion.RotateTowards(transform.rotation, toRotation, 360);
            }
        }
    }

    private void Jump()
    {
        if (jumps < maxJump)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                StopMovement();
                isJumping = true;
                jumps++;
                isOnTheGround = false;
                animator.SetBool("isJumping",isJumping);
                rigidbody.AddForce(Vector3.up * jumpForce);
            }
        }
    }

    private void checkAttack()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            BasicAttack();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            // HandleButtonPress(2);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            // HandleButtonPress(3);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            // HandleButtonPress(4);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            // HandleButtonPress(5);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            // HandleButtonPress(6);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            // HandleButtonPress(7);
        }
        
        CheckIfAttacking();
    }

    private void BasicAttack()
    {
        if (!isAlreadyAttacking)
        {
            StopMovement();
            isAttacking = true;
            isAlreadyAttacking = true;
            animator.SetBool("isAttacking",isAttacking);
        }
    }

    private void CheckIfAttacking()
    {
        if (isAttacking)
        {
            // Check if the attacking animation has finish
            if (animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1f)
            {
                isAttacking = false;
                isAlreadyAttacking = false;
                animator.SetBool("isAttacking", isAttacking);
            }
        }
    }

    private void SpecialAttackOne()
    {
        // effectsManager.Effects[0].damage;
    }

    // Set all moving variables to false
    private void StopMovement()
    {
        isMoving = false;
        isRunning = false;
        animator.SetBool("isRunning",isRunning);
        animator.SetBool("isMoving",isMoving);
    }

    public void Die()
    {
        if (!isAlreadyDead)
        {
            isAlreadyDead = true; 
            isAlive = false;
            StopMovement();
            // AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            // audioSource.clip = deathSound;
            // audioSource.Play();
            animator.SetBool("isAlive",isAlive);
        }
    }
    
    /// ////////////////////////////////
    // Setters and Getters for Player states - Properties
    /// ////////////////////////////////
    public bool IsAlive { get => isAlive; set => isAlive = value; }
}
