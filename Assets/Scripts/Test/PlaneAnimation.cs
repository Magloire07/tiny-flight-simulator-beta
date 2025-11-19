using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlaneAnimation : MonoBehaviour {

    public Transform propeller;
    public float propSpeed = 100;

    public float smoothTime = .5f;
    [Header ("Aileron (Roll)")]
    public Transform aileronLeft;
    public Transform aileronRight;
    public float aileronMax = 20;
    [Tooltip("Max deflection for symmetric aileron (both same direction) from Up/Down arrows")]
    public float aileronSymmetricMax = 20;
    [Header ("Elevator (Pitch)")]
    public Transform elevator;
    public float elevatorMax = 20;
    [Header ("Rudder (Yaw)")]
    public Transform rudder;
    public float rudderMax = 20;

    // Smoothing vars
    float smoothedRoll;
    float smoothRollV;
    float smoothedPitch;
    float smoothPitchV;
    float smoothedYaw;
    float smoothYawV;
    float smoothedAileronSym;
    float smoothAileronSymV;

    [Header("Input Gating")]
    public bool animateOnlyOnManualInput = true;
    [Range(0f, 1f)] public float inputThreshold = 0.05f;
    public float inputTimeout = 0.2f;
    float lastManualInputTime;

    MFlight.Demo.Plane plane;

    void Start () {
        plane = GetComponent<MFlight.Demo.Plane> ();
    }

    void Update () {
        // https://en.wikipedia.org/wiki/Aircraft_principal_axes
        propeller.Rotate (Vector3.forward * propSpeed * Time.deltaTime);

        bool manualActive = true;
        if (animateOnlyOnManualInput) {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            float kbdH = Input.GetAxis("Horizontal");
            float kbdV = Input.GetAxis("Vertical");
            float magnitude = new Vector2(mouseX, mouseY).magnitude + Mathf.Max(Mathf.Abs(kbdH), Mathf.Abs(kbdV));
            if (magnitude > inputThreshold) lastManualInputTime = Time.time;
            manualActive = (Time.time - lastManualInputTime) <= inputTimeout;
        }

        // Ailerons: use commanded roll (exclude stabilization corrections) + symmetric
        float targetRoll = manualActive ? plane.CommandRoll : 0f;
        float targetAileronSym = manualActive ? plane.AileronSymmetric : 0f;
        smoothedRoll = Mathf.SmoothDamp (smoothedRoll, targetRoll, ref smoothRollV, Time.deltaTime * smoothTime);
        smoothedAileronSym = Mathf.SmoothDamp (smoothedAileronSym, targetAileronSym, ref smoothAileronSymV, Time.deltaTime * smoothTime);
        float leftDeflection = (-smoothedRoll * aileronMax) + (smoothedAileronSym * aileronSymmetricMax);
        float rightDeflection = (smoothedRoll * aileronMax) + (smoothedAileronSym * aileronSymmetricMax);
        aileronLeft.localEulerAngles = new Vector3 (leftDeflection, aileronLeft.localEulerAngles.y, aileronLeft.localEulerAngles.z);
        aileronRight.localEulerAngles = new Vector3 (rightDeflection, aileronRight.localEulerAngles.y, aileronRight.localEulerAngles.z);

        // Pitch: pilot commanded only (trim removed)
        float commandedPitch = plane.CommandPitch;
        float targetPitch = manualActive ? commandedPitch : 0f;
        smoothedPitch = Mathf.SmoothDamp (smoothedPitch, targetPitch, ref smoothPitchV, Time.deltaTime * smoothTime);
        elevator.localEulerAngles = new Vector3 (-smoothedPitch * elevatorMax, elevator.localEulerAngles.y, elevator.localEulerAngles.z);

        // Yaw: commanded (exclude stabilization corrections); no trim yet
        float targetYaw = manualActive ? plane.CommandYaw : 0f;
        smoothedYaw = Mathf.SmoothDamp (smoothedYaw, targetYaw, ref smoothYawV, Time.deltaTime * smoothTime);
        rudder.localEulerAngles = new Vector3 (rudder.localEulerAngles.x, -smoothedYaw * rudderMax, rudder.localEulerAngles.z);
    }
}