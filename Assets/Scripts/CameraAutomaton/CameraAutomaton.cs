using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraAutomaton : MonoBehaviour
{
    private CameraState currentCameraState;
    private List<float> previousPlayerAngles;

    [HideInInspector] public Vector2 currentCameraAngle;
    [HideInInspector] public float currentPlayerAngle;
    [HideInInspector] public float cameraDistanceToPlayer;
    [HideInInspector] public float currentPlayerAngleOffset;
    [HideInInspector] public Vector3 aimVector;

    public int numberOfStoredPlayerAnglesForSmoothedAnimationRoll;
    public float minCameraDistanceToPlayer;
    public GameObject cameraPositionGoal;
    public GameObject cameraFocusPointer;
    public Vector3 cameraFocusOffsetToPlayer;
    public GameObject playerForwardPointer;
    public Vector2 playerForwardPointerOffset;
    public GameObject playerAimPointer;
    public float playerAimPointerOffset;
    public GameObject playerCamera;

    // Start is called before the first frame update
    void Start()
    {
        cameraDistanceToPlayer = 10.0f;
        previousPlayerAngles = new List<float>();
        setCameraAngle(new Vector2(3*Mathf.PI/4, Mathf.PI / 4));
        currentPlayerAngle = 0.0f;
        currentPlayerAngleOffset = 0.0f;

        currentCameraState = new NoneState();
        currentCameraState.onEnterState();
    }

    // Update is called once per frame
    void Update()
    {
        CameraState newState = currentCameraState.checkTransition();
        if (newState != null)
        {
            currentCameraState.onExitState();
            currentCameraState = newState;
            currentCameraState.onEnterState();
        }

        currentCameraState.updateState();
    }

    private void FixedUpdate()
    {
        updatePreviousPlayerAngles();
        currentCameraState.fixedUpdateState();

        updatePlayerForwardPointer();
        updateCameraPositionGoal();
        updateCameraFocusPointer();
        updateCameraPositionAndRotation();
        updatePlayerRotation();
        updateAim();
    }

    public float getAveragePreviousPlayerAngleDifferences()
    {
        if (previousPlayerAngles.Count == 0)
            return 0;

        float sum = 0;
        for (int i = 0; i < previousPlayerAngles.Count - 1; i++)
            sum += previousPlayerAngles[i+1] - previousPlayerAngles[i];
        sum += currentPlayerAngle - previousPlayerAngles[previousPlayerAngles.Count - 1];

        return sum / previousPlayerAngles.Count;
    }

    private void updatePreviousPlayerAngles()
    {
        if(previousPlayerAngles.Count < numberOfStoredPlayerAnglesForSmoothedAnimationRoll)
        {
            previousPlayerAngles.Add(currentPlayerAngle);
        }
        else
        {
            previousPlayerAngles.Add(currentPlayerAngle);
            previousPlayerAngles.RemoveAt(0);
        }
    }

    public void setCameraAngle(Vector2 cameraAngle)
    {
            float xComponent = cameraAngle.x;
            float yComponent = cameraAngle.y;
            if (cameraAngle.y < 0)
                yComponent = 0;
            if (cameraAngle.y > Mathf.PI)
                yComponent = Mathf.PI;
            currentCameraAngle = new Vector2(xComponent, yComponent);
    }

    void updateAim()
    {
        aimVector = ((this.transform.position + new Vector3(0, playerAimPointerOffset, 0)) - playerCamera.transform.position).normalized;

        RaycastHit hit;
        if (Physics.Raycast(this.transform.position + new Vector3(0, playerAimPointerOffset, 0), aimVector, out hit, 50))
        {
            playerAimPointer.SetActive(true);
            playerAimPointer.transform.position = hit.point;
        }
        else
        {
            playerAimPointer.SetActive(false);
        }
    }

    void updateCameraPositionGoal()
    {
        float distanceToCenter = cameraDistanceToPlayer;
        cameraPositionGoal.transform.position = this.transform.position
            + new Vector3(distanceToCenter * Mathf.Cos(-1*currentCameraAngle.x) * Mathf.Sin(currentCameraAngle.y),
                          distanceToCenter * Mathf.Cos(currentCameraAngle.y),
                          distanceToCenter * Mathf.Sin(-1*currentCameraAngle.x) * Mathf.Sin(currentCameraAngle.y)
                          );
    }

    public void addCameraDistanceToPlayer(float value)
    {
        cameraDistanceToPlayer -= value;
        if(cameraDistanceToPlayer < minCameraDistanceToPlayer)
        {
            cameraDistanceToPlayer = minCameraDistanceToPlayer;
        }
    }

    void updatePlayerForwardPointer()
    {
        playerForwardPointer.transform.position = this.transform.position
             + new Vector3(playerForwardPointerOffset.x * Mathf.Cos(-1*currentPlayerAngle + currentPlayerAngleOffset),
                           playerForwardPointerOffset.y,
                           playerForwardPointerOffset.x * Mathf.Sin(-1*currentPlayerAngle + currentPlayerAngleOffset)
                           );
    }

    void updateCameraFocusPointer()
    {
        cameraFocusPointer.transform.position = this.transform.position + cameraFocusOffsetToPlayer;
    }

    void updateCameraPositionAndRotation()
    {
        playerCamera.transform.position += (cameraPositionGoal.transform.position - playerCamera.transform.position) * Globals.singleton.cameraGoalReachingSpeed;
        playerCamera.transform.LookAt(cameraFocusPointer.transform);
    }

    void updatePlayerRotation()
    {
        this.transform.LookAt(playerForwardPointer.transform);
    }
}
