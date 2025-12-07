using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Menu de démarrage simplifié qui s'affiche au lancement et permet de configurer avant de jouer
/// </summary>
public class StartMenuController : MonoBehaviour
{
    [Header("Panneaux")]
    [Tooltip("Panneau principal du menu de démarrage")]
    public GameObject startMenuPanel;
    
    [Tooltip("Panneau de sélection d'avion")]
    public GameObject aircraftPanel;
    
    [Tooltip("Panneau de paramètres rapides")]
    public GameObject quickSettingsPanel;
    
    [Tooltip("Panneau du didacticiel")]
    public GameObject tutorialPanel;
    
    [Header("Sélection d'Avion")]
    [Tooltip("Texte du nom de l'avion")]
    public Text aircraftNameText;
    
    [Tooltip("Texte de description")]
    public Text aircraftDescriptionText;
    
    [Tooltip("Index de l'avion sélectionné")]
    private int selectedAircraftIndex = 0;
    
    [Header("Paramètres Rapides")]
    [Tooltip("Slider de météo initiale")]
    public Slider initialWeatherSlider;
    
    [Tooltip("Slider d'heure initiale")]
    public Slider initialTimeSlider;
    
    [Tooltip("Text météo")]
    public Text weatherText;
    
    [Tooltip("Text heure")]
    public Text timeText;
    
    [Header("Didacticiel")]
    [Tooltip("Texte du didacticiel")]
    public Text tutorialText;
    
    [Tooltip("Numéro de page du didacticiel")]
    private int tutorialPage = 0;
    
    [Header("Références")]
    [Tooltip("GameMenuController pour appliquer les paramètres")]
    public GameMenuController gameMenuController;
    
    [Tooltip("Références aux objets à désactiver pendant le menu")]
    public GameObject flightObject;
    
    // Données des avions
    private string[] aircraftNames = { "Avion de Tourisme", "Avion Acrobatique", "Planeur" };
    private string[] aircraftDescriptions = {
        "Parfait pour débuter\n• Vitesse: 180 km/h\n• Stabilité: ★★★★★\n• Maniabilité: ★★★☆☆",
        "Pour pilotes expérimentés\n• Vitesse: 250 km/h\n• Stabilité: ★★★☆☆\n• Maniabilité: ★★★★★",
        "Vol silencieux\n• Vitesse: 120 km/h\n• Stabilité: ★★★★☆\n• Maniabilité: ★★★★☆"
    };
    
    // Pages de didacticiel
    private string[] tutorialPages = {
        "🎮 BIENVENUE !\n\nBienvenue dans le simulateur de vol.\n\nCe didacticiel vous apprendra les bases du pilotage.\n\n→ Utilisez les flèches pour naviguer",
        
        "✈️ CONTRÔLES DE BASE\n\nCLAVIER:\n• W/S: Pitch (monter/descendre)\n• A/D: Roll (incliner gauche/droite)\n• Q/E: Yaw (tourner)\n• Shift/Ctrl: Throttle (accélérer/ralentir)\n\nSOURIS:\n• Clic droit maintenu: Regarder autour",
        
        "🎛️ INTERFACE (HUD)\n\n• Altimètre: Votre altitude\n• Anémomètre: Votre vitesse\n• Horizon artificiel: Orientation\n• Compas: Direction\n\nMenu en vol: Bouton MENU en haut à droite",
        
        "🌤️ MÉTÉO DYNAMIQUE\n\nLa météo affecte votre vol:\n• Vent: Pousse l'avion\n• Pluie: Réduit visibilité\n• Brouillard: Limite vision\n• Orage: Turbulences fortes\n\nAjustez dans le menu en vol!",
        
        "🚀 DÉCOLLAGE\n\n1. Augmentez throttle (Shift)\n2. Accélérez sur la piste\n3. Tirez le manche (S) vers 120 km/h\n4. Maintenez angle stable\n5. Montez progressivement",
        
        "🛬 ATTERRISSAGE\n\n1. Réduisez throttle (Ctrl)\n2. Alignez avec la piste\n3. Descendez progressivement\n4. Vitesse ~100 km/h\n5. Touchez en douceur\n6. Freinez (B)",
        
        "✅ PRÊT À VOLER !\n\nConseils:\n• Commencez en beau temps\n• Pratiquez les virages\n• Ne volez pas trop vite près du sol\n• Gardez un œil sur l'altitude\n\nBon vol ! 🛫"
    };

    void Start()
    {
        // Trouver GameMenuController si non assigné
        if (gameMenuController == null)
            gameMenuController = FindObjectOfType<GameMenuController>();
        
        // Initialiser les sliders
        if (initialWeatherSlider != null)
        {
            initialWeatherSlider.value = 0.3f;
            initialWeatherSlider.onValueChanged.AddListener(OnWeatherSliderChanged);
            UpdateWeatherText();
        }
        
        if (initialTimeSlider != null)
        {
            initialTimeSlider.minValue = 0f;
            initialTimeSlider.maxValue = 24f;
            initialTimeSlider.value = 12f;
            initialTimeSlider.onValueChanged.AddListener(OnTimeSliderChanged);
            UpdateTimeText();
        }
        
        // Afficher le menu principal au démarrage
        ShowMainPanel();
        
        // Activer le curseur
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // Mettre le jeu en pause
        Time.timeScale = 0f;
        
        // Désactiver les contrôles de vol
        if (flightObject != null)
        {
            var plane = flightObject.GetComponent<MFlight.Demo.Plane>();
            if (plane != null) plane.enabled = false;
        }
    }

