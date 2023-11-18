using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] float moveSpeed = 5f;
    [SerializeField] float jumpForce = 250f;
    [SerializeField] BoxCollider boxCollider;
    [SerializeField] Rigidbody rigidbody;

    private bool isAlive;

    // Start is called before the first frame update
    void Start()
    {
        isAlive = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (isAlive) {
            MoveCharacter();
            Jump();
        }
    }

    private void MoveCharacter()
    {
        // Obtiene la entrada del usuario
        float zInput = Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed;
        float xInput = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        // Mueve el personaje
        transform.Translate(xInput, 0, zInput);
    }

    private void Jump() 
    {
        if (Input.GetKeyDown(KeyCode.Space)) {
            rigidbody.AddForce(Vector3.up * jumpForce);
        }
    }



    public void SetIsAlive(Boolean state)
    {
        isAlive = state;
    }
    public Boolean GetIsAlive()
    {
        return isAlive;
    }
}
