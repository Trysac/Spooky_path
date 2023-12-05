using System;
using System.Collections;
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
    
    [Header("Effects")] 
    [SerializeField] GameObject damageEffect;
    [SerializeField] GameObject deadEffect;
    [SerializeField] AudioSource source;
    [SerializeField] AudioClip[] sounds;
    
    private bool isAlive;
    private bool isAlreadyDead;
    
    private bool isMoving;
    private bool isRunning;
    
    private bool isJumping;
    private bool isOnTheGround;
    private int jumps;
    
    private bool isAttacking;
    private bool isAlreadyAttacking;

    private bool isDefenseShieldActivate;

    GameController gameController;

    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
        isAlreadyDead = false;
        isDefenseShieldActivate = false;
        animator.SetBool("isAlive",isAlive);
        isOnTheGround = true;
        gameController = GameObject.Find("GameController").GetComponent<GameController>();
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlive)
        {
            if (isDefenseShieldActivate)
            {
                StartCoroutine(Deactivatedefense());
            }

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
        if (isAlreadyAttacking)
        {
            return;
        }
        
        StopAttacksSoundEffects();
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (!UseMana(effectsManager.Effects[0].Cost))
            {
                return;
            }

            InstantiateObjectAtPosition(
                effectsManager.Effects[0].Vxf, 
                effectsManager.Effects[0].SpawPoint.position, 
                1, 
                effectsManager.Effects[0].Duration);
            effectsManager.MainSound.clip = effectsManager.Effects[0].EffectSound;
            effectsManager.MainSound.Play();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (!UseMana(effectsManager.Effects[1].Cost))
            {
                return;
            }
            
            InstantiateObjectAtPosition(
                effectsManager.Effects[1].Vxf, 
                effectsManager.Effects[1].SpawPoint.position, 
                2, 
                effectsManager.Effects[1].Duration);
            effectsManager.MainSound.clip = effectsManager.Effects[1].EffectSound;
            effectsManager.MainSound.Play();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            
            if (!UseMana(effectsManager.Effects[2].Cost))
            {
                return;
            }
            
            InstantiateObjectAtPosition(
                effectsManager.Effects[2].Vxf, 
                effectsManager.Effects[2].SpawPoint.position, 
                3, 
                effectsManager.Effects[2].Duration);
            effectsManager.MainSound.clip = effectsManager.Effects[2].EffectSound;
            effectsManager.MainSound.Play();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            if (!UseMana(effectsManager.Effects[3].Cost))
            {
                return;
            }
            
            InstantiateObjectAtPosition(
                effectsManager.Effects[3].Vxf, 
                effectsManager.Effects[3].SpawPoint.position, 
                4, 
                effectsManager.Effects[3].Duration);
            effectsManager.MainSound.clip = effectsManager.Effects[3].EffectSound;
            effectsManager.SecondaySound.clip = effectsManager.Effects[2].SecondaryEffectSound;
            effectsManager.MainSound.Play();
            effectsManager.SecondaySound.Play();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            if (!UseMana(effectsManager.Effects[4].Cost))
            {
                return;
            }
            
            InstantiateObjectAtPosition(
                effectsManager.Effects[4].Vxf, 
                effectsManager.Effects[4].SpawPoint.position, 
                5, 
                effectsManager.Effects[4].Duration);
            effectsManager.MainSound.clip = effectsManager.Effects[4].EffectSound;
            effectsManager.SecondaySound.clip = effectsManager.Effects[4].SecondaryEffectSound;
            effectsManager.MainSound.Play();
            effectsManager.SecondaySound.Play();
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            if (!UseMana(effectsManager.Effects[5].Cost))
            {
                return;
            }
            
            InstantiateObjectAtPosition(
                effectsManager.Effects[5].Vxf, 
                effectsManager.Effects[5].SpawPoint.position, 
                6, 
                effectsManager.Effects[5].Duration);
            effectsManager.MainSound.clip = effectsManager.Effects[5].EffectSound;
            effectsManager.MainSound.Play();
            if (gameController.Health + 40 > gameController.MaxHealth)
            {
                gameController.Health = gameController.MaxHealth;
            }
            else
            {
                gameController.Health += 40;
            }
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            if (!UseMana(effectsManager.Effects[6].Cost))
            {
                return;
            }
            
            InstantiateObjectAtPosition(
                effectsManager.Effects[6].Vxf, 
                effectsManager.Effects[6].SpawPoint.position, 
                7, 
                effectsManager.Effects[6].Duration);
            effectsManager.MainSound.clip = effectsManager.Effects[6].EffectSound;
            effectsManager.SecondaySound.clip = effectsManager.Effects[6].SecondaryEffectSound;
            effectsManager.MainSound.Play();
            effectsManager.SecondaySound.Play();
            
        }
    }
    
    private void InstantiateObjectAtPosition(GameObject prefab, Vector3 position, int espAttackID, float delayToDestroy)
    { 
        StopMovement();
        isAttacking = true;
        isAlreadyAttacking = true;
        animator.SetBool("isAttacking",isAttacking);
        animator.SetInteger("EspAttack",espAttackID);
        GameObject instantiatedObject = Instantiate(prefab, position, transform.rotation);
        instantiatedObject.SetActive(true);
        instantiatedObject.GetComponent<SelfDestroy>().DestroyWithDelay(delayToDestroy);
        if (espAttackID.Equals(7) | espAttackID.Equals(6) | espAttackID.Equals(3))
        {
            instantiatedObject.transform.parent = transform;
        }
    }
    
    private void InstantiateObjectAtPosition()
    { 
        GameObject instantiatedObject = Instantiate(damageEffect, transform.position, transform.rotation);
        instantiatedObject.SetActive(true);
        instantiatedObject.GetComponent<SelfDestroy>().DestroyWithDelay(1);
        instantiatedObject.transform.parent = transform;
    }
    
    private void InstantiateDeadObjectAtPosition()
    { 
        GameObject instantiatedObject = Instantiate(deadEffect, transform.position, transform.rotation);
        instantiatedObject.SetActive(true);
        instantiatedObject.transform.parent = transform;
    }
    
    // Set all moving variables to false
    private void StopMovement()
    {
        isMoving = false;
        isRunning = false;
        animator.SetBool("isRunning",isRunning);
        animator.SetBool("isMoving",isMoving);
    }

    private void StopAttacksSoundEffects()
    {
        effectsManager.MainSound.Stop();
        effectsManager.SecondaySound.Stop();
    }

    public void Die()
    {
        if (!isAlreadyDead)
        {
            isAlreadyDead = true; 
            isAlive = false;
            StopMovement();
            InstantiateDeadObjectAtPosition();
            source.clip = sounds[1];
            source.Play();
            animator.SetBool("isAlive",isAlive);
        }
    }

    public void SetAnimationVariablesToFalse()
    {
        animator.SetBool("isAttacking", isAttacking);
        animator.SetInteger("EspAttack",0);
    }

    private bool UseMana(int mana)
    {
        if (mana > gameController.Mana)
        {
            // Play Clip
            return false;
        }
        else
        {
            gameController.Mana -= mana;
            return true;
        }
    }
    
    public void TakeDamage(int damage)
    {
        if (isDefenseShieldActivate)
        {
            return;
        }

        if (damage >= gameController.Health)
        {
            Die();
        }
        else
        {
            source.clip = sounds[0];
            source.Play();
            gameController.Health -= damage;
            InstantiateObjectAtPosition();
        }
    }
    
    IEnumerator Deactivatedefense()
    {
        yield return new WaitForSeconds(5f);
        isDefenseShieldActivate = false;
    }
    
    
    /// ////////////////////////////////
    // Setters and Getters for Player states - Properties
    /// ////////////////////////////////
    public bool IsAlive { get => isAlive; set => isAlive = value; }
    public bool IsMoving { get => isMoving; set => isMoving = value; }
    public bool IsRunning { get => isRunning; set => isRunning = value; }
    public bool IsJumping { get => isJumping; set => isJumping = value; }
    public bool IsAttacking { get => isAttacking; set => isAttacking = value; }
    public bool IsAlreadyAttacking { get => isAlreadyAttacking; set => isAlreadyAttacking = value; }
}
