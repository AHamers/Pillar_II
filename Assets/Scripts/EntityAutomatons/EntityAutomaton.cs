using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Reflection;
using TMPro;

public class EntityAutomaton : MonoBehaviour
{
    private EntityState currentEntityState;
    private float currentHealth;
    private GameObject healthBar;
    private GameObject healthBarPrefab;

    public string defaultState;
    public float maxHealth;
    public bool showHealthBar;
    public Vector3 healthBarOffset;

    // Start is called before the first frame update
    void Start()
    {
        healthBar = null;
        if (showHealthBar)
        {
            healthBarPrefab = Globals.singleton.HealthBarPrefab;
            healthBar = GameObject.Instantiate(healthBarPrefab);
        }

        currentHealth = maxHealth;

        Type type = Type.GetType(defaultState);
        EntityState defaultEntityStateObject = (EntityState)(Activator.CreateInstance(type));
        currentEntityState = defaultEntityStateObject;
        currentEntityState.setAutomaton(this);

        currentEntityState.onEnterState();
    }

    // Update is called once per frame
    void Update()
    {
        EntityState newState = currentEntityState.checkTransition();
        if(newState != null)
        {
            currentEntityState.onExitState();
            currentEntityState = newState;
            currentEntityState.setAutomaton(this);
            currentEntityState.onEnterState();
        }

        currentEntityState.updateState();

        if(showHealthBar)
            updateHealthBar();
    }

    private void FixedUpdate()
    {
        currentEntityState.fixedUpdateState();
    }

    public void takeDamage(float damage)
    {
        this.currentHealth -= damage;
        if(currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log(this.gameObject.name + " died.");
            die();
        }
    }

    public void die()
    {
        GameObject dieFX = GameObject.Instantiate(Globals.singleton.deathFXPrefab);
        dieFX.transform.position = this.transform.position;
        dieFX.transform.localScale = this.transform.localScale;
        dieFX.transform.rotation = this.transform.rotation;
        //dieFX.GetComponent<ParticleSystem>().shape.mesh = this.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
        GameObject.Destroy(this.gameObject);
        if(healthBar != null)
        {
            GameObject.Destroy(healthBar);
        }
    }

    private void updateHealthBar()
    {
        healthBar.transform.position = this.transform.position + healthBarOffset;
        healthBar.transform.LookAt(Globals.singleton.player.GetComponent<CameraAutomaton>().playerCamera.transform.position);
        healthBar.transform.Rotate(Vector3.up, 180);
        healthBar.transform.forward -= new Vector3(0, healthBar.transform.forward.y, 0);
        healthBar.GetComponent<TextMeshPro>().text = this.currentHealth + " / " + this.maxHealth;
    }
}
