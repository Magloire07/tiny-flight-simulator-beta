using UnityEngine;

/// <summary>
/// Rendu de nuages volumétriques avec Ray Marching
/// Intégré au DynamicWeatherSystem
/// </summary>
[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
public class VolumetricCloudsRenderer : MonoBehaviour
{
    [Header("Shader")]
    [Tooltip("Shader de nuages volumétriques")]
    public Shader volumetricCloudsShader;
    
    [Header("Paramètres des Nuages")]
    [Tooltip("Densité des nuages (0-2)")]
    [Range(0f, 2f)]
    public float cloudDensity = 0.5f;
    
    [Tooltip("Échelle des nuages (plus petit = nuages plus petits)")]
    [Range(0.1f, 10f)]
    public float cloudScale = 1.0f;
    
    [Tooltip("Vitesse de déplacement des nuages")]
    [Range(0f, 5f)]
    public float cloudSpeed = 0.5f;
    
    [Tooltip("Hauteur de la couche de nuages (mètres)")]
    [Range(100f, 5000f)]
    public float cloudHeight = 1000f;
    
    [Tooltip("Épaisseur de la couche de nuages (mètres)")]
    [Range(100f, 2000f)]
    public float cloudThickness = 500f;
    
    [Header("Qualité")]
    [Tooltip("Nombre de pas du ray marching (plus = meilleure qualité, moins de performance)")]
    [Range(10, 100)]
    public int raySteps = 50;
    
    [Tooltip("Absorption de la lumière par les nuages")]
    [Range(0f, 1f)]
    public float lightAbsorption = 0.3f;
    
    [Header("Couleurs")]
    [Tooltip("Couleur du soleil")]
    public Color sunColor = new Color(1f, 0.95f, 0.8f);
    
    [Tooltip("Couleur de base des nuages")]
    public Color cloudColor = new Color(0.9f, 0.9f, 0.95f);
    
    [Header("Intégration Météo")]
    [Tooltip("Référence au système météo dynamique")]
    public DynamicWeatherSystem weatherSystem;
    
    [Tooltip("Multiplier la densité par l'intensité météo")]
    public bool useWeatherIntensity = true;
    
    [Tooltip("Densité maximale en cas d'orage")]
    [Range(0.5f, 3f)]
    public float stormDensityMultiplier = 2.0f;
    
    private Material cloudMaterial;
    private Camera cam;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (volumetricCloudsShader == null)
        {
            Debug.LogError("VolumetricCloudsRenderer: Shader non assigné!");
            enabled = false;
            return;
        }
        
        cloudMaterial = new Material(volumetricCloudsShader);
        
        // Trouver le système météo si non assigné
        if (weatherSystem == null)
        {
            weatherSystem = FindObjectOfType<DynamicWeatherSystem>();
        }
    }
    
    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (cloudMaterial == null)
        {
            Graphics.Blit(src, dest);
            return;
        }
        
        // Calculer la densité en fonction de la météo
        float finalDensity = cloudDensity;
        if (useWeatherIntensity && weatherSystem != null)
        {
            float weatherIntensity = weatherSystem.weatherIntensity;
            finalDensity = cloudDensity * Mathf.Lerp(0.3f, stormDensityMultiplier, weatherIntensity);
        }
        
        // Mettre à jour les propriétés du shader
        cloudMaterial.SetFloat("_CloudDensity", finalDensity);
        cloudMaterial.SetFloat("_CloudScale", cloudScale);
        cloudMaterial.SetFloat("_CloudSpeed", cloudSpeed);
        cloudMaterial.SetFloat("_CloudHeight", cloudHeight);
        cloudMaterial.SetFloat("_CloudThickness", cloudThickness);
        cloudMaterial.SetFloat("_RaySteps", raySteps);
        cloudMaterial.SetFloat("_LightAbsorption", lightAbsorption);
        cloudMaterial.SetColor("_SunColor", sunColor);
        cloudMaterial.SetColor("_CloudColor", cloudColor);
        
        // Passer la position de la caméra et la direction du soleil
        cloudMaterial.SetVector("_CameraPos", cam.transform.position);
        
        // Direction du soleil (utiliser la lumière du système météo si disponible)
        Vector3 sunDir = Vector3.down;
        if (weatherSystem != null && weatherSystem.sunLight != null)
        {
            sunDir = -weatherSystem.sunLight.transform.forward;
        }
        cloudMaterial.SetVector("_SunDirection", sunDir);
        
        // Appliquer l'effet
        Graphics.Blit(src, dest, cloudMaterial);
    }
    
    void OnDestroy()
    {
        if (cloudMaterial != null)
        {
            DestroyImmediate(cloudMaterial);
        }
    }
    
    /// <summary>
    /// Active ou désactive le rendu des nuages
    /// </summary>
    public void SetEnabled(bool enable)
    {
        enabled = enable;
    }
    
    /// <summary>
    /// Définit la densité des nuages directement
    /// </summary>
    public void SetCloudDensity(float density)
    {
        cloudDensity = Mathf.Clamp(density, 0f, 2f);
    }
}
