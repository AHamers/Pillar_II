using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraOnlyState : CameraState
{
    private Vector2 lastMousePosition;

    public override CameraState checkTransition()
    {
        if(Input.GetMouseButtonDown(1))
        {
            return new CameraAndPlayerState();
        }
        if(Input.GetMouseButtonUp(0))
        {
            return new NoneState();
        }
        return null;
    }

    public override void fixedUpdateState()
    {
        CameraAutomaton cameraAutomaton = Globals.singleton.player.GetComponent<CameraAutomaton>();
        cameraAutomaton.setCameraAngle(cameraAutomaton.currentCameraAngle +(new Vector2(Input.mousePosition.x, Input.mousePosition.y) - lastMousePosition) * Globals.singleton.cameraSensivity * Time.deltaTime);

        lastMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
    }

    public override void onEnterState()
    {
        lastMousePosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
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
