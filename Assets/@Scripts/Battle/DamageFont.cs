using UnityEngine;
using DamageNumbersPro;
public class DamageFont : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    //Assign prefab in inspector.
    public DamageNumber numberPrefab;

    void Update()
    {
        //On leftclick.
        
            //Spawn new popup at transform.position with a random number between 0 and 100.
            DamageNumber damageNumber = numberPrefab.Spawn(transform.position, Random.Range(1,100));
        
    }
}