    #region Navigation Panneaux

    public void ShowMainPanel()
    {
        HideAllPanels();
        if (startMenuPanel != null) startMenuPanel.SetActive(true);
    }

    public void ShowAircraftPanel()
    {
        HideAllPanels();
        if (aircraftPanel != null)
        {
            aircraftPanel.SetActive(true);
            UpdateAircraftDisplay();
        }
    }

    public void ShowQuickSettingsPanel()
    {
        HideAllPanels();
        if (quickSettingsPanel != null) quickSettingsPanel.SetActive(true);
    }

    public void ShowTutorialPanel()
    {
        HideAllPanels();
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPage = 0;
            UpdateTutorialDisplay();
        }
    }

    void HideAllPanels()
    {
        if (startMenuPanel != null) startMenuPanel.SetActive(false);
        if (aircraftPanel != null) aircraftPanel.SetActive(false);
        if (quickSettingsPanel != null) quickSettingsPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    #endregion

    #region Avion

    public void PreviousAircraft()
    {
        selectedAircraftIndex--;
        if (selectedAircraftIndex < 0)
            selectedAircraftIndex = aircraftNames.Length - 1;
        UpdateAircraftDisplay();
    }

    public void NextAircraft()
    {
        selectedAircraftIndex++;
        if (selectedAircraftIndex >= aircraftNames.Length)
            selectedAircraftIndex = 0;
        UpdateAircraftDisplay();
    }

    void UpdateAircraftDisplay()
    {
        if (aircraftNameText != null)
            aircraftNameText.text = aircraftNames[selectedAircraftIndex];
        
        if (aircraftDescriptionText != null)
            aircraftDescriptionText.text = aircraftDescriptions[selectedAircraftIndex];
    }

    #endregion

    #region Paramètres

    void OnWeatherSliderChanged(float value)
    {
        UpdateWeatherText();
    }

    void OnTimeSliderChanged(float value)
    {
        UpdateTimeText();
    }

    void UpdateWeatherText()
    {
        if (weatherText != null && initialWeatherSlider != null)
        {
            float value = initialWeatherSlider.value;
            string desc = value < 0.3f ? "Beau" : value < 0.6f ? "Nuageux" : value < 0.8f ? "Couvert" : "Orage";
            weatherText.text = $"Météo: {desc}";
        }
    }

    void UpdateTimeText()
    {
        if (timeText != null && initialTimeSlider != null)
        {
            float value = initialTimeSlider.value;
            int hours = Mathf.FloorToInt(value);
            int minutes = Mathf.FloorToInt((value - hours) * 60f);
            timeText.text = $"Heure: {hours:D2}:{minutes:D2}";
        }
    }

    #endregion

    #region Didacticiel

    public void PreviousTutorialPage()
    {
        tutorialPage--;
        if (tutorialPage < 0) tutorialPage = 0;
        UpdateTutorialDisplay();
    }

    public void NextTutorialPage()
    {
        tutorialPage++;
        if (tutorialPage >= tutorialPages.Length)
            tutorialPage = tutorialPages.Length - 1;
        UpdateTutorialDisplay();
    }

    void UpdateTutorialDisplay()
    {
        if (tutorialText != null)
            tutorialText.text = tutorialPages[tutorialPage] + $"\n\nPage {tutorialPage + 1}/{tutorialPages.Length}";
    }

    #endregion

    #region Démarrer le Jeu

    /// <summary>
    /// Lance le jeu avec les paramètres sélectionnés
    /// </summary>
    public void StartGame()
    {
        // Appliquer les paramètres au GameMenuController
        if (gameMenuController != null)
        {
            if (initialWeatherSlider != null && gameMenuController.weatherSlider != null)
            {
                gameMenuController.weatherSlider.value = initialWeatherSlider.value;
            }
            
            if (initialTimeSlider != null && gameMenuController.timeSlider != null)
            {
                gameMenuController.timeSlider.value = initialTimeSlider.value;
            }
        }
        
        // Sauvegarder l'avion sélectionné
        PlayerPrefs.SetInt("SelectedAircraft", selectedAircraftIndex);
        
        // Cacher le menu de démarrage
        gameObject.SetActive(false);
        
        // Reprendre le jeu
        Time.timeScale = 1f;
        
        // Réactiver les contrôles de vol
        if (flightObject != null)
        {
            var plane = flightObject.GetComponent<MFlight.Demo.Plane>();
            if (plane != null) plane.enabled = true;
        }
        
        // Gérer le curseur
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = true;
        
        Debug.Log("Jeu lancé avec avion " + selectedAircraftIndex);
    }

    /// <summary>
    /// Quitte le jeu
    /// </summary>
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    #endregion
}
