using UnityEngine;

/// <summary>
/// Applique les effets du vent dynamique sur l'avion
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WindEffect : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au système de météo dynamique")]
    public DynamicWeatherSystem weatherSystem;
    
    [Tooltip("Rigidbody de l'avion")]
    public Rigidbody rb;
    
    [Header("Paramètres")]
    [Tooltip("Multiplicateur de force du vent")]
    [Range(0f, 5f)]
    public float windForceMultiplier = 1f;
    
    [Tooltip("Le vent affecte aussi la rotation")]
    public bool affectRotation = true;
    
    [Tooltip("Multiplicateur du couple de vent")]
    [Range(0f, 2f)]
    public float windTorqueMultiplier = 0.5f;
    
    void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();
        
        if (weatherSystem == null)
            weatherSystem = FindObjectOfType<DynamicWeatherSystem>();
    }
    
    void FixedUpdate()
    {
        if (weatherSystem == null || rb == null) return;
        
        // Obtenir la force du vent
        Vector3 windForce = weatherSystem.GetWindForce();
        
        if (windForce.magnitude > 0.1f)
        {
            // Appliquer la force du vent
            rb.AddForce(windForce * windForceMultiplier, ForceMode.Force);
            
            // Appliquer un couple pour simuler l'effet du vent sur les surfaces
            if (affectRotation)
            {
                Vector3 torque = Vector3.Cross(transform.forward, windForce.normalized) * windForce.magnitude;
                rb.AddTorque(torque * windTorqueMultiplier, ForceMode.Force);
            }
        }
    }
}
