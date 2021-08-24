using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Globals : MonoBehaviour
{
    public static Globals singleton;

    //========================Global variables========================
    [Header("Shortcuts")]
    public KeyCode forward;
    public KeyCode backward;
    public KeyCode left;
    public KeyCode right;
    [Space(15)]
    public KeyCode sprint;
    public KeyCode jump;
    [Space(15)]
    public KeyCode castFireball;

    [Header("Important GameObjects")]
    public GameObject player;
    public GameObject HealthBarPrefab;
    public GameObject deathFXPrefab;

    [Header("Player control")]
    public float playerWalkSpeed;
    public float playerSprintSpeed;
    public Vector2 jumpForce;
    public float airControl;
    [Space(15)]
    public float cameraGoalReachingSpeed;
    public float cameraSensivity;
    public float zoomSpeed;

    [Header("Spells")]
    public GameObject fireBallPrefab;
    public GameObject fireBallExplosionPrefab;
    public float fireBallSpeed;
    public float fireBallLifeTime;
    public float fireBallExplosionLifeTime;
    public float fireBallExmplosionDamageRadius;
    public float fireBallExplosionDamage;

    //================================================================

    void Awake()
    {
        Globals.singleton = this;
    }
}
