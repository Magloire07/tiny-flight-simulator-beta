using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Contrôleur du menu principal avec sélection d'avion, scénarios, paramètres et didacticiel
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Panneaux du Menu")]
    [Tooltip("Panneau principal avec les boutons principaux")]
    public GameObject mainPanel;
    
    [Tooltip("Panneau de sélection d'avion")]
    public GameObject aircraftSelectionPanel;
    
    [Tooltip("Panneau de sélection de scénario")]
    public GameObject scenarioSelectionPanel;
    
    [Tooltip("Panneau des paramètres")]
    public GameObject settingsPanel;
    
    [Tooltip("Panneau du didacticiel")]
    public GameObject tutorialPanel;
    
    [Header("Sélection d'Avion")]
    [Tooltip("Liste des avions disponibles")]
    public List<AircraftData> availableAircraft = new List<AircraftData>();
    
    [Tooltip("Index de l'avion actuellement sélectionné")]
    private int selectedAircraftIndex = 0;
    
    [Tooltip("Texte affichant le nom de l'avion")]
    public Text aircraftNameText;
    
    [Tooltip("Texte affichant la description de l'avion")]
    public Text aircraftDescriptionText;
    
    [Tooltip("Image de prévisualisation de l'avion")]
    public Image aircraftPreviewImage;
    
    [Header("Sélection de Scénario")]
    [Tooltip("Liste des scénarios disponibles")]
    public List<ScenarioData> availableScenarios = new List<ScenarioData>();
    
    [Tooltip("Index du scénario actuellement sélectionné")]
    private int selectedScenarioIndex = 0;
    
    [Tooltip("Texte affichant le nom du scénario")]
    public Text scenarioNameText;
    
    [Tooltip("Texte affichant la description du scénario")]
    public Text scenarioDescriptionText;
    
    [Tooltip("Image de prévisualisation du scénario")]
    public Image scenarioPreviewImage;
    
    [Header("Paramètres")]
    [Tooltip("Slider pour le volume audio")]
    public Slider volumeSlider;
    
    [Tooltip("Slider pour la qualité graphique")]
    public Slider graphicsQualitySlider;
    
    [Tooltip("Toggle pour le mode plein écran")]
    public Toggle fullscreenToggle;
    
    [Tooltip("Dropdown pour la résolution")]
    public Dropdown resolutionDropdown;
    
    [Tooltip("Texte affichant la qualité graphique")]
    public Text graphicsQualityText;
    
    [Header("Didacticiel")]
    [Tooltip("Index de la page actuelle du didacticiel")]
    private int tutorialPageIndex = 0;
    
    [Tooltip("Liste des pages du didacticiel")]
    public List<TutorialPage> tutorialPages = new List<TutorialPage>();
    
    [Tooltip("Texte du titre du didacticiel")]
    public Text tutorialTitleText;
    
    [Tooltip("Texte du contenu du didacticiel")]
    public Text tutorialContentText;
    
    [Tooltip("Image du didacticiel")]
    public Image tutorialImage;
    
    [Tooltip("Bouton page précédente")]
    public Button previousPageButton;
    
    [Tooltip("Bouton page suivante")]
    public Button nextPageButton;
    
    [Header("Audio")]
    [Tooltip("Son de clic de bouton")]
    public AudioClip buttonClickSound;
    
    private AudioSource audioSource;
    
    private Resolution[] resolutions;

    void Start()
    {
        // Créer l'AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        
        // Charger les paramètres sauvegardés
        LoadSettings();
        
        // Initialiser les résolutions
        InitializeResolutions();
        
        // Initialiser les données par défaut si nécessaire
        InitializeDefaultData();
        
        // Afficher le panneau principal
        ShowMainPanel();
    }

    #region Navigation entre Panneaux

    /// <summary>
    /// Affiche le panneau principal
    /// </summary>
    public void ShowMainPanel()
    {
        HideAllPanels();
        if (mainPanel != null)
            mainPanel.SetActive(true);
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche le panneau de sélection d'avion
    /// </summary>
    public void ShowAircraftSelectionPanel()
    {
        HideAllPanels();
        if (aircraftSelectionPanel != null)
        {
            aircraftSelectionPanel.SetActive(true);
            UpdateAircraftDisplay();
        }
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche le panneau de sélection de scénario
    /// </summary>
    public void ShowScenarioSelectionPanel()
    {
        HideAllPanels();
        if (scenarioSelectionPanel != null)
        {
            scenarioSelectionPanel.SetActive(true);
            UpdateScenarioDisplay();
        }
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche le panneau des paramètres
    /// </summary>
    public void ShowSettingsPanel()
    {
        HideAllPanels();
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche le panneau du didacticiel
    /// </summary>
    public void ShowTutorialPanel()
    {
        HideAllPanels();
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            tutorialPageIndex = 0;
            UpdateTutorialDisplay();
        }
        PlayButtonSound();
    }

    /// <summary>
    /// Cache tous les panneaux
    /// </summary>
    void HideAllPanels()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (aircraftSelectionPanel != null) aircraftSelectionPanel.SetActive(false);
        if (scenarioSelectionPanel != null) scenarioSelectionPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (tutorialPanel != null) tutorialPanel.SetActive(false);
    }

    #endregion

    #region Sélection d'Avion

    /// <summary>
    /// Sélectionne l'avion précédent
    /// </summary>
    public void PreviousAircraft()
    {
        if (availableAircraft.Count == 0) return;
        
        selectedAircraftIndex--;
        if (selectedAircraftIndex < 0)
            selectedAircraftIndex = availableAircraft.Count - 1;
        
        UpdateAircraftDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Sélectionne l'avion suivant
    /// </summary>
    public void NextAircraft()
    {
        if (availableAircraft.Count == 0) return;
        
        selectedAircraftIndex++;
        if (selectedAircraftIndex >= availableAircraft.Count)
            selectedAircraftIndex = 0;
        
        UpdateAircraftDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Met à jour l'affichage de l'avion sélectionné
    /// </summary>
    void UpdateAircraftDisplay()
    {
        if (availableAircraft.Count == 0 || selectedAircraftIndex >= availableAircraft.Count)
            return;
        
        AircraftData aircraft = availableAircraft[selectedAircraftIndex];
        
        if (aircraftNameText != null)
            aircraftNameText.text = aircraft.aircraftName;
        
        if (aircraftDescriptionText != null)
            aircraftDescriptionText.text = aircraft.description;
        
        if (aircraftPreviewImage != null && aircraft.previewSprite != null)
            aircraftPreviewImage.sprite = aircraft.previewSprite;
        
        // Sauvegarder la sélection
        PlayerPrefs.SetInt("SelectedAircraft", selectedAircraftIndex);
    }

    #endregion

    #region Sélection de Scénario

    /// <summary>
    /// Sélectionne le scénario précédent
    /// </summary>
    public void PreviousScenario()
    {
        if (availableScenarios.Count == 0) return;
        
        selectedScenarioIndex--;
        if (selectedScenarioIndex < 0)
            selectedScenarioIndex = availableScenarios.Count - 1;
        
        UpdateScenarioDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Sélectionne le scénario suivant
    /// </summary>
    public void NextScenario()
    {
        if (availableScenarios.Count == 0) return;
        
        selectedScenarioIndex++;
        if (selectedScenarioIndex >= availableScenarios.Count)
            selectedScenarioIndex = 0;
        
        UpdateScenarioDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Met à jour l'affichage du scénario sélectionné
    /// </summary>
    void UpdateScenarioDisplay()
    {
        if (availableScenarios.Count == 0 || selectedScenarioIndex >= availableScenarios.Count)
            return;
        
        ScenarioData scenario = availableScenarios[selectedScenarioIndex];
        
        if (scenarioNameText != null)
            scenarioNameText.text = scenario.scenarioName;
        
        if (scenarioDescriptionText != null)
            scenarioDescriptionText.text = scenario.description;
        
        if (scenarioPreviewImage != null && scenario.previewSprite != null)
            scenarioPreviewImage.sprite = scenario.previewSprite;
        
        // Sauvegarder la sélection
        PlayerPrefs.SetInt("SelectedScenario", selectedScenarioIndex);
    }

    /// <summary>
    /// Lance le jeu avec les sélections actuelles
    /// </summary>
    public void StartGame()
    {
        PlayButtonSound();
        
        // Sauvegarder les sélections
        PlayerPrefs.SetInt("SelectedAircraft", selectedAircraftIndex);
        PlayerPrefs.SetInt("SelectedScenario", selectedScenarioIndex);
        PlayerPrefs.Save();
        
        // Charger la scène Flight Demo
        SceneManager.LoadScene("Flight Demo");
    }
    
    /// <summary>
    /// Lance le jeu avec un scénario spécifique
    /// </summary>
    public void StartGameWithScenario()
    {
        PlayButtonSound();
        
        // Sauvegarder les sélections
        PlayerPrefs.SetInt("SelectedAircraft", selectedAircraftIndex);
        PlayerPrefs.SetInt("SelectedScenario", selectedScenarioIndex);
        PlayerPrefs.Save();
        
        // Charger la scène correspondant au scénario
        if (availableScenarios.Count > 0 && selectedScenarioIndex < availableScenarios.Count)
        {
            string sceneName = availableScenarios[selectedScenarioIndex].sceneName;
            if (!string.IsNullOrEmpty(sceneName))
            {
                SceneManager.LoadScene(sceneName);
            }
            else
            {
                // Par défaut, charger Flight Demo
                SceneManager.LoadScene("Flight Demo");
            }
        }
        else
        {
            SceneManager.LoadScene("Flight Demo");
        }
    }
    
    /// <summary>
    /// Retourne au menu principal depuis la scène de jeu
    /// </summary>
    public static void ReturnToMainMenu()
    {
        Time.timeScale = 1f; // S'assurer que le temps n'est pas en pause
        SceneManager.LoadScene("MainMenu");
    }

    #endregion

    #region Paramètres

    /// <summary>
    /// Initialise la liste des résolutions
    /// </summary>
    void InitializeResolutions()
    {
        if (resolutionDropdown == null) return;
        
        resolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();
        
        List<string> options = new List<string>();
        int currentResolutionIndex = 0;
        
        for (int i = 0; i < resolutions.Length; i++)
        {
            string option = resolutions[i].width + " x " + resolutions[i].height + " @ " + resolutions[i].refreshRate + "Hz";
            options.Add(option);
            
            if (resolutions[i].width == Screen.currentResolution.width &&
                resolutions[i].height == Screen.currentResolution.height)
            {
                currentResolutionIndex = i;
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    /// <summary>
    /// Change le volume
    /// </summary>
    public void SetVolume(float volume)
    {
        AudioListener.volume = volume;
        PlayerPrefs.SetFloat("Volume", volume);
    }

    /// <summary>
    /// Change la qualité graphique
    /// </summary>
    public void SetGraphicsQuality(float quality)
    {
        int qualityLevel = Mathf.RoundToInt(quality);
        QualitySettings.SetQualityLevel(qualityLevel);
        PlayerPrefs.SetInt("GraphicsQuality", qualityLevel);
        
        if (graphicsQualityText != null)
        {
            string[] qualityNames = { "Très Bas", "Bas", "Moyen", "Élevé", "Très Élevé", "Ultra" };
            if (qualityLevel < qualityNames.Length)
                graphicsQualityText.text = qualityNames[qualityLevel];
        }
    }

    /// <summary>
    /// Change le mode plein écran
    /// </summary>
    public void SetFullscreen(bool isFullscreen)
    {
        Screen.fullScreen = isFullscreen;
        PlayerPrefs.SetInt("Fullscreen", isFullscreen ? 1 : 0);
    }

    /// <summary>
    /// Change la résolution
    /// </summary>
    public void SetResolution(int resolutionIndex)
    {
        if (resolutionIndex < 0 || resolutionIndex >= resolutions.Length)
            return;
        
        Resolution resolution = resolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionIndex", resolutionIndex);
    }

    /// <summary>
    /// Charge les paramètres sauvegardés
    /// </summary>
    void LoadSettings()
    {
        // Volume
        if (volumeSlider != null)
        {
            float volume = PlayerPrefs.GetFloat("Volume", 1f);
            volumeSlider.value = volume;
            AudioListener.volume = volume;
        }
        
        // Qualité graphique
        if (graphicsQualitySlider != null)
        {
            int quality = PlayerPrefs.GetInt("GraphicsQuality", QualitySettings.GetQualityLevel());
            graphicsQualitySlider.value = quality;
            QualitySettings.SetQualityLevel(quality);
            SetGraphicsQuality(quality);
        }
        
        // Plein écran
        if (fullscreenToggle != null)
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.isOn = fullscreen;
            Screen.fullScreen = fullscreen;
        }
        
        // Résolution
        if (resolutionDropdown != null)
        {
            int resIndex = PlayerPrefs.GetInt("ResolutionIndex", resolutions.Length - 1);
            if (resIndex < resolutions.Length)
            {
                resolutionDropdown.value = resIndex;
            }
        }
        
        // Sélections
        selectedAircraftIndex = PlayerPrefs.GetInt("SelectedAircraft", 0);
        selectedScenarioIndex = PlayerPrefs.GetInt("SelectedScenario", 0);
    }

    #endregion

    #region Didacticiel

    /// <summary>
    /// Affiche la page précédente du didacticiel
    /// </summary>
    public void PreviousTutorialPage()
    {
        if (tutorialPages.Count == 0) return;
        
        tutorialPageIndex--;
        if (tutorialPageIndex < 0)
            tutorialPageIndex = 0;
        
        UpdateTutorialDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Affiche la page suivante du didacticiel
    /// </summary>
    public void NextTutorialPage()
    {
        if (tutorialPages.Count == 0) return;
        
        tutorialPageIndex++;
        if (tutorialPageIndex >= tutorialPages.Count)
            tutorialPageIndex = tutorialPages.Count - 1;
        
        UpdateTutorialDisplay();
        PlayButtonSound();
    }

    /// <summary>
    /// Met à jour l'affichage du didacticiel
    /// </summary>
    void UpdateTutorialDisplay()
    {
        if (tutorialPages.Count == 0 || tutorialPageIndex >= tutorialPages.Count)
            return;
        
        TutorialPage page = tutorialPages[tutorialPageIndex];
        
        if (tutorialTitleText != null)
            tutorialTitleText.text = page.title;
        
        if (tutorialContentText != null)
            tutorialContentText.text = page.content;
        
        if (tutorialImage != null && page.image != null)
            tutorialImage.sprite = page.image;
        
        // Activer/désactiver les boutons de navigation
        if (previousPageButton != null)
            previousPageButton.interactable = (tutorialPageIndex > 0);
        
        if (nextPageButton != null)
            nextPageButton.interactable = (tutorialPageIndex < tutorialPages.Count - 1);
    }

    #endregion

    #region Utilitaires

    /// <summary>
    /// Joue le son de clic de bouton
    /// </summary>
    void PlayButtonSound()
    {
        if (audioSource != null && buttonClickSound != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    /// <summary>
    /// Initialise les données par défaut
    /// </summary>
    void InitializeDefaultData()
    {
        // Avions par défaut si la liste est vide
        if (availableAircraft.Count == 0)
        {
            availableAircraft.Add(new AircraftData
            {
                aircraftName = "Avion de Tourisme",
                description = "Parfait pour les débutants. Maniable et stable.\n\n" +
                             "• Vitesse max: 180 km/h\n" +
                             "• Maniabilité: ★★★★☆\n" +
                             "• Stabilité: ★★★★★",
                prefabName = "TouristPlane"
            });
            
            availableAircraft.Add(new AircraftData
            {
                aircraftName = "Avion Acrobatique",
                description = "Pour les pilotes expérimentés. Très maniable.\n\n" +
                             "• Vitesse max: 250 km/h\n" +
                             "• Maniabilité: ★★★★★\n" +
                             "• Stabilité: ★★★☆☆",
                prefabName = "AcrobaticPlane"
            });
        }
        
        // Scénarios par défaut si la liste est vide
        if (availableScenarios.Count == 0)
        {
            availableScenarios.Add(new ScenarioData
            {
                scenarioName = "Vol Libre",
                description = "Explorez le monde librement sans contraintes.\n\n" +
                             "• Météo: Variable\n" +
                             "• Difficulté: ★☆☆☆☆\n" +
                             "• Durée: Illimitée",
                sceneName = "Flight Demo"
            });
            
            availableScenarios.Add(new ScenarioData
            {
                scenarioName = "Vol dans la Tempête",
                description = "Affrontez une météo difficile et testez vos compétences.\n\n" +
                             "• Météo: Tempête\n" +
                             "• Difficulté: ★★★★☆\n" +
                             "• Durée: 15 minutes",
                sceneName = "Flight Demo"
            });
        }
        
        // Pages de didacticiel par défaut si la liste est vide
        if (tutorialPages.Count == 0)
        {
            tutorialPages.Add(new TutorialPage
            {
                title = "🎮 Bienvenue !",
                content = "Bienvenue dans le simulateur de vol !\n\n" +
                         "Ce didacticiel vous guidera à travers les bases du pilotage.\n\n" +
                         "Utilisez les flèches pour naviguer entre les pages."
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "✈️ Contrôles de Base",
                content = "CLAVIER:\n" +
                         "• W/S: Pitch (monter/descendre)\n" +
                         "• A/D: Roll (incliner)\n" +
                         "• Q/E: Yaw (tourner)\n" +
                         "• Shift/Ctrl: Throttle (accélérer/ralentir)\n\n" +
                         "SOURIS:\n" +
                         "• Bouton droit maintenu: Regarder autour"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "🎛️ Interface",
                content = "HUD (Affichage Tête Haute):\n\n" +
                         "• Altimètre: Votre altitude actuelle\n" +
                         "• Anémomètre: Votre vitesse\n" +
                         "• Horizon artificiel: Votre orientation\n" +
                         "• Compas: Votre direction\n\n" +
                         "Menu (ESC): Accédez aux paramètres en vol"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "🌤️ Météo",
                content = "Le système météo dynamique affecte votre vol:\n\n" +
                         "• Vent: Pousse l'avion\n" +
                         "• Pluie: Réduit la visibilité\n" +
                         "• Brouillard: Limite la vision\n" +
                         "• Orage: Turbulences fortes\n\n" +
                         "Ajustez la météo dans le menu en vol!"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "🚀 Décollage",
                content = "Pour décoller:\n\n" +
                         "1. Augmentez le throttle (Shift)\n" +
                         "2. Accélérez sur la piste\n" +
                         "3. Tirez doucement sur le manche (S)\n" +
                         "4. Maintenez l'angle de montée stable\n" +
                         "5. Rétractez le train d'atterrissage si disponible"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "🛬 Atterrissage",
                content = "Pour atterrir:\n\n" +
                         "1. Réduisez le throttle\n" +
                         "2. Alignez-vous avec la piste\n" +
                         "3. Descendez progressivement\n" +
                         "4. Gardez une vitesse stable\n" +
                         "5. Touchez en douceur le sol\n" +
                         "6. Freinez (B)"
            });
            
            tutorialPages.Add(new TutorialPage
            {
                title = "✅ Prêt à Voler !",
                content = "Vous connaissez maintenant les bases !\n\n" +
                         "Conseils:\n" +
                         "• Commencez par le Vol Libre\n" +
                         "• Pratiquez les virages\n" +
                         "• Ne volez pas trop vite près du sol\n" +
                         "• Gardez toujours un œil sur l'altitude\n\n" +
                         "Bon vol ! 🛫"
            });
        }
    }

    /// <summary>
    /// Quitte le jeu
    /// </summary>
    public void QuitGame()
    {
        PlayButtonSound();
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    #endregion
}

/// <summary>
/// Données d'un avion
/// </summary>
[System.Serializable]
public class AircraftData
{
    public string aircraftName;
    public string description;
    public Sprite previewSprite;
    public string prefabName;
}

/// <summary>
/// Données d'un scénario
/// </summary>
[System.Serializable]
public class ScenarioData
{
    public string scenarioName;
    public string description;
    public Sprite previewSprite;
    public string sceneName;
}

/// <summary>
/// Page de didacticiel
/// </summary>
[System.Serializable]
public class TutorialPage
{
    public string title;
    [TextArea(5, 15)]
    public string content;
    public Sprite image;
}
