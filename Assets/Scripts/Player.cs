using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runningSpeed = 8f;
    [SerializeField] float jumpForce = 250f;
    [SerializeField] float rotationSpeed = 720f;
    [SerializeField] int maxJump = 2;
    
    [SerializeField] BoxCollider boxCollider;
    [SerializeField] Rigidbody rigidbody;
    [SerializeField] Animator animator;
    
    [SerializeField] Transform model3D;

    private bool isAlive;
    
    private bool isMoving;
    private bool isRunning;
    
    private bool isJumping;
    private bool isOnTheGround;
    private int jumps;
    
    private bool isAttacking;
    private bool isAlreadyAttacking;


    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
        isOnTheGround = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlive)
        {
            BasicAttack();
            Move();
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

    private void BasicAttack()
    {
        if (!isAlreadyAttacking && Input.GetKeyDown(KeyCode.Alpha1))
        {
            StopMovement();
            isAttacking = true;
            isAlreadyAttacking = true;
            animator.SetBool("isAttacking",isAttacking);
        }

        CheckIfAttacking();
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

    // Set all moving variables to false
    private void StopMovement()
    {
        isMoving = false;
        isRunning = false;
        animator.SetBool("isRunning",isRunning);
        animator.SetBool("isMoving",isMoving);
    }

    
    /// ////////////////////////////////
    // Setters and Getters for Player states - Properties
    /// ////////////////////////////////
    public bool IsAlive { get => isAlive; set => isAlive = value; }
}
