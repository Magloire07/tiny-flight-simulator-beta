using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Gère les paramètres et restrictions des missions
/// </summary>
public class MissionManager : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Système météo dynamique")]
    public DynamicWeatherSystem weatherSystem;
    
    [Tooltip("Contrôleur du menu in-game")]
    public GameMenuController gameMenuController;
    
    [Header("Mission 3 - Paramètres")]
    [Tooltip("Intensité météo pour Mission 3 (orage)")]
    public float mission3WeatherIntensity = 1f;
    
    [Tooltip("Heure pour Mission 3 (24h = minuit)")]
    public float mission3TimeOfDay = 24f;
    
    [Header("Mission 3 - Sons d'Halloween")]
    [Tooltip("Premier son d'Halloween de terreur")]
    public AudioClip halloweenSound1;
    
    [Tooltip("Deuxième son d'Halloween de terreur")]
    public AudioClip halloweenSound2;
    
    [Tooltip("Volume des sons d'Halloween (0-1)")]
    [Range(0f, 1f)]
    public float halloweenSoundVolume = 0.5f;
    
    [Tooltip("Intervalle minimum entre les sons (secondes)")]
    public float minSoundInterval = 30f;
    
    [Tooltip("Intervalle maximum entre les sons (secondes)")]
    public float maxSoundInterval = 60f;
    
    [Header("Mission 3 - Image Effrayante")]
    [Tooltip("Image effrayante qui défile sur l'écran")]
    public Sprite scaryImage;
    
    [Tooltip("Vitesse de défilement de l'image (pixels/seconde)")]
    public float scaryImageSpeed = 500f;
    
    [Tooltip("Intervalle minimum entre les apparitions de l'image (secondes)")]
    public float minImageInterval = 1f;
    
    [Tooltip("Intervalle maximum entre les apparitions de l'image (secondes)")]
    public float maxImageInterval = 120f;
    
    [Tooltip("Taille de l'image effrayante (largeur en pixels)")]
    public float scaryImageSize = 200f;
    
    // État de la mission
    private int selectedMissionIndex = -1;
    private bool isMissionLocked = false;
    private AudioSource halloweenAudioSource1;
    private AudioSource halloweenAudioSource2;
    private bool isMission3Active = false;
    private Canvas scaryImageCanvas;
    private GameObject scaryImageObject;
    
    void Start()
    {
        // Récupérer l'index de la mission sélectionnée
        selectedMissionIndex = PlayerPrefs.GetInt("SelectedMission", -1);
        
        // Trouver les références si non assignées
        if (weatherSystem == null)
            weatherSystem = FindObjectOfType<DynamicWeatherSystem>();
        
        if (gameMenuController == null)
            gameMenuController = FindObjectOfType<GameMenuController>();
        
        // Créer les AudioSources dédiées pour les sons d'Halloween
        halloweenAudioSource1 = gameObject.AddComponent<AudioSource>();
        halloweenAudioSource1.loop = false;
        halloweenAudioSource1.playOnAwake = false;
        halloweenAudioSource1.spatialBlend = 0f; // Son 2D (non spatialisé)
        halloweenAudioSource1.volume = halloweenSoundVolume;
        halloweenAudioSource1.priority = 128; // Priorité moyenne
        
        halloweenAudioSource2 = gameObject.AddComponent<AudioSource>();
        halloweenAudioSource2.loop = false;
        halloweenAudioSource2.playOnAwake = false;
        halloweenAudioSource2.spatialBlend = 0f; // Son 2D (non spatialisé)
        halloweenAudioSource2.volume = halloweenSoundVolume;
        halloweenAudioSource2.priority = 128; // Priorité moyenne
        
        // Appliquer les paramètres de la mission
        ApplyMissionSettings();
    }
    
    /// <summary>
    /// Applique les paramètres spécifiques à la mission sélectionnée
    /// </summary>
    void ApplyMissionSettings()
    {
        // Vérifier si c'est Mission3 en lisant le nom depuis PlayerPrefs
        // On récupère l'index puis on cherche l'objet avec tag "mission3"
        GameObject mission3Object = GameObject.FindGameObjectWithTag("mission3");
        bool isMission3Selected = false;
        
        if (mission3Object != null)
        {
            // Vérifier si cette mission3 existe dans la sélection
            // On considère que si mission3 existe avec le tag, et qu'une mission est sélectionnée, on vérifie
            isMission3Selected = true; // On appliquera les settings si le tag mission3 existe
            Debug.Log("MissionManager: Tag 'mission3' détecté - Application des paramètres spéciaux");
        }
        
        // Alternative: vérifier via le nom sauvegardé
        string selectedMissionName = PlayerPrefs.GetString("SelectedMissionName", "");
        if (selectedMissionName.Contains("mission3") || selectedMissionName.Contains("Mission3"))
        {
            isMission3Selected = true;
        }
        
        if (isMission3Selected && selectedMissionIndex >= 0)
        {
            Debug.Log("MissionManager: Application des paramètres Mission 3 (Orage à minuit)");
            
            // Activer Mission 3
            isMission3Active = true;
            
            // Appliquer la météo orageuse
            if (weatherSystem != null)
            {
                weatherSystem.SetWeatherIntensity(mission3WeatherIntensity);
            }
            
            // Verrouiller les paramètres
            isMissionLocked = true;
            
            // Désactiver les sliders dans le menu et appliquer les valeurs
            if (gameMenuController != null)
            {
                // Attendre un frame pour que tout soit initialisé
                StartCoroutine(ApplyMission3SettingsDelayed());
            }
            
            // Démarrer les sons d'Halloween
            if (halloweenSound1 != null || halloweenSound2 != null)
            {
                StartCoroutine(PlayHalloweenSoundsRoutine());
            }
            
            // Démarrer l'image effrayante
            if (scaryImage != null)
            {
                CreateScaryImageCanvas();
                StartCoroutine(ShowScaryImageRoutine());
            }
        }
        else
        {
            // Missions libres - tout est modifiable
            isMissionLocked = false;
            
            if (gameMenuController != null)
            {
                gameMenuController.SetWeatherSliderLocked(false);
                gameMenuController.SetTimeSliderLocked(false);
            }
        }
    }
    
    /// <summary>
    /// Vérifie si les paramètres sont verrouillés pour cette mission
    /// </summary>
    public bool IsLocked()
    {
        return isMissionLocked;
    }
    
    /// <summary>
    /// Obtient l'index de la mission actuelle
    /// </summary>
    public int GetMissionIndex()
    {
        return selectedMissionIndex;
    }
    
    /// <summary>
    /// Applique les paramètres de Mission 3 avec un délai pour l'initialisation
    /// </summary>
    IEnumerator ApplyMission3SettingsDelayed()
    {
        // Attendre que le GameMenuController soit complètement initialisé
        yield return new WaitForSeconds(0.5f);
        
        if (gameMenuController != null)
        {
            // Forcer les valeurs
            gameMenuController.SetWeatherValue(mission3WeatherIntensity);
            gameMenuController.SetTimeValue(mission3TimeOfDay);
            
            // Verrouiller les sliders
            gameMenuController.SetWeatherSliderLocked(true);
            gameMenuController.SetTimeSliderLocked(true);
            
            Debug.Log($"MissionManager: Paramètres Mission 3 appliqués - Météo: {mission3WeatherIntensity}, Heure: {mission3TimeOfDay}h");
        }
    }
    
    /// <summary>
    /// Joue les sons d'Halloween de manière aléatoire pendant Mission 3
    /// </summary>
    IEnumerator PlayHalloweenSoundsRoutine()
    {
        Debug.Log("MissionManager: Démarrage des sons d'Halloween pour Mission 3");
        
        while (isMission3Active)
        {
            // Attendre un intervalle aléatoire
            float waitTime = Random.Range(minSoundInterval, maxSoundInterval);
            yield return new WaitForSeconds(waitTime);
            
            // Choisir un son aléatoire parmi les deux et l'AudioSource correspondante
            bool useSound1 = Random.value > 0.5f;
            
            if (useSound1 && halloweenSound1 != null && halloweenAudioSource1 != null)
            {
                // Jouer le son 1 sur son AudioSource dédiée
                halloweenAudioSource1.volume = halloweenSoundVolume;
                halloweenAudioSource1.PlayOneShot(halloweenSound1);
                Debug.Log($"MissionManager: Son d'Halloween 1 joué - {halloweenSound1.name}");
            }
            else if (!useSound1 && halloweenSound2 != null && halloweenAudioSource2 != null)
            {
                // Jouer le son 2 sur son AudioSource dédiée
                halloweenAudioSource2.volume = halloweenSoundVolume;
                halloweenAudioSource2.PlayOneShot(halloweenSound2);
                Debug.Log($"MissionManager: Son d'Halloween 2 joué - {halloweenSound2.name}");
            }
            else if (halloweenSound1 != null && halloweenAudioSource1 != null)
            {
                // Fallback: jouer le son 1 si le son 2 n'est pas disponible
                halloweenAudioSource1.volume = halloweenSoundVolume;
                halloweenAudioSource1.PlayOneShot(halloweenSound1);
                Debug.Log($"MissionManager: Son d'Halloween 1 joué (fallback) - {halloweenSound1.name}");
            }
            else if (halloweenSound2 != null && halloweenAudioSource2 != null)
            {
                // Fallback: jouer le son 2 si le son 1 n'est pas disponible
                halloweenAudioSource2.volume = halloweenSoundVolume;
                halloweenAudioSource2.PlayOneShot(halloweenSound2);
                Debug.Log($"MissionManager: Son d'Halloween 2 joué (fallback) - {halloweenSound2.name}");
            }
        }
    }
    
    /// <summary>
    /// Crée le Canvas pour l'image effrayante
    /// </summary>
    void CreateScaryImageCanvas()
    {
        // Créer un Canvas overlay pour l'image
        GameObject canvasObject = new GameObject("ScaryImageCanvas");
        scaryImageCanvas = canvasObject.AddComponent<Canvas>();
        scaryImageCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        scaryImageCanvas.sortingOrder = 1000; // Au-dessus de tout
        
        // Ajouter un CanvasScaler
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        
        // Ajouter un GraphicRaycaster (optionnel mais standard)
        canvasObject.AddComponent<GraphicRaycaster>();
        
        // Créer l'objet Image
        scaryImageObject = new GameObject("ScaryImage");
        scaryImageObject.transform.SetParent(canvasObject.transform, false);
        
        Image imageComponent = scaryImageObject.AddComponent<Image>();
        imageComponent.sprite = scaryImage;
        imageComponent.preserveAspect = true;
        
        // Configurer le RectTransform
        RectTransform rectTransform = scaryImageObject.GetComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(scaryImageSize, scaryImageSize);
        
        // Cacher l'image au départ
        scaryImageObject.SetActive(false);
        
        Debug.Log("MissionManager: Canvas d'image effrayante créé");
    }
    
    /// <summary>
    /// Affiche l'image effrayante à intervalles aléatoires
    /// </summary>
    IEnumerator ShowScaryImageRoutine()
    {
        Debug.Log("MissionManager: Démarrage du système d'image effrayante");
        
        while (isMission3Active)
        {
            // Attendre un intervalle aléatoire
            float waitTime = Random.Range(minImageInterval, maxImageInterval);
            yield return new WaitForSeconds(waitTime);
            
            // Faire défiler l'image
            if (scaryImageObject != null)
            {
                yield return StartCoroutine(ScrollScaryImage());
            }
        }
    }
    
    /// <summary>
    /// Fait défiler l'image effrayante sur l'écran
    /// </summary>
    IEnumerator ScrollScaryImage()
    {
        RectTransform rectTransform = scaryImageObject.GetComponent<RectTransform>();
        
        // Choisir une direction aléatoire (0=haut, 1=droite, 2=bas, 3=gauche)
        int direction = Random.Range(0, 4);
        
        Vector2 startPos = Vector2.zero;
        Vector2 endPos = Vector2.zero;
        
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;
        
        switch (direction)
        {
            case 0: // Bas vers Haut
                startPos = new Vector2(Random.Range(-screenWidth/2, screenWidth/2), -screenHeight/2 - scaryImageSize);
                endPos = new Vector2(startPos.x, screenHeight/2 + scaryImageSize);
                break;
                
            case 1: // Gauche vers Droite
                startPos = new Vector2(-screenWidth/2 - scaryImageSize, Random.Range(-screenHeight/2, screenHeight/2));
                endPos = new Vector2(screenWidth/2 + scaryImageSize, startPos.y);
                break;
                
            case 2: // Haut vers Bas
                startPos = new Vector2(Random.Range(-screenWidth/2, screenWidth/2), screenHeight/2 + scaryImageSize);
                endPos = new Vector2(startPos.x, -screenHeight/2 - scaryImageSize);
                break;
                
            case 3: // Droite vers Gauche
                startPos = new Vector2(screenWidth/2 + scaryImageSize, Random.Range(-screenHeight/2, screenHeight/2));
                endPos = new Vector2(-screenWidth/2 - scaryImageSize, startPos.y);
                break;
        }
        
        // Afficher l'image
        scaryImageObject.SetActive(true);
        rectTransform.anchoredPosition = startPos;
        
        // Calculer la durée du défilement
        float distance = Vector2.Distance(startPos, endPos);
        float duration = distance / scaryImageSpeed;
        
        // Défilement progressif
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        // Cacher l'image à la fin
        scaryImageObject.SetActive(false);
        
        Debug.Log($"MissionManager: Image effrayante défilée (direction: {direction})");
    }
}
