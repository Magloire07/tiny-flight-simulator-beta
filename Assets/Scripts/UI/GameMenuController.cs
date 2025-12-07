using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

/// <summary>
/// Contrôle le menu de réglages du jeu (météo, heure, vue caméra)
/// </summary>
public class GameMenuController : MonoBehaviour
{
    [Header("Références Menu UI")]
    [Tooltip("Panel principal du menu")]
    public GameObject menuPanel;
    
    [Tooltip("Objet bouton (GameObject avec Image) pour ouvrir/fermer le menu")]
    public GameObject menuButton;
    
    [Tooltip("Touche alternative pour ouvrir/fermer le menu (optionnel)")]
    public KeyCode menuKey = KeyCode.None;
    
    [Header("Météo")]
    [Tooltip("Slider pour l'intensité de la météo (0=beau, 1=orageux)")]
    public Slider weatherSlider;
    
    [Tooltip("Texte affichant la valeur météo")]
    public Text weatherValueText;
    
    [Tooltip("Référence au WeatherMap pour modifier la météo")]
    public WeatherMap weatherMap;
    
    [Tooltip("Référence au système de météo dynamique")]
    public DynamicWeatherSystem dynamicWeather;
    
    [Header("Heure")]
    [Tooltip("Slider pour l'heure de la journée (0=minuit, 12=midi, 24=minuit)")]
    public Slider timeSlider;
    
    [Tooltip("Texte affichant l'heure")]
    public Text timeValueText;
    
    [Tooltip("Lumière directionnelle (soleil) à contrôler")]
    public Light sunLight;
    
    [Tooltip("Référence au CloudMaster pour modifier les couleurs du ciel")]
    public CloudMaster cloudMaster;
    
    [Header("Vue Caméra")]
    [Tooltip("Button pour basculer cockpit/extérieur")]
    public Button toggleViewButton;
    
    [Tooltip("Texte du bouton de vue")]
    public Text toggleViewButtonText;
    
    [Tooltip("Référence au CameraViewSwitcher")]
    public CameraViewSwitcher cameraViewSwitcher;
    
    [Header("Contrôle Entrée")]
    [Tooltip("Bouton pour basculer clavier/joystick")]
    public Button toggleInputButton;
    
    [Tooltip("Texte du bouton de contrôle")]
    public Text toggleInputButtonText;
    
    [Tooltip("Référence au InputModeController")]
    public InputModeController inputModeController;
    
    [Header("Menu Principal")]
    [Tooltip("Bouton pour retourner au menu principal")]
    public Button returnToMainMenuButton;
    
    [Header("Réglages")]
    [Tooltip("Désactiver le contrôle de l'avion quand le menu est ouvert")]
    public bool disableFlightControlsWhenMenuOpen = true;
    
    // État
    private bool isMenuOpen = false;
    private float currentWeatherIntensity = 0.5f;
    private float currentTimeOfDay = 12f; // Midi par défaut
    
