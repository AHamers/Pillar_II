using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAndPlayerState : CameraState
{
    private Vector2 lastMousePosition;

    public override CameraState checkTransition()
    {
        if(Input.GetMouseButtonUp(1))
        {
            if (Input.GetMouseButton(0))
            {
                return new CameraOnlyState();
            }
            else
                return new NoneState();
        }
        return null;
    }

    public override void fixedUpdateState()
    {
        CameraAutomaton cameraAutomaton = Globals.singleton.player.GetComponent<CameraAutomaton>();
        cameraAutomaton.setCameraAngle(cameraAutomaton.currentCameraAngle + (new Vector2(Input.mousePosition.x, Input.mousePosition.y) - lastMousePosition) * Globals.singleton.cameraSensivity * Time.deltaTime);
        cameraAutomaton.currentPlayerAngle = cameraAutomaton.currentCameraAngle.x;
        
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
