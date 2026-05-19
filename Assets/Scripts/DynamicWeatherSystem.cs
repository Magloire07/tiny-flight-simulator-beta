using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Système de météo dynamique avec pluie, orage, brouillard et vent
/// </summary>
public class DynamicWeatherSystem : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Caméra principale pour suivre les effets")]
    public Camera mainCamera;
    
    [Tooltip("Lumière directionnelle (soleil)")]
    public Light sunLight;
    
    [Tooltip("CloudMaster pour ajuster les nuages")]
    public CloudMaster cloudMaster;
    
    [Tooltip("Rendu volumétrique des nuages (Ray Marching)")]
    public VolumetricCloudsRenderer volumetricClouds;
    
    [Tooltip("Texte UI pour afficher la météo actuelle")]
    public Text weatherDisplayText;
    
    [Header("Intensité Météo")]
    [Tooltip("Intensité globale de la météo (0=beau, 1=tempête)")]
    [Range(0f, 1f)]
    public float weatherIntensity = 0f;
    
    [Header("Systèmes de Particules")]
    [Tooltip("Système de particules pour la pluie")]
    public ParticleSystem rainParticles;
    
    [Tooltip("Système de particules pour l'orage (éclairs simulés)")]
    public ParticleSystem stormParticles;
    
    [Header("Brouillard")]
    [Tooltip("Activer le brouillard")]
    public bool useFog = false;
    
    [Tooltip("Distance de brouillard minimale (beau temps)")]
    public float minFogDistance = 2000f;
    
    [Tooltip("Distance de brouillard maximale (tempête)")]
    public float maxFogDistance = 200f;
    
    [Tooltip("Couleur du brouillard")]
    public Color fogColor = new Color(0.7f, 0.75f, 0.8f); // Gris-bleu atmosphérique
    
    [Header("Vent")]
    [Tooltip("Force du vent maximum (m/s)")]
    public float maxWindForce = 20f;
    
    [Tooltip("Direction du vent (degrés, 0=Nord)")]
    [Range(0f, 360f)]
    public float windDirection = 0f;
    
    [Tooltip("Variation aléatoire du vent")]
    public float windVariation = 5f;
    
    [Tooltip("Fréquence de changement du vent (Hz)")]
    public float windChangeFrequency = 0.5f;
    
    [Header("Effets Audio")]
    [Tooltip("Source audio pour le son de pluie")]
    public AudioSource rainAudioSource;
    
    [Tooltip("Clip audio de pluie légère")]
    public AudioClip lightRainSound;
    
    [Tooltip("Clip audio de pluie forte")]
    public AudioClip heavyRainSound;
    
    [Tooltip("Source audio pour le tonnerre")]
    public AudioSource thunderAudioSource;
    
    [Tooltip("Clips audio de tonnerre")]
    public AudioClip[] thunderSounds;
    
    [Tooltip("Intervalle minimum entre les tonnerres (secondes)")]
    public float minThunderInterval = 3f;
    
    [Tooltip("Intervalle maximum entre les tonnerres (secondes)")]
    public float maxThunderInterval = 10f;
    
    [Header("Éclairs")]
    [Tooltip("Activer les éclairs visuels")]
    public bool enableLightning = true;
    
    [Tooltip("Intensité maximale de l'éclair")]
    public float lightningIntensity = 3f;
    
    [Tooltip("Durée de l'éclair (secondes)")]
    public float lightningDuration = 0.1f;
    
    // Variables internes
    private float currentWindForce = 0f;
    private Vector3 currentWindDirection = Vector3.zero;
    private float nextThunderTime = 0f;
    private bool isLightningActive = false;
    private float lightningTimer = 0f;
    private float originalSunIntensity = 1f;
    private ParticleSystem.EmissionModule rainEmission;
    private ParticleSystem.EmissionModule stormEmission;
    private float nextWeatherUpdate = 0f;
    private const float weatherUpdateInterval = 0.1f; // 10 fps suffisant pour la météo
    
    void Start()
    {
        // Trouver les références automatiquement
        if (mainCamera == null)
            mainCamera = Camera.main;
        
        if (sunLight == null)
        {
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    sunLight = light;
                    originalSunIntensity = sunLight.intensity;
                    break;
                }
            }
        }
        
        if (cloudMaster == null)
            cloudMaster = FindObjectOfType<CloudMaster>();
        
        // Créer les systèmes de particules si non assignés
        if (rainParticles == null)
            CreateRainParticles();
        
        if (stormParticles == null)
            CreateStormParticles();
        
        // Configurer l'audio
        if (rainAudioSource == null)
        {
            rainAudioSource = gameObject.AddComponent<AudioSource>();
            rainAudioSource.loop = true;
            rainAudioSource.playOnAwake = false;
            rainAudioSource.spatialBlend = 0f; // 2D sound
        }
        
        if (thunderAudioSource == null)
        {
            thunderAudioSource = gameObject.AddComponent<AudioSource>();
            thunderAudioSource.loop = false;
            thunderAudioSource.playOnAwake = false;
            thunderAudioSource.spatialBlend = 0f; // 2D sound
        }
        
        // Obtenir les modules d'émission
        if (rainParticles != null)
            rainEmission = rainParticles.emission;
        if (stormParticles != null)
            stormEmission = stormParticles.emission;
        
        // Configurer le brouillard
        RenderSettings.fog = useFog;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogMode = FogMode.Linear; // Mode linéaire pour distance start/end
        
        // Activer le MissionManager UNIQUEMENT si le jeu est lancé depuis le MainMenu
        // Utiliser une coroutine avec délai pour éviter les erreurs d'initialisation
        StartCoroutine(ActivateMissionManagerDelayed());
        
        // Calculer le prochain tonnerre
        ScheduleNextThunder();
        
        // Appliquer l'état initial
        UpdateWeather();
    }
    
    void Update()
    {
        // Météo et vent throttlés à 10 fps — inutile d'appeler chaque frame
        if (Time.time >= nextWeatherUpdate)
        {
            nextWeatherUpdate = Time.time + weatherUpdateInterval;
            UpdateWeather();
            UpdateWind();
        }
        UpdateThunder();
        UpdateLightning();
        UpdateWeatherDisplay();
    }
    
    /// <summary>
    /// Met à jour tous les effets météo selon l'intensité
    /// </summary>
    void UpdateWeather()
    {
        // Pluie
        UpdateRain();
        
        // Brouillard
        if (useFog)
        {
            float fogDistance = Mathf.Lerp(minFogDistance, maxFogDistance, weatherIntensity);
            RenderSettings.fogStartDistance = fogDistance * 0.3f; // Commence plus tôt
            RenderSettings.fogEndDistance = fogDistance;
            
            // Ajuster la couleur du brouillard selon l'intensité (plus sombre en tempête)
            Color currentFogColor = Color.Lerp(new Color(0.8f, 0.85f, 0.9f), new Color(0.4f, 0.4f, 0.45f), weatherIntensity);
            RenderSettings.fogColor = currentFogColor;
        }
        
        // Assombrir le soleil par temps mauvais
        if (sunLight != null)
        {
            float targetIntensity = Mathf.Lerp(originalSunIntensity, originalSunIntensity * 0.2f, weatherIntensity);
            sunLight.intensity = Mathf.Lerp(sunLight.intensity, targetIntensity, Time.deltaTime * 2f);
        }
        
        // Particules d'orage (uniquement si intensité > 0.7)
        if (stormParticles != null && weatherIntensity > 0.7f)
        {
            if (!stormParticles.isPlaying)
                stormParticles.Play();
            
            stormEmission.rateOverTime = Mathf.Lerp(0f, 20f, (weatherIntensity - 0.7f) / 0.3f);
        }
        else if (stormParticles != null && stormParticles.isPlaying)
        {
            stormParticles.Stop();
        }
    }
    
    /// <summary>
    /// Met à jour la pluie
    /// </summary>
    void UpdateRain()
    {
        if (rainParticles == null) return;
        
        // Activer/désactiver la pluie selon l'intensité
        if (weatherIntensity > 0.3f)
        {
            if (!rainParticles.isPlaying)
                rainParticles.Play();
            
            // Ajuster le taux d'émission (300-3000 particules/sec)
            float emissionRate = Mathf.Lerp(300f, 3000f, (weatherIntensity - 0.3f) / 0.7f);
            rainEmission.rateOverTime = emissionRate;
            
            // Audio de pluie
            if (rainAudioSource != null)
            {
                if (!rainAudioSource.isPlaying)
                {
                    rainAudioSource.clip = weatherIntensity > 0.7f ? heavyRainSound : lightRainSound;
                    if (rainAudioSource.clip != null)
                        rainAudioSource.Play();
                }
                
                rainAudioSource.volume = Mathf.Lerp(0.3f, 1f, (weatherIntensity - 0.3f) / 0.7f);
                
                // Changer le clip si l'intensité change
                AudioClip targetClip = weatherIntensity > 0.7f ? heavyRainSound : lightRainSound;
                if (rainAudioSource.clip != targetClip && targetClip != null)
                {
                    rainAudioSource.clip = targetClip;
                    rainAudioSource.Play();
                }
            }
        }
        else
        {
            if (rainParticles.isPlaying)
                rainParticles.Stop();
            
            if (rainAudioSource != null && rainAudioSource.isPlaying)
                rainAudioSource.Stop();
        }
        
        // Suivre la caméra (la retrouver à chaque frame au cas où elle change)
        if (mainCamera == null || !mainCamera.enabled)
            mainCamera = Camera.main;
        
        if (mainCamera != null && rainParticles != null)
        {
            rainParticles.transform.position = mainCamera.transform.position + Vector3.up * 50f;
        }
    }
    
    /// <summary>
    /// Met à jour le vent
    /// </summary>
    void UpdateWind()
    {
        // Force du vent proportionnelle à l'intensité météo
        currentWindForce = weatherIntensity * maxWindForce;
        
        // Direction du vent avec variation aléatoire
        float windAngle = windDirection + Mathf.PerlinNoise(Time.time * windChangeFrequency, 0f) * windVariation * 2f - windVariation;
        currentWindDirection = Quaternion.Euler(0f, windAngle, 0f) * Vector3.forward;
        currentWindDirection = currentWindDirection.normalized * currentWindForce;
    }
    
    /// <summary>
    /// Met à jour le système de tonnerre
    /// </summary>
    void UpdateThunder()
    {
        // Tonnerre seulement si orage (intensité > 0.7)
        if (weatherIntensity < 0.7f) return;
        
        if (Time.time >= nextThunderTime && thunderAudioSource != null && thunderSounds != null && thunderSounds.Length > 0)
        {
            // Jouer un son de tonnerre aléatoire
            AudioClip thunder = thunderSounds[Random.Range(0, thunderSounds.Length)];
            thunderAudioSource.PlayOneShot(thunder, Mathf.Lerp(0.5f, 1f, (weatherIntensity - 0.7f) / 0.3f));
            
            // Déclencher un éclair
            if (enableLightning)
                TriggerLightning();
            
            ScheduleNextThunder();
        }
    }
    
    /// <summary>
    /// Planifie le prochain tonnerre
    /// </summary>
    void ScheduleNextThunder()
    {
        // Intervalle réduit avec l'intensité de l'orage
        float interval = Mathf.Lerp(maxThunderInterval, minThunderInterval, (weatherIntensity - 0.7f) / 0.3f);
        nextThunderTime = Time.time + interval;
    }
    
    /// <summary>
    /// Déclenche un éclair visuel
    /// </summary>
    void TriggerLightning()
    {
        isLightningActive = true;
        lightningTimer = lightningDuration;
        
        if (sunLight != null)
        {
            sunLight.intensity = originalSunIntensity * lightningIntensity;
        }
    }
    
    /// <summary>
    /// Met à jour l'effet d'éclair
    /// </summary>
    void UpdateLightning()
    {
        if (!isLightningActive) return;
        
        lightningTimer -= Time.deltaTime;
        
        if (lightningTimer <= 0f)
        {
            isLightningActive = false;
            // La lumière reviendra progressivement via UpdateWeather()
        }
    }
    
    /// <summary>
    /// Définit l'intensité météo (appelé par GameMenuController)
    /// </summary>
    public void SetWeatherIntensity(float intensity)
    {
        weatherIntensity = Mathf.Clamp01(intensity);
    }
    
    /// <summary>
    /// Retourne la force actuelle du vent
    /// </summary>
    public Vector3 GetWindForce()
    {
        return currentWindDirection;
    }
    
    /// <summary>
    /// Retourne la description textuelle de la météo actuelle
    /// </summary>
    public string GetWeatherDescription()
    {
        if (weatherIntensity < 0.1f)
            return "☀️ Ciel dégagé";
        else if (weatherIntensity < 0.3f)
            return "⛅ Légèrement nuageux";
        else if (weatherIntensity < 0.5f)
            return "🌧️ Pluie légère";
        else if (weatherIntensity < 0.7f)
            return "🌧️ Pluie modérée";
        else if (weatherIntensity < 0.85f)
            return "⛈️ Forte pluie";
        else
            return "⛈️ ORAGE VIOLENT";
    }
    
    /// <summary>
    /// Met à jour l'affichage de la météo
    /// </summary>
    void UpdateWeatherDisplay()
    {
        if (weatherDisplayText == null) return;
        
        string weatherDesc = GetWeatherDescription();
        float windSpeed = currentWindForce;
        int windDir = Mathf.RoundToInt(windDirection);
        
        string windInfo = windSpeed > 1f ? $"\n🌬️ Vent: {windSpeed:F1} m/s ({windDir}°)" : "";
        string fogInfo = (useFog && weatherIntensity > 0.3f) ? $"\n🌫️ Visibilité: {RenderSettings.fogEndDistance:F0}m" : "";
        
        weatherDisplayText.text = $"{weatherDesc}{windInfo}{fogInfo}";
    }
    
    /// <summary>
    /// Crée un système de particules pour la pluie
    /// </summary>
    void CreateRainParticles()
    {
        GameObject rainObj = new GameObject("RainParticles");
        rainObj.transform.parent = transform;
        rainParticles = rainObj.AddComponent<ParticleSystem>();
        
        var main = rainParticles.main;
        main.startLifetime = 3f; // Durée de vie ajustée
        main.startSpeed = 20f;
        main.startSize = 0.2f; // Taille visible
        main.startColor = new Color(0.8f, 0.8f, 1f, 0.9f); // Plus opaque
        main.maxParticles = 10000; // Plus de particules max
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 2f; // Plus de gravité pour accélérer vers le sol
        
        var emission = rainParticles.emission;
        emission.rateOverTime = 2000f; // Augmenté pour pluie continue
        
        var shape = rainParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(100f, 0.1f, 100f);
        shape.rotation = new Vector3(0f, 0f, 0f); // Rotation par défaut
        
        // Force les particules à tomber vers le bas
        var velocityOverLifetime = rainParticles.velocityOverLifetime;
        velocityOverLifetime.enabled = true;
        velocityOverLifetime.space = ParticleSystemSimulationSpace.World;
        velocityOverLifetime.y = -20f; // Force vers le bas
        
        var renderer = rainParticles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard; // Mode point au lieu de stretch
        renderer.sortMode = ParticleSystemSortMode.Distance;
        
        // Créer un matériau simple pour les particules
        Material rainMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        rainMaterial.color = new Color(1f, 1f, 1f, 1f); // Blanc opaque
        
        // Créer une texture ronde pour les gouttes
        Texture2D rainTexture = new Texture2D(32, 32);
        Color[] pixels = new Color[32 * 32];
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dx = (x - 16f) / 16f;
                float dy = (y - 16f) / 16f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(1f - distance);
                pixels[y * 32 + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        rainTexture.SetPixels(pixels);
        rainTexture.Apply();
        rainMaterial.mainTexture = rainTexture;
        rainMaterial.SetFloat("_Mode", 3); // Transparent
        rainMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        rainMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        rainMaterial.SetInt("_ZWrite", 0);
        rainMaterial.DisableKeyword("_ALPHATEST_ON");
        rainMaterial.EnableKeyword("_ALPHABLEND_ON");
        rainMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        rainMaterial.renderQueue = 3000;
        
        renderer.material = rainMaterial;
        
        Debug.Log("DynamicWeatherSystem: Système de pluie créé avec " + rainParticles.main.maxParticles + " particules max");
    }
    
    /// <summary>
    /// Crée un système de particules pour l'orage
    /// </summary>
    void CreateStormParticles()
    {
        GameObject stormObj = new GameObject("StormParticles");
        stormObj.transform.parent = transform;
        stormParticles = stormObj.AddComponent<ParticleSystem>();
        
        var main = stormParticles.main;
        main.startLifetime = 0.5f;
        main.startSpeed = 30f;
        main.startSize = 0.2f;
        main.startColor = new Color(1f, 1f, 1f, 0.8f);
        main.maxParticles = 500;
        
        var emission = stormParticles.emission;
        emission.rateOverTime = 10f;
    }
    
    /// <summary>
    /// Active le MissionManager avec un délai pour éviter les erreurs d'initialisation
    /// </summary>
    System.Collections.IEnumerator ActivateMissionManagerDelayed()
    {
        // Attendre que la scène soit complètement chargée
        yield return new WaitForEndOfFrame();
        yield return new WaitForSeconds(0.1f);
        
        int fromMainMenu = PlayerPrefs.GetInt("FromMainMenu", 0);
        
        if (fromMainMenu == 1)
        {
            // Effacer le flag immédiatement pour ne pas qu'il persiste
            PlayerPrefs.DeleteKey("FromMainMenu");
            PlayerPrefs.Save();
            
            // Vérifier qu'une mission a été sélectionnée
            int selectedMission = PlayerPrefs.GetInt("SelectedMission", -1);
            string selectedMissionName = PlayerPrefs.GetString("SelectedMissionName", "");
            
            if (selectedMission >= 0 || !string.IsNullOrEmpty(selectedMissionName))
            {
                // Activer le MissionManager
                MissionManager missionManager = FindObjectOfType<MissionManager>(true);
                if (missionManager != null && !missionManager.gameObject.activeInHierarchy)
                {
                    missionManager.gameObject.SetActive(true);
                    Debug.Log($"DynamicWeatherSystem: MissionManager activé pour mission: {selectedMissionName} (index: {selectedMission})");
                }
            }
            else
            {
                Debug.Log("DynamicWeatherSystem: Lancé depuis MainMenu mais aucune mission sélectionnée");
            }
        }
        else
        {
            Debug.Log("DynamicWeatherSystem: Scène lancée directement (pas depuis MainMenu), MissionManager reste désactivé");
        }
    }
}
