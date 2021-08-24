using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : EntityState
{
    public override EntityState checkTransition()
    {
        if(Input.GetKeyDown(Globals.singleton.castFireball))
        {
            return new PlayerCastSpellState(new FireballSpell(), this);
        }
        if(Input.GetKeyDown(Globals.singleton.forward)
        || Input.GetKeyDown(Globals.singleton.backward)
        || Input.GetKeyDown(Globals.singleton.left)
        || Input.GetKeyDown(Globals.singleton.right))
        {
            if(Input.GetKey(Globals.singleton.sprint))
            {
                return new PlayerSprintState();
            }
            else
            {
                return new PlayerWalkState();
            }
        }
        if(Input.GetKeyDown(Globals.singleton.jump))
        {
            return new PlayerJumpState(Globals.singleton.player.transform.position);
        }

        return null;
    }

    public override void fixedUpdateState()
    {
    }

    public override void onEnterState()
    {
        Globals.singleton.player.GetComponent<Animator>().SetInteger("PlayerState", (int)EntityAnimationState.IDLE);
    }

    public override void onExitState()
    {
    }

    public override void updateState()
    {
    }
}
