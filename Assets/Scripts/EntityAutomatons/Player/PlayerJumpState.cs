using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : EntityState
{
    private Vector3 previousPlayerPosition;
    private Vector3 playerVelocity;

    public PlayerJumpState(Vector3 previousPlayerPosition)
    {
        this.previousPlayerPosition = previousPlayerPosition;
    }

    public override EntityState checkTransition()
    {
        if (Input.GetKeyDown(Globals.singleton.castFireball))
        {
            return new PlayerCastSpellState(new FireballSpell(), this);
        }
        if (Globals.singleton.player.GetComponent<CharacterController>().isGrounded == true)
        {
            if(Input.GetKey(Globals.singleton.forward) || Input.GetKey(Globals.singleton.backward) || Input.GetKey(Globals.singleton.left) || Input.GetKey(Globals.singleton.right))
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
            return new PlayerIdleState();
        }
        return null;
    }

    public override void fixedUpdateState()
    {
        Globals.singleton.player.GetComponent<Animator>().SetFloat("PlayerVerticalSpeed", Globals.singleton.player.GetComponent<CharacterController>().velocity.y);

        //airControl
        Vector3 motion = Vector3.zero;
        int directionsPressed = 0;
        if (Input.GetKey(Globals.singleton.forward))
        {
            motion += Globals.singleton.player.transform.forward;
            directionsPressed++;
        }
        if (Input.GetKey(Globals.singleton.backward))
        {
            motion -= Globals.singleton.player.transform.forward;
            directionsPressed++;
        }
        if (Input.GetKey(Globals.singleton.left))
        {
            motion -= Globals.singleton.player.transform.right;
            directionsPressed++;
        }
        if (Input.GetKey(Globals.singleton.right))
        {
            motion += Globals.singleton.player.transform.right;
            directionsPressed++;
        }
        if (directionsPressed == 0)
            motion = Vector3.zero;
        else
        {
            motion /= directionsPressed;
            motion *= Globals.singleton.airControl;
        }

        motion += playerVelocity;
        Globals.singleton.player.GetComponent<CharacterController>().Move(motion * Time.deltaTime);

        playerVelocity += Physics.gravity * Time.deltaTime;
    }

    public override void onEnterState()
    {
        Globals.singleton.player.GetComponent<Animator>().SetInteger("PlayerState", (int)EntityAnimationState.JUMPING);

        playerVelocity = new Vector3(0, 1, 0) * Globals.singleton.jumpForce.y;
        Globals.singleton.player.GetComponent<CharacterController>().Move(playerVelocity * Time.deltaTime);
    }

    public override void onExitState()
    {
        Globals.singleton.player.GetComponent<CharacterController>().SimpleMove(Vector3.zero);
    }

    public override void updateState()
    {
    }
}
