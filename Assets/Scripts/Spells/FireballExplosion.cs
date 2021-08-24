using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballExplosion : MonoBehaviour
{
    float realtimeAtInstantiating;

    // Start is called before the first frame update
    void Start()
    {
        Collider[] hit = Physics.OverlapSphere(this.transform.position, Globals.singleton.fireBallExmplosionDamageRadius);
        for(int i = 0; i < hit.Length; i++)
        { 
            EntityAutomaton hitEntity;
            if (hit[i].gameObject.TryGetComponent<EntityAutomaton>(out hitEntity))
            {
                hitEntity.takeDamage(Globals.singleton.fireBallExplosionDamage);
            }
        }

        realtimeAtInstantiating = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if(Time.realtimeSinceStartup - realtimeAtInstantiating > Globals.singleton.fireBallExplosionLifeTime)
        {
            GameObject.Destroy(this.gameObject);
        }
    }
}
