using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player_BasicMove : MonoBehaviour
{
    [SerializeField] float moveSpeed = 10F;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        MoveCharacter();
    }

    void MoveCharacter()
    {
        // Obtiene la entrada del usuario
        float zInput = Input.GetAxis("Horizontal") * Time.deltaTime * moveSpeed;
        float xInput = Input.GetAxis("Vertical") * Time.deltaTime * moveSpeed;

        // Mueve el personaje
        transform.Translate(xInput, 0, zInput);
    }
}