    void Start()
    {
        // Trouver les références automatiquement si non assignées
        if (weatherMap == null)
            weatherMap = FindObjectOfType<WeatherMap>();
        
        if (dynamicWeather == null)
            dynamicWeather = FindObjectOfType<DynamicWeatherSystem>();
        
        if (cameraViewSwitcher == null)
            cameraViewSwitcher = FindObjectOfType<CameraViewSwitcher>();
        
        if (cloudMaster == null)
            cloudMaster = FindObjectOfType<CloudMaster>();
        
        if (inputModeController == null)
            inputModeController = FindObjectOfType<InputModeController>();
        
        if (sunLight == null)
        {
            // Chercher une lumière directionnelle
            Light[] lights = FindObjectsOfType<Light>();
            foreach (Light light in lights)
            {
                if (light.type == LightType.Directional)
                {
                    sunLight = light;
                    break;
                }
            }
        }
        
        // Initialiser les sliders
        if (weatherSlider != null)
        {
            weatherSlider.value = currentWeatherIntensity;
            weatherSlider.onValueChanged.AddListener(OnWeatherChanged);
            UpdateWeatherText();
        }
        
        if (timeSlider != null)
        {
            timeSlider.minValue = 0f;
            timeSlider.maxValue = 24f;
            timeSlider.value = currentTimeOfDay;
            timeSlider.onValueChanged.AddListener(OnTimeChanged);
            UpdateTimeText();
        }
        
        if (toggleViewButton != null)
        {
        if (toggleInputButton != null)
        {
            toggleInputButton.onClick.AddListener(OnToggleInputClicked);
            UpdateInputButtonText();
        }
        
        if (returnToMainMenuButton != null)
        {
            returnToMainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }
        
        // Ajouter le listener pour le bouton menuOnToggleInputClicked);
            UpdateInputButtonText();
        }
        
        // Ajouter le listener pour le bouton menu
        if (menuButton != null)
        {
            Debug.Log("GameMenuController: Configuration du bouton menu sur " + menuButton.name);
            
            // Ajouter EventTrigger pour détecter les clics sur le GameObject
            EventTrigger trigger = menuButton.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = menuButton.AddComponent<EventTrigger>();
                Debug.Log("GameMenuController: EventTrigger ajouté");
            }
            
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.PointerClick;
            entry.callback.AddListener((data) => { 
                Debug.Log("GameMenuController: Clic détecté sur le bouton menu!");
                ToggleMenu(); 
            });
            trigger.triggers.Add(entry);
        }
        else
        {
            Debug.LogWarning("GameMenuController: menuButton n'est pas assigné!");
        }
        
        // Fermer le menu au démarrage
        if (menuPanel != null)
            menuPanel.SetActive(false);
        
