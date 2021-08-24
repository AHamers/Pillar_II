using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSprintState : EntityState
{
    private Vector3 previousPlayerPosition;
    public override EntityState checkTransition()
    {
        if (Input.GetKeyDown(Globals.singleton.castFireball))
        {
            return new PlayerCastSpellState(new FireballSpell(), this);
        }
        if (!Input.GetKey(Globals.singleton.forward)
        && !Input.GetKey(Globals.singleton.backward)
        && !Input.GetKey(Globals.singleton.left)
        && !Input.GetKey(Globals.singleton.right))
        {
            return new PlayerIdleState();
        }
        else if (Input.GetKeyUp(Globals.singleton.sprint))
        {
            return new PlayerWalkState();
        }
        if (Input.GetKeyDown(Globals.singleton.jump))
        {
            return new PlayerJumpState(previousPlayerPosition);
        }
        return null;
    }

    public override void fixedUpdateState()
    {
        previousPlayerPosition = Globals.singleton.player.transform.position;
        CharacterController cc = Globals.singleton.player.GetComponent<CharacterController>();
        GameObject player = cc.gameObject;

        player.GetComponent<CameraAutomaton>().currentPlayerAngleOffset = 0.0f;

        int numberOfDirectionsPressed = 0;
        if (Input.GetKey(Globals.singleton.forward))
        {
            player.GetComponent<CameraAutomaton>().currentPlayerAngleOffset += 0.0f;
            numberOfDirectionsPressed++;
        }
        if (Input.GetKey(Globals.singleton.backward))
        {
            if (Input.GetKey(Globals.singleton.right))
                player.GetComponent<CameraAutomaton>().currentPlayerAngleOffset -= Mathf.PI;
            else
                player.GetComponent<CameraAutomaton>().currentPlayerAngleOffset += Mathf.PI;
            numberOfDirectionsPressed++;
        }
        if (Input.GetKey(Globals.singleton.left))
        {
            player.GetComponent<CameraAutomaton>().currentPlayerAngleOffset += Mathf.PI / 2.0f;
            numberOfDirectionsPressed++;
        }
        if (Input.GetKey(Globals.singleton.right))
        {
            player.GetComponent<CameraAutomaton>().currentPlayerAngleOffset -= Mathf.PI / 2.0f;
            numberOfDirectionsPressed++;
        }

        if (numberOfDirectionsPressed > 0)
            player.GetComponent<CameraAutomaton>().currentPlayerAngleOffset /= numberOfDirectionsPressed;
        if (numberOfDirectionsPressed > 2)
            player.GetComponent<CameraAutomaton>().currentPlayerAngleOffset = 0;

        cc.SimpleMove(player.transform.forward * Globals.singleton.playerSprintSpeed * Time.deltaTime);
    }

    public override void onEnterState()
    {
        Globals.singleton.player.GetComponent<Animator>().SetInteger("PlayerState", (int)EntityAnimationState.RUNNING);
    }

    public override void onExitState()
    {
    }

    public override void updateState()
    {
        CameraAutomaton cameraAutomaton = Globals.singleton.player.GetComponent<CameraAutomaton>();
        Globals.singleton.player.GetComponent<Animator>().SetFloat("PlayerRollAngle", cameraAutomaton.getAveragePreviousPlayerAngleDifferences());
    }
}
