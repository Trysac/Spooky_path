using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelfDestroy : MonoBehaviour
{
    [SerializeField] int damage = 10;
    [SerializeField] float duration = 0.1f;
    
    private bool hit;
    private Player Player;
    
    // Start is called before the first frame update
    void Start()
    {
        hit = false;
        Player = GameObject.Find("Player").GetComponent<Player>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.transform.tag.Equals("Enemy"))
        {
            other.gameObject.GetComponent<Enemy>().TakeDamage(damage);
        }
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
    
    // IEnumerator SelfDestruction(float delay)
    // {
    //     yield return new WaitForSeconds(delay);
    //     Player.IsAlreadyAttacking = false;
    //     Player.IsAttacking = false;
    //     Player.SetAnimationVariablesToFalse();
    //     Destroy(gameObject);
    // }
}
