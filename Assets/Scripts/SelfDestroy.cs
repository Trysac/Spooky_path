using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestroy : MonoBehaviour
{
    private Player Player;
    
    // Start is called before the first frame update
    void Start()
    {
        Player = GameObject.Find("Player").GetComponent<Player>();
    }
    
    public void DestroyWithDelay(float delay)
    {
        StartCoroutine(SelfDestruction(delay));
    }
    
    IEnumerator SelfDestruction(float delay)
    {
        yield return new WaitForSeconds(delay);
        Player.IsAlreadyAttacking = false;
        Player.IsAttacking = false;
        Player.SetAnimationVariablesToFalse();
        Destroy(gameObject);
    }
}
