using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] Transform playerTransform;
    [SerializeField] float lateralDistance = 5f;
    [SerializeField] float height = 8f;
    [SerializeField] float smoothSpeed = 10f;
    [SerializeField] float initialrotation_Y = 180f;

    void Start()
    {

    }
    
    void Update()
    {
        if (playerTransform != null)
        {
            // Calculate the desired camera position
            Vector3 desiredPosition = playerTransform.position - playerTransform.forward * lateralDistance + Vector3.up * height;
    
            // Use SmoothDamp function to smooth camera movement
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    
            // Set the camera position
            transform.position = smoothedPosition;
    
            // Make the camera look at the player
            transform.LookAt(playerTransform.position + Vector3.up * height);
        }
    }
    
    // void LateUpdate()
    // {
    //     transform.rotation = Quaternion.Euler(0, initialrotation_Y, 0);
    //     if (playerTransform != null)
    //     {
    //         // Calculate the desired camera position
    //         Vector3 desiredPosition = playerTransform.position - playerTransform.forward * lateralDistance + Vector3.up * height;
    //
    //         // Use SmoothDamp function to smooth camera movement
    //         Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    //
    //         // Set the camera position
    //         transform.position = smoothedPosition;
    //
    //         // Make the camera look at the player
    //         transform.LookAt(playerTransform.position + Vector3.up * height);
    //     }
    // }
} 

    