        isMenuOpen = false;
    }
    
    void Update()
    {
        // Détecter l'appui sur la touche menu (si définie)
        if (menuKey != KeyCode.None && Input.GetKeyDown(menuKey))
        {
            ToggleMenu();
        }
    }
    
    /// <summary>
    /// Ouvre ou ferme le menu
    /// </summary>
    public void ToggleMenu()
    {
        Debug.Log("GameMenuController: ToggleMenu appelé. État actuel: " + isMenuOpen);
        
        isMenuOpen = !isMenuOpen;
        
        Debug.Log("GameMenuController: Nouvel état: " + isMenuOpen);
        
        if (menuPanel != null)
        {
            menuPanel.SetActive(isMenuOpen);
            Debug.Log("GameMenuController: MenuPanel.SetActive(" + isMenuOpen + ")");
        }
        else
        {
            Debug.LogWarning("GameMenuController: menuPanel n'est pas assigné!");
        }
        
        // Gérer le curseur
        if (isMenuOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (!Application.isEditor)
            {
                Cursor.lockState = CursorLockMode.Confined;
                Cursor.visible = false;
            }
        }
        
        // Désactiver les contrôles de vol si nécessaire
        if (disableFlightControlsWhenMenuOpen)
        {
            // Vous pouvez ajouter ici la désactivation des scripts de contrôle
            // Exemple: plane.enabled = !isMenuOpen;
        }
        
        Debug.Log("Menu " + (isMenuOpen ? "ouvert" : "fermé"));
    }
    
    /// <summary>
    /// Appelé quand le slider météo change
    /// </summary>
    void OnWeatherChanged(float value)
    {
        currentWeatherIntensity = value;
        UpdateWeatherText();
        
        // Appliquer la météo sur le WeatherMap
        if (weatherMap != null)
        {
            // Modifier minMax pour contrôler la densité des nuages
            // INVERSÉ: value 0 = beaucoup de nuages, value 1 = peu de nuages
            // Donc on inverse avec (1 - value)
            weatherMap.minMax = new Vector2(0f, Mathf.Lerp(0.3f, 1.0f, 1f - value));
            
            // Régénérer la weather map avec les nouveaux paramètres
            weatherMap.UpdateMap();
            
            Debug.Log("Météo mise à jour: " + value + " (minMax: " + weatherMap.minMax + ")");
        }
        
        // Appliquer la météo dynamique (pluie, orage, etc.)
        if (dynamicWeather != null)
        {
            dynamicWeather.SetWeatherIntensity(value);
        }
    }
    
    /// <summary>
    /// Appelé quand le slider temps change
    /// </summary>
    void OnTimeChanged(float value)
    {
        currentTimeOfDay = value;
        UpdateTimeText();
        UpdateSunRotation();
    }
    
    /// <summary>
    /// Met à jour la rotation du soleil selon l'heure
    /// </summary>
    void UpdateSunRotation()
    {
        if (sunLight == null) return;
        
        // Calculer l'angle du soleil (0h = -90°, 12h = 90°, 24h = -90°)
        // Lever: 6h (0°), Midi: 12h (90°), Coucher: 18h (0°)
        float normalizedTime = currentTimeOfDay / 24f;
        float sunAngle = (normalizedTime * 360f) - 90f; // -90° à 270°
        
        // Appliquer la rotation
        sunLight.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0f);
        
        // Ajuster l'intensité selon l'heure
        if (currentTimeOfDay >= 6f && currentTimeOfDay <= 18f)
        {
            // Jour
            float dayProgress = (currentTimeOfDay - 6f) / 12f; // 0 à 1
            sunLight.intensity = Mathf.Sin(dayProgress * Mathf.PI); // Courbe sinusoïdale
        }
        else
        {
            // Nuit
            sunLight.intensity = 0.05f; // Lumière lunaire très faible
        }
        
        // Changer la couleur du soleil
        if (currentTimeOfDay >= 5f && currentTimeOfDay <= 7f)
        {
            // Aube (orange)
            sunLight.color = Color.Lerp(new Color(1f, 0.5f, 0.3f), Color.white, (currentTimeOfDay - 5f) / 2f);
        }
        else if (currentTimeOfDay >= 17f && currentTimeOfDay <= 19f)
        {
            // Crépuscule (orange)
            sunLight.color = Color.Lerp(Color.white, new Color(1f, 0.5f, 0.3f), (currentTimeOfDay - 17f) / 2f);
        }
        else if (currentTimeOfDay >= 7f && currentTimeOfDay <= 17f)
        {
            // Jour (blanc)
            sunLight.color = Color.white;
        }
        else
        {
            // Nuit (bleu foncé)
            sunLight.color = new Color(0.5f, 0.6f, 1f);
        }
        
        // Modifier les couleurs du ciel dans CloudMaster
        UpdateSkyColors();
    }
    
    /// <summary>
    /// Met à jour les couleurs du ciel selon l'heure
    /// </summary>
    void UpdateSkyColors()
    {
        if (cloudMaster == null) return;
        
        // Définir les couleurs selon l'heure du jour
        if (currentTimeOfDay >= 5f && currentTimeOfDay <= 7f)
        {
            // Aube - Orange/Rose vers Bleu clair
            float t = (currentTimeOfDay - 5f) / 2f;
            cloudMaster.colA = Color.Lerp(new Color(1f, 0.4f, 0.2f), new Color(0.5f, 0.7f, 1f), t); // Horizon
            cloudMaster.colB = Color.Lerp(new Color(0.8f, 0.5f, 0.6f), new Color(0.3f, 0.5f, 0.9f), t); // Zénith
        }
        else if (currentTimeOfDay >= 7f && currentTimeOfDay <= 17f)
        {
            // Jour - Bleu ciel
            cloudMaster.colA = new Color(0.5f, 0.7f, 1f); // Bleu clair horizon
            cloudMaster.colB = new Color(0.3f, 0.5f, 0.9f); // Bleu plus foncé zénith
        }
        else if (currentTimeOfDay >= 17f && currentTimeOfDay <= 19f)
        {
            // Crépuscule - Bleu vers Orange/Rose
            float t = (currentTimeOfDay - 17f) / 2f;
            cloudMaster.colA = Color.Lerp(new Color(0.5f, 0.7f, 1f), new Color(1f, 0.4f, 0.2f), t); // Horizon
            cloudMaster.colB = Color.Lerp(new Color(0.3f, 0.5f, 0.9f), new Color(0.8f, 0.3f, 0.4f), t); // Zénith
        }
        else if (currentTimeOfDay >= 19f || currentTimeOfDay <= 5f)
        {
            // Nuit - Bleu très foncé/Noir
            cloudMaster.colA = new Color(0.05f, 0.05f, 0.15f); // Presque noir avec teinte bleue
            cloudMaster.colB = new Color(0.02f, 0.02f, 0.1f); // Noir bleuté
        }
    }
    
    /// <summary>
    /// Appelé quand le bouton vue est cliqué
    /// </summary>
    public void OnToggleViewClicked()
    {
        Debug.Log("OnToggleViewClicked appelé!");
        
        if (cameraViewSwitcher != null)
        {
            Debug.Log("CameraViewSwitcher trouvé, appel de ToggleView()");
            cameraViewSwitcher.ToggleView();
            UpdateViewButtonText();
        }
        else
        {
            Debug.LogWarning("CameraViewSwitcher est null! Vérifiez la référence dans l'Inspector.");
        }
    }
    
    /// <summary>
    /// Met à jour le texte du slider météo
    /// </summary>
    void UpdateWeatherText()
    {
        if (weatherValueText != null)
        {
            string weatherDesc = "";
            if (currentWeatherIntensity < 0.3f)
                weatherDesc = "Beau temps";
            else if (currentWeatherIntensity < 0.6f)
                weatherDesc = "Nuageux";
            else if (currentWeatherIntensity < 0.8f)
                weatherDesc = "Couvert";
            else
                weatherDesc = "Orageux";
            
            weatherValueText.text = $"{weatherDesc} ({currentWeatherIntensity:F2})";
        }
    }
    
    /// <summary>
    /// Met à jour le texte du slider temps
    /// </summary>
    void UpdateTimeText()
    {
        if (timeValueText != null)
        {
            int hours = Mathf.FloorToInt(currentTimeOfDay);
            int minutes = Mathf.FloorToInt((currentTimeOfDay - hours) * 60f);
            timeValueText.text = $"{hours:D2}:{minutes:D2}";
        }
    }
    
    /// <summary>
    /// Met à jour le texte du bouton vue
    /// </summary>
    void UpdateViewButtonText()
    {
        if (toggleViewButtonText != null && cameraViewSwitcher != null)
        {
            toggleViewButtonText.text = cameraViewSwitcher.isCockpitView ? "Vue Extérieure" : "Vue Cockpit";
        }
    }
    
    /// <summary>
    /// Appelé quand le bouton contrôle est cliqué
    /// </summary>
    public void OnToggleInputClicked()
    {
        if (inputModeController != null)
        {
            inputModeController.ToggleInputMode();
            UpdateInputButtonText();
        }
    }
    
    /// <summary>
    /// Met à jour le texte du bouton contrôle
    /// </summary>
    void UpdateInputButtonText()
    {
        if (toggleInputButtonText != null && inputModeController != null)
        {
            toggleInputButtonText.text = "Contrôle: " + inputModeController.GetCurrentModeName();
        }
    }
    
    /// <summary>
    /// Retourne au menu principal
    /// </summary>
    public void ReturnToMainMenu()
    {
        Debug.Log("Retour au menu principal...");
        Time.timeScale = 1f; // S'assurer que le temps n'est pas en pause
        SceneManager.LoadScene("MainMenu");
    }
}
