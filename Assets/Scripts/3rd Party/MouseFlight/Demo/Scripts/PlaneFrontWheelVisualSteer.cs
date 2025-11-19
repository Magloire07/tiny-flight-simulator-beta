using UnityEngine;

public class PlaneFrontWheelVisualSteer : MonoBehaviour
{
    [Header("Wheel Collider")]
    public WheelCollider wheelCollider;

    [Header("Wheel Mesh (Transform)")]
    public Transform wheelMesh;

    [Header("Visual Steering Settings")]
    [Tooltip("Multiplier for visual steering (usually 1, but can be -1 if inverted)")]
    public float steerMultiplier = 1f;

    void Update()
    {
        if (wheelCollider != null && wheelMesh != null)
        {
            // Get current steer angle from WheelCollider
            float steerAngle = wheelCollider.steerAngle * steerMultiplier;
            // Apply steer angle to wheel mesh (local Y axis)
            Vector3 localEuler = wheelMesh.localEulerAngles;
            localEuler.y = steerAngle;
            wheelMesh.localEulerAngles = localEuler;
        }
    }
}
