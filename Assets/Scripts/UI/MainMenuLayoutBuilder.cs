using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script helper pour construire automatiquement le layout du menu principal avec groupes harmonisés
/// </summary>
public class MainMenuLayoutBuilder : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("Hauteur des boutons principaux")]
    public float buttonHeight = 60f;
    
    [Tooltip("Espacement entre éléments")]
    public float spacing = 20f;
    
    [Tooltip("Padding des panneaux")]
    public RectOffset padding = new RectOffset(40, 40, 40, 40);
    
    [Header("Couleurs")]
    public Color primaryButtonColor = new Color(0.2f, 0.4f, 0.8f);
    public Color secondaryButtonColor = new Color(0.3f, 0.3f, 0.3f);
    public Color textColor = Color.white;
    public Color panelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    
    [Header("Fonts")]
    public int titleFontSize = 48;
    public int buttonFontSize = 24;
    public int normalTextSize = 18;

    [ContextMenu("Build Main Panel")]
    public void BuildMainPanel()
    {
        GameObject mainPanel = CreatePanel("MainPanel", transform);
        
        // Layout vertical principal
        VerticalLayoutGroup mainLayout = mainPanel.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = spacing * 2;
        mainLayout.padding = padding;
        mainLayout.childAlignment = TextAnchor.MiddleCenter;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = true;
        mainLayout.childForceExpandHeight = false;
        
        // Titre
        CreateTitle("SIMULATEUR DE VOL", mainPanel.transform);
        
        // Groupe de boutons principaux
        GameObject buttonGroup = CreateVerticalGroup("ButtonGroup", mainPanel.transform, spacing);
        
        CreateButton("ButtonJouer", "▶ JOUER", buttonGroup.transform, buttonHeight, primaryButtonColor);
        CreateButton("ButtonAvion", "✈️ SÉLECTION AVION", buttonGroup.transform, buttonHeight, secondaryButtonColor);
        CreateButton("ButtonParametres", "⚙️ PARAMÈTRES", buttonGroup.transform, buttonHeight, secondaryButtonColor);
        CreateButton("ButtonTutoriel", "📖 DIDACTICIEL", buttonGroup.transform, buttonHeight, secondaryButtonColor);
        CreateButton("ButtonQuitter", "❌ QUITTER", buttonGroup.transform, buttonHeight, new Color(0.8f, 0.2f, 0.2f));
        
        Debug.Log("MainPanel créé avec succès!");
    }

    [ContextMenu("Build Aircraft Selection Panel")]
    public void BuildAircraftSelectionPanel()
    {
        GameObject panel = CreatePanel("AircraftPanel", transform);
        panel.SetActive(false);
        
        VerticalLayoutGroup mainLayout = panel.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = spacing;
        mainLayout.padding = padding;
        mainLayout.childAlignment = TextAnchor.UpperCenter;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = true;
        
        // Titre
        CreateSubtitle("SÉLECTION D'AVION", panel.transform);
        
        // Nom de l'avion
        CreateText("AircraftName", "Avion de Tourisme", panel.transform, 32, TextAnchor.MiddleCenter, 60f);
        
        // Description
        CreateText("AircraftDescription", "Description de l'avion...", panel.transform, normalTextSize, TextAnchor.UpperLeft, 200f);
        
        // Groupe de navigation horizontal
        GameObject navGroup = CreateHorizontalGroup("NavigationGroup", panel.transform, spacing);
        LayoutElement navLayout = navGroup.AddComponent<LayoutElement>();
        navLayout.preferredHeight = buttonHeight;
        
        CreateButton("ButtonPrevious", "◀ PRÉCÉDENT", navGroup.transform, buttonHeight, secondaryButtonColor);
        CreateButton("ButtonNext", "SUIVANT ▶", navGroup.transform, buttonHeight, secondaryButtonColor);
        
        // Bouton retour
        CreateButton("ButtonRetour", "← RETOUR", panel.transform, buttonHeight, secondaryButtonColor);
        
        Debug.Log("AircraftPanel créé avec succès!");
    }

    [ContextMenu("Build Quick Settings Panel")]
    public void BuildQuickSettingsPanel()
    {
        GameObject panel = CreatePanel("QuickSettingsPanel", transform);
        panel.SetActive(false);
        
        VerticalLayoutGroup mainLayout = panel.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = spacing;
        mainLayout.padding = padding;
        mainLayout.childAlignment = TextAnchor.UpperCenter;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = true;
        
        // Titre
        CreateSubtitle("PARAMÈTRES RAPIDES", panel.transform);
        
        // Groupe Météo
        GameObject weatherGroup = CreateVerticalGroup("WeatherGroup", panel.transform, 10f);
        LayoutElement weatherLayout = weatherGroup.AddComponent<LayoutElement>();
        weatherLayout.preferredHeight = 100f;
        
        CreateText("WeatherText", "Météo: Beau", weatherGroup.transform, normalTextSize, TextAnchor.MiddleLeft, 30f);
        CreateSlider("WeatherSlider", weatherGroup.transform, 0f, 1f, 0.3f);
        
        // Groupe Heure
        GameObject timeGroup = CreateVerticalGroup("TimeGroup", panel.transform, 10f);
        LayoutElement timeLayout = timeGroup.AddComponent<LayoutElement>();
        timeLayout.preferredHeight = 100f;
        
        CreateText("TimeText", "Heure: 12:00", timeGroup.transform, normalTextSize, TextAnchor.MiddleLeft, 30f);
        CreateSlider("TimeSlider", timeGroup.transform, 0f, 24f, 12f);
        
        // Bouton retour
        CreateButton("ButtonRetour", "← RETOUR", panel.transform, buttonHeight, secondaryButtonColor);
        
        Debug.Log("QuickSettingsPanel créé avec succès!");
    }

    [ContextMenu("Build Tutorial Panel")]
    public void BuildTutorialPanel()
    {
        GameObject panel = CreatePanel("TutorialPanel", transform);
        panel.SetActive(false);
        
        VerticalLayoutGroup mainLayout = panel.AddComponent<VerticalLayoutGroup>();
        mainLayout.spacing = spacing;
        mainLayout.padding = padding;
        mainLayout.childAlignment = TextAnchor.UpperCenter;
        mainLayout.childControlWidth = true;
        mainLayout.childControlHeight = false;
        mainLayout.childForceExpandWidth = true;
        
        // Titre
        CreateSubtitle("DIDACTICIEL", panel.transform);
        
        // Texte du tutoriel
        CreateText("TutorialText", "Contenu du didacticiel...", panel.transform, normalTextSize, TextAnchor.UpperLeft, 400f);
        
        // Groupe de navigation horizontal
        GameObject navGroup = CreateHorizontalGroup("NavigationGroup", panel.transform, spacing);
        LayoutElement navLayout = navGroup.AddComponent<LayoutElement>();
        navLayout.preferredHeight = buttonHeight;
        
        CreateButton("ButtonPrevPage", "◀ PRÉCÉDENT", navGroup.transform, buttonHeight, secondaryButtonColor);
        CreateButton("ButtonNextPage", "SUIVANT ▶", navGroup.transform, buttonHeight, secondaryButtonColor);
        
        // Bouton retour
        CreateButton("ButtonRetour", "← RETOUR", panel.transform, buttonHeight, secondaryButtonColor);
        
        Debug.Log("TutorialPanel créé avec succès!");
    }

    #region Helpers de Création

    GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        Image image = panel.AddComponent<Image>();
        image.color = panelColor;
        
        return panel;
    }

    GameObject CreateVerticalGroup(string name, Transform parent, float groupSpacing)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        
        RectTransform rect = group.AddComponent<RectTransform>();
        
        VerticalLayoutGroup layout = group.AddComponent<VerticalLayoutGroup>();
        layout.spacing = groupSpacing;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        
        return group;
    }

    GameObject CreateHorizontalGroup(string name, Transform parent, float groupSpacing)
    {
        GameObject group = new GameObject(name);
        group.transform.SetParent(parent, false);
        
        RectTransform rect = group.AddComponent<RectTransform>();
        
        HorizontalLayoutGroup layout = group.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = groupSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;
        
        return group;
    }

    GameObject CreateButton(string name, string text, Transform parent, float height, Color color)
    {
        GameObject button = new GameObject(name);
        button.transform.SetParent(parent, false);
        
        RectTransform rect = button.AddComponent<RectTransform>();
        
        LayoutElement layoutElement = button.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
        layoutElement.minHeight = height;
        
        Image image = button.AddComponent<Image>();
        image.color = color;
        
        Button btn = button.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = color;
        colors.highlightedColor = color * 1.2f;
        colors.pressedColor = color * 0.8f;
        btn.colors = colors;
        
        // Texte du bouton
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(button.transform, false);
        
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        Text textComponent = textObj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        textComponent.fontSize = buttonFontSize;
        textComponent.color = textColor;
        textComponent.alignment = TextAnchor.MiddleCenter;
        textComponent.fontStyle = FontStyle.Bold;
        
        return button;
    }

    GameObject CreateText(string name, string content, Transform parent, int fontSize, TextAnchor alignment, float height)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rect = textObj.AddComponent<RectTransform>();
        
        LayoutElement layoutElement = textObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = height;
        
        Text text = textObj.AddComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = fontSize;
        text.color = textColor;
        text.alignment = alignment;
        
        return textObj;
    }

    GameObject CreateTitle(string content, Transform parent)
    {
        GameObject titleObj = CreateText("Title", content, parent, titleFontSize, TextAnchor.MiddleCenter, 80f);
        Text text = titleObj.GetComponent<Text>();
        text.fontStyle = FontStyle.Bold;
        return titleObj;
    }

    GameObject CreateSubtitle(string content, Transform parent)
    {
        GameObject subtitleObj = CreateText("Subtitle", content, parent, 36, TextAnchor.MiddleCenter, 60f);
        Text text = subtitleObj.GetComponent<Text>();
        text.fontStyle = FontStyle.Bold;
        return subtitleObj;
    }

    GameObject CreateSlider(string name, Transform parent, float minValue, float maxValue, float defaultValue)
    {
        GameObject sliderObj = new GameObject(name);
        sliderObj.transform.SetParent(parent, false);
        
        RectTransform rect = sliderObj.AddComponent<RectTransform>();
        
        LayoutElement layoutElement = sliderObj.AddComponent<LayoutElement>();
        layoutElement.preferredHeight = 30f;
        
        Slider slider = sliderObj.AddComponent<Slider>();
        slider.minValue = minValue;
        slider.maxValue = maxValue;
        slider.value = defaultValue;
        
        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(sliderObj.transform, false);
        RectTransform bgRect = background.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0, 0.25f);
        bgRect.anchorMax = new Vector2(1, 0.75f);
        bgRect.sizeDelta = Vector2.zero;
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f);
        
        // Fill Area
        GameObject fillArea = new GameObject("Fill Area");
        fillArea.transform.SetParent(sliderObj.transform, false);
        RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0.25f);
        fillAreaRect.anchorMax = new Vector2(1, 0.75f);
        fillAreaRect.sizeDelta = new Vector2(-20, 0);
        
        // Fill
        GameObject fill = new GameObject("Fill");
        fill.transform.SetParent(fillArea.transform, false);
        RectTransform fillRect = fill.AddComponent<RectTransform>();
        fillRect.sizeDelta = Vector2.zero;
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = primaryButtonColor;
        
        slider.fillRect = fillRect;
        
        // Handle Slide Area
        GameObject handleArea = new GameObject("Handle Slide Area");
        handleArea.transform.SetParent(sliderObj.transform, false);
        RectTransform handleAreaRect = handleArea.AddComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.sizeDelta = new Vector2(-20, 0);
        
        // Handle
        GameObject handle = new GameObject("Handle");
        handle.transform.SetParent(handleArea.transform, false);
        RectTransform handleRect = handle.AddComponent<RectTransform>();
        handleRect.sizeDelta = new Vector2(20, 20);
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = Color.white;
        
        slider.handleRect = handleRect;
        slider.targetGraphic = handleImage;
        
        return sliderObj;
    }

    #endregion

    [ContextMenu("Build All Panels")]
    public void BuildAllPanels()
    {
        BuildMainPanel();
        BuildAircraftSelectionPanel();
        BuildQuickSettingsPanel();
        BuildTutorialPanel();
        
        Debug.Log("Tous les panneaux ont été créés avec succès!");
    }
}
