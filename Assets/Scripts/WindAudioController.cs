using UnityEngine;
using DemoPlane = MFlight.Demo.Plane;

/// <summary>
/// Contrôle le son du vent en fonction de la vitesse de l'avion
/// </summary>
public class WindAudioController : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence au système de météo dynamique")]
    public DynamicWeatherSystem weatherSystem;
    
    [Tooltip("Référence à l'avion pour obtenir la vitesse")]
    public DemoPlane plane;
    
    [Tooltip("Rigidbody de l'avion")]
    public Rigidbody rb;
    
    [Tooltip("AudioSource pour le son du vent (sera créé automatiquement si non assigné)")]
    public AudioSource windAudioSource;
    
    [Header("Sons")]
    [Tooltip("Clip audio du vent (doit être en loop)")]
    public AudioClip windSound;
    
    [Tooltip("Son de vent léger (optionnel)")]
    public AudioClip lightWindSound;
    
    [Tooltip("Son de vent fort (optionnel)")]
    public AudioClip strongWindSound;
    
    [Header("Paramètres Volume")]
    [Tooltip("Volume minimum du vent (au sol, moteur éteint)")]
    [Range(0f, 1f)]
    public float minVolume = 0.01f;
    
    [Tooltip("Volume maximum du vent (en vol rapide)")]
    [Range(0f, 1f)]
    public float maxVolume = 0.4f;
    
    [Tooltip("Vitesse de l'avion pour volume maximum (en m/s)")]
    public float maxAirspeed = 80f;
    
    [Tooltip("Vitesse minimale pour passer au windSound (décollage)")]
    public float takeoffSpeed = 15f;
    
    [Header("Paramètres Pitch")]
    [Tooltip("Pitch minimum (vent faible)")]
    [Range(0.5f, 1.5f)]
    public float minPitch = 0.7f;
    
    [Tooltip("Pitch maximum (vent fort)")]
    [Range(0.5f, 2f)]
    public float maxPitch = 1.3f;
    
    [Header("Transition")]
    [Tooltip("Vitesse de transition du volume (lissage)")]
    [Range(0.1f, 5f)]
    public float volumeTransitionSpeed = 2f;
    
    [Tooltip("Vitesse de transition du pitch (lissage)")]
    [Range(0.1f, 5f)]
    public float pitchTransitionSpeed = 2f;
    
    [Header("Debug")]
    [Tooltip("Afficher les informations de debug")]
    public bool showDebug = false;
    
    // Variables internes
    private float currentTargetVolume;
    private float currentTargetPitch;
    private float currentWindSpeed;
    
    void Start()
    {
        // Créer un AudioSource dédié si non assigné
        if (windAudioSource == null)
        {
            GameObject windAudioObj = new GameObject("WindAudioSource");
            windAudioObj.transform.SetParent(transform);
            windAudioObj.transform.localPosition = Vector3.zero;
            windAudioSource = windAudioObj.AddComponent<AudioSource>();
            Debug.Log("WindAudioController: AudioSource dédié créé pour le son du vent");
        }
        
        // Configurer l'AudioSource
        windAudioSource.loop = true;
        windAudioSource.playOnAwake = true;
        windAudioSource.spatialBlend = 0f; // 2D sound (pas de spatialisation)
        windAudioSource.volume = minVolume;
        windAudioSource.pitch = minPitch;
        windAudioSource.priority = 200; // Priorité basse (256 = plus basse, 0 = plus haute) pour ne pas couvrir le moteur
        
        // Trouver les références si non assignées
        if (plane == null)
        {
            plane = GetComponent<DemoPlane>();
            if (plane == null)
            {
                Debug.LogWarning("WindAudioController: Plane non trouvé!");
            }
        }
        
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning("WindAudioController: Rigidbody non trouvé!");
            }
        }
        
        if (weatherSystem == null)
        {
            weatherSystem = FindObjectOfType<DynamicWeatherSystem>();
        }
        
        // Assigner le clip par défaut (lightWindSound au sol)
        if (lightWindSound != null && windAudioSource.clip == null)
        {
            windAudioSource.clip = lightWindSound;
        }
        else if (windSound != null && windAudioSource.clip == null)
        {
            windAudioSource.clip = windSound;
        }
        
        // Démarrer la lecture
        if (windAudioSource.clip != null && !windAudioSource.isPlaying)
        {
            windAudioSource.Play();
        }
    }
    
    void Update()
    {
        if (rb == null) return;
        
        // Obtenir la vitesse de l'avion (relative à l'air)
        float airspeed = rb.velocity.magnitude;
        currentWindSpeed = airspeed;
        
        // Obtenir le throttle si disponible
        float throttle = 0f;
        if (plane != null)
        {
            throttle = plane.throttle;
        }
        
        // Calculer le ratio basé sur la vitesse ET le throttle
        float speedRatio = Mathf.Clamp01(airspeed / maxAirspeed);
        float combinedRatio = Mathf.Clamp01((speedRatio * 0.7f) + (throttle * 0.3f)); // 70% vitesse, 30% throttle
        
        currentTargetVolume = Mathf.Lerp(minVolume, maxVolume, combinedRatio);
        currentTargetPitch = Mathf.Lerp(minPitch, maxPitch, combinedRatio);
        
        // Transition lisse vers les valeurs cibles
        windAudioSource.volume = Mathf.Lerp(
            windAudioSource.volume, 
            currentTargetVolume, 
            Time.deltaTime * volumeTransitionSpeed
        );
        
        windAudioSource.pitch = Mathf.Lerp(
            windAudioSource.pitch, 
            currentTargetPitch, 
            Time.deltaTime * pitchTransitionSpeed
        );
        
        // Changer le clip si différents sons sont disponibles
        UpdateWindSoundClip(airspeed);
        
        // Debug
        if (showDebug)
        {
            Debug.Log($"WindAudio - Airspeed: {airspeed:F1} m/s | Throttle: {throttle:F2} | Volume: {windAudioSource.volume:F2} | Pitch: {windAudioSource.pitch:F2}");
        }
    }
    
    /// <summary>
    /// Change le clip audio selon la vitesse de l'avion
    /// </summary>
    void UpdateWindSoundClip(float airspeed)
    {
        AudioClip targetClip = windSound;
        
        // Au sol ou vitesse très faible = lightWindSound
        if (lightWindSound != null && airspeed < takeoffSpeed)
        {
            targetClip = lightWindSound;
        }
        // Décollage/vol = windSound
        else if (windSound != null && airspeed >= takeoffSpeed && airspeed < maxAirspeed * 0.7f)
        {
            targetClip = windSound;
        }
        // Vol rapide = strongWindSound
        else if (strongWindSound != null && airspeed >= maxAirspeed * 0.7f)
        {
            targetClip = strongWindSound;
        }
        // Fallback si pas de lightWindSound
        else if (windSound != null)
        {
            targetClip = windSound;
        }
        
        // Changer le clip si différent (avec fondu)
        if (targetClip != null && windAudioSource.clip != targetClip)
        {
            windAudioSource.clip = targetClip;
            windAudioSource.Play();
        }
    }
    
    /// <summary>
    /// Obtient la vitesse actuelle du vent
    /// </summary>
    public float GetCurrentWindSpeed()
    {
        return currentWindSpeed;
    }
    
    /// <summary>
    /// Définit le volume manuellement
    /// </summary>
    public void SetVolume(float volume)
    {
        currentTargetVolume = Mathf.Clamp01(volume);
    }
    
    /// <summary>
    /// Active/désactive le son du vent
    /// </summary>
    public void SetWindAudioEnabled(bool enabled)
    {
        if (enabled && !windAudioSource.isPlaying)
        {
            windAudioSource.Play();
        }
        else if (!enabled && windAudioSource.isPlaying)
        {
            windAudioSource.Stop();
        }
    }
}
