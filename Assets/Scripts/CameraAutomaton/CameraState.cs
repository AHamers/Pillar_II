using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class CameraState
{
    public abstract CameraState checkTransition();
    public abstract void updateState();
    public abstract void fixedUpdateState();
    public abstract void onEnterState();
    public abstract void onExitState();
}
