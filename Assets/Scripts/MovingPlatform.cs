using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class MovingPlatform : MonoBehaviour
{
    [Header("Movement Configuration")] 
    [SerializeField] MovementType movementType;
    [SerializeField] Vector3 initialPoint;
    [SerializeField] Vector3 finalPoint;
    [SerializeField] float ascendingHeight = 5.0f;
    [SerializeField] float circularRadius = 5.0f;
    
    [Header("Movement Parameters")] 
    [SerializeField] float speed = 0.5f;


    private bool isFalling;
    // Start is called before the first frame update
    void Start()
    {
        initialPoint = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        switch (movementType)
        {
            case MovementType.Lateral:
                MoveLateral();
                break;
            case MovementType.Ascending:
                Ascend();
                break;
            case MovementType.Circular:
                MoveCircular();
                break;
            case MovementType.Falling:
                if (isFalling) { Fall(); }
                break;
        }
    }
    
    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            isFalling = true;
        }
    }
    
    void MoveLateral()
    {
        transform.position = Vector3.Lerp(initialPoint, finalPoint, Mathf.PingPong(Time.time * speed, 1.0f));
    }

    void Ascend()
    {
        float y = initialPoint.y + Mathf.PingPong(Time.time * speed, ascendingHeight);
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    void MoveCircular()
    {
        float angle = Mathf.PingPong(Time.time * speed, 360f);
        float radians = Mathf.Deg2Rad * angle;
        float x = initialPoint.x + Mathf.Cos(radians) * circularRadius;
        float z = initialPoint.z + Mathf.Sin(radians) * circularRadius;
        transform.position = new Vector3(x, transform.position.y, z);
    }

    void Fall()
    {
        // Implement falling logic here
        // For example, make the platform fall downwards
        transform.Translate(Vector3.down * speed * Time.deltaTime);
    }
}

public enum MovementType
{
    Lateral,
    Ascending,
    Circular,
    Falling
}

