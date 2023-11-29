using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lava : MonoBehaviour
{
    [Header("Animation Configuration")] 
    [SerializeField] float X_Speed = 0.008f;
    [SerializeField] float Y_Speed;
    [SerializeField] private Renderer renderer;
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        renderer.material.mainTextureOffset 
            = new Vector2(
                Time.time * X_Speed, 
                Time.time * Y_Speed);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Player"))
        {
            Debug.Log("DEAD");
            other.GetComponent<Player>().IsAlive = false;
        }
    }
}
