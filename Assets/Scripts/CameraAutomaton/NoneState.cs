using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NoneState : CameraState
{
    public override CameraState checkTransition()
    {
        //left click
        if(Input.GetMouseButtonDown(0))
        {
            return new CameraOnlyState();
        }

        //right click
        if(Input.GetMouseButtonDown(1))
        {
            return new CameraAndPlayerState();
        }

        return null;
    }

    public override void fixedUpdateState()
    {
    }

    public override void onEnterState()
    {
    }

    public override void onExitState()
    {
    }

    public override void updateState()
    {
        CameraAutomaton cameraAutomaton = Globals.singleton.player.GetComponent<CameraAutomaton>();
        cameraAutomaton.addCameraDistanceToPlayer(Input.mouseScrollDelta.y * Time.deltaTime * Globals.singleton.zoomSpeed);
    }
}
