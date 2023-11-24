using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float runningSpeed = 8f;
    [SerializeField] float jumpForce = 250f;
    [SerializeField] float rotationSpeed = 720f;
    
    [SerializeField] BoxCollider boxCollider;
    [SerializeField] Rigidbody rigidbody;
    [SerializeField] Animator animator;
    
    [SerializeField] Transform model3D;

    private bool isAlive;
    bool isMoving;
    bool isJumping;
    bool isRunning;

    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
        isJumping = false;
        isRunning = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlive)
        {
            Move();
            Jump();
        }
    }

    /// <summary>
    /// Genereal Player actions (Functions):
    /// 1. Move 
    ///     1.1 Walk
    ///     1.2 Run
    /// 2. Jump
    /// </summary>

    // Validate if the player is moving, and call the respective funtion of walk() or run()
    private void Move()
    {
        if (rigidbody.velocity.magnitude > 0) {
            isMoving = true;
            if (Input.GetKeyDown(KeyCode.LeftShift) | Input.GetKeyDown(KeyCode.RightShift))
            {
                isRunning = true;
            }
            else { isRunning = false; }
        }
        else
        {
            isMoving = false;
            isRunning = false;
            // animator.SetTrigger("StopAllAnimations");
            animator.Play("Idle");
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
        // Obtiene la entrada del usuario
        float zInput = Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed;
        float xInput = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        // Mueve el personaje
        transform.Translate(xInput, 0, zInput);
    }

    private void Run()
    {
        // Obtiene la entrada del usuario
        float zInput = Input.GetAxis("Horizontal") * Time.deltaTime * runningSpeed;
        float xInput = Input.GetAxis("Vertical") * Time.deltaTime * runningSpeed;

        // Mueve el personaje
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
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // animator.SetTrigger("StopAllAnimations");
            animator.Play("Jump");
            rigidbody.AddForce(Vector3.up * jumpForce);
        }
    }



    /// ////////////////////////////////
    // Setters and Getters for Player states 
    /// ////////////////////////////////
    
    public void SetIsAlive(Boolean state)
    {
        isAlive = state;
    }
    public Boolean GetIsAlive()
    {
        return isAlive;
    }
}
