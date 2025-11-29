using UnityEngine;
using MFlight.Demo;
using DemoPlane = MFlight.Demo.Plane;

/// <summary>
/// Ajoute des turbulences atmosphériques et modifie la résistance de l'air
/// en fonction de l'altitude et des conditions météorologiques.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class AtmosphericTurbulence : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au script Plane pour accéder aux propriétés aérodynamiques")]
    public DemoPlane plane;
    
    [Tooltip("Référence optionnelle à WeatherMap pour intensité météo")]
    public WeatherMap weatherMap;
    
    private Rigidbody rb;
    
    [Header("Turbulences")]
    [Tooltip("Intensité de base des turbulences (multiplicateur)")]
    [Range(0f, 10f)]
    public float baseTurbulenceIntensity = 1f;
    
    [Tooltip("Altitude minimale pour turbulences fortes (mètres)")]
    public float minTurbulenceAltitude = 500f;
    
    [Tooltip("Altitude maximale où les turbulences sont au maximum (mètres)")]
    public float maxTurbulenceAltitude = 3000f;
    
    [Tooltip("Force maximale de turbulence appliquée (Newtons)")]
    public float maxTurbulenceForce = 500f;
    
    [Tooltip("Couple de turbulence maximal (Newton-mètres)")]
    public float maxTurbulenceTorque = 200f;
    
    [Tooltip("Vitesse de changement des turbulences (Hz)")]
    [Range(0.1f, 10f)]
    public float turbulenceFrequency = 2f;
    
    [Header("Résistance de l'air selon altitude")]
    [Tooltip("Modifier la densité de l'air selon l'altitude (affecte portance et traînée)")]
    public bool applyAltitudeDensity = true;
    
    [Tooltip("Altitude de référence niveau mer (mètres)")]
    public float seaLevelAltitude = 0f;
    
    [Tooltip("Facteur d'échelle de densité par altitude (formule exponentielle)")]
    public float densityScaleFactor = 0.00012f; // ~1% par 100m
    
    [Header("Effet Météo")]
    [Tooltip("Les nuages augmentent les turbulences")]
    public bool weatherAffectsTurbulence = true;
    
    [Tooltip("Multiplicateur de turbulence dans les nuages")]
    [Range(1f, 5f)]
    public float cloudTurbulenceMultiplier = 2f;
    
    [Header("Debug")]
    [Tooltip("Afficher les valeurs de turbulence dans la console")]
    public bool showDebugInfo = false;
    
    // Variables internes
    private Vector3 turbulenceOffset;
    private Vector3 turbulenceVelocity;
    private float originalAirDensity;
    private float currentAltitude;
    private float weatherIntensity;
    
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        
        if (plane == null)
        {
            plane = GetComponent<DemoPlane>();
            if (plane == null)
            {
                Debug.LogError("AtmosphericTurbulence: Aucune référence Plane trouvée!");
                enabled = false;
                return;
            }
        }
        
        // Sauvegarder la densité de l'air originale
        if (plane.aero != null)
        {
            originalAirDensity = plane.aero.airDensity;
        }
        
        // Trouver WeatherMap si non assigné
        if (weatherMap == null)
        {
            weatherMap = FindObjectOfType<WeatherMap>();
        }
    }
    
    void FixedUpdate()
    {
        if (rb == null || plane == null) return;
        
        // Calculer l'altitude actuelle
        currentAltitude = transform.position.y - seaLevelAltitude;
        
        // Appliquer la densité de l'air selon l'altitude
        if (applyAltitudeDensity && plane.aero != null)
        {
            ApplyAltitudeDensity();
        }
        
        // Calculer et appliquer les turbulences
        if (baseTurbulenceIntensity > 0.01f)
        {
            ApplyTurbulence();
        }
    }
    
    /// <summary>
    /// Modifie la densité de l'air selon l'altitude (formule atmosphérique simplifiée)
    /// </summary>
    void ApplyAltitudeDensity()
    {
        // Formule exponentielle: densité = densité_mer * e^(-altitude * facteur)
        float densityRatio = Mathf.Exp(-currentAltitude * densityScaleFactor);
        
        // Limiter entre 0.1 (très haute altitude) et 1.0 (niveau mer)
        densityRatio = Mathf.Clamp(densityRatio, 0.1f, 1f);
        
        plane.aero.airDensity = originalAirDensity * densityRatio;
        
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Altitude: {currentAltitude:F0}m, Densité: {plane.aero.airDensity:F3} kg/m³ ({densityRatio * 100:F1}%)");
        }
    }
    
    /// <summary>
    /// Applique des forces et couples de turbulence basés sur l'altitude et la météo
    /// </summary>
    void ApplyTurbulence()
    {
        // Calculer le facteur d'intensité selon l'altitude
        float altitudeFactor = 0f;
        if (currentAltitude > minTurbulenceAltitude)
        {
            altitudeFactor = Mathf.InverseLerp(minTurbulenceAltitude, maxTurbulenceAltitude, currentAltitude);
            altitudeFactor = Mathf.Clamp01(altitudeFactor);
        }
        
        // Obtenir l'intensité météo locale si WeatherMap disponible
        weatherIntensity = 0f;
        if (weatherAffectsTurbulence && weatherMap != null && weatherMap.weatherMap != null)
        {
            // Échantillonner la texture météo à la position de l'avion
            Vector3 containerPos = transform.position;
            
            // Normaliser la position dans l'espace 0-1 de la weathermap
            // (approximation simple - ajuster selon la taille réelle du container)
            float u = (containerPos.x % 1000f) / 1000f;
            float v = (containerPos.z % 1000f) / 1000f;
            
            // Note: weatherMap.weatherMap est une RenderTexture, difficile à lire directement
            // On utilise une approximation simple ici
            weatherIntensity = Mathf.PerlinNoise(containerPos.x * 0.001f, containerPos.z * 0.001f);
        }
        
        // Combiner les facteurs
        float totalIntensity = baseTurbulenceIntensity * (0.2f + altitudeFactor * 0.8f);
        
        if (weatherIntensity > 0.5f)
        {
            totalIntensity *= Mathf.Lerp(1f, cloudTurbulenceMultiplier, (weatherIntensity - 0.5f) * 2f);
        }
        
        // Générer un bruit de turbulence lisse (Perlin noise en mouvement)
        float time = Time.time * turbulenceFrequency;
        
        Vector3 turbulenceForce = new Vector3(
            (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(time + 100f, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(time + 200f, 0f) - 0.5f) * 2f
        );
        
        Vector3 turbulenceTorque = new Vector3(
            (Mathf.PerlinNoise(time + 300f, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(time + 400f, 0f) - 0.5f) * 2f,
            (Mathf.PerlinNoise(time + 500f, 0f) - 0.5f) * 2f
        );
        
        // Appliquer les forces avec l'intensité totale
        rb.AddForce(turbulenceForce * maxTurbulenceForce * totalIntensity, ForceMode.Force);
        rb.AddTorque(turbulenceTorque * maxTurbulenceTorque * totalIntensity, ForceMode.Force);
        
        if (showDebugInfo && Time.frameCount % 60 == 0)
        {
            Debug.Log($"Turbulence - Alt:{currentAltitude:F0}m, AltFactor:{altitudeFactor:F2}, Weather:{weatherIntensity:F2}, Total:{totalIntensity:F2}");
        }
    }
    
    void OnDisable()
    {
        // Restaurer la densité de l'air originale
        if (plane != null && plane.aero != null)
        {
            plane.aero.airDensity = originalAirDensity;
        }
    }
}
