using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireballSpell : Spell
{
    public override void cast(EntityAutomaton caller)
    {
        GameObject fireball = GameObject.Instantiate(Globals.singleton.fireBallPrefab);
        fireball.transform.position = caller.transform.position + new Vector3(0, caller.GetComponent<CameraAutomaton>().playerAimPointerOffset, 0);
        fireball.GetComponent<Fireball>().velocity = caller.GetComponent<CameraAutomaton>().aimVector.normalized * Globals.singleton.fireBallSpeed;
    }
}
