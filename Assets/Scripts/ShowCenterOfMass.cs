using UnityEngine;

[ExecuteAlways]
public class ShowCenterOfMass : MonoBehaviour
{
    [Header("Target Rigidbody (optional)")] public Rigidbody rb;

    [Header("Sphere Settings")] public float sphereSize = 0.12f;
    public Color sphereColor = Color.yellow;

    [Header("Axis Settings")] public bool drawAxes = true;
    public float axisLength = 0.5f;
    public float axisThickness = 1f; // Gizmos line thickness not directly supported; kept for future Handles use.
    public Color xColor = Color.red;
    public Color yColor = Color.green;
    public Color zColor = Color.blue;

    [Header("World Space Offset")] public Vector3 centerOffset; // optional offset added to rb.worldCenterOfMass for fine tuning.

    private void Reset()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void OnValidate()
    {
        if (axisLength < 0f) axisLength = 0f;
        if (sphereSize < 0f) sphereSize = 0f;
    }

    private void OnDrawGizmos()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (rb == null) return;

        Vector3 p = rb.worldCenterOfMass + centerOffset;

        if (drawAxes)
        {
            var tf = rb.transform;
            Gizmos.color = xColor;
            Gizmos.DrawLine(p, p + tf.right * axisLength);
            Gizmos.color = yColor;
            Gizmos.DrawLine(p, p + tf.up * axisLength);
            Gizmos.color = zColor;
            Gizmos.DrawLine(p, p + tf.forward * axisLength);
        }

        Gizmos.color = sphereColor;
        Gizmos.DrawSphere(p, sphereSize);
    }
}
