using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Fireball : MonoBehaviour
{
    public Vector3 velocity;

    private float realtimeAtInstantiating;

    // Start is called before the first frame update
    void Start()
    {
        realtimeAtInstantiating = Time.realtimeSinceStartup;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        this.transform.position += velocity * Time.deltaTime;
        if((Time.realtimeSinceStartup - realtimeAtInstantiating) > Globals.singleton.fireBallLifeTime)
        {
            explode();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("player"))
        {
            explode();
        }
    }

    private void explode()
    {
        GameObject explosion = GameObject.Instantiate(Globals.singleton.fireBallExplosionPrefab);
        explosion.transform.position = this.transform.position;
        GameObject.Destroy(this.gameObject);
    }
}
