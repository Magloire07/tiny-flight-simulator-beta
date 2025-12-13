using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// Affiche une carte UI montrant les aéroports dans un périmètre donné
/// </summary>
public class AirportMapUI : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Transform de l'avion (joueur)")]
    public Transform playerPlane;
    
    [Tooltip("Aéroport de référence (point de départ)")]
    public Transform referenceAirport;
    
    [Tooltip("ProceduralWorldManager pour accéder aux aéroports générés")]
    public ProceduralWorldManager worldManager;
    
    [Header("Configuration Carte")]
    [Tooltip("Périmètre de détection des aéroports (en mètres)")]
    public float detectionRadius = 10000f;
    
    [Tooltip("Échelle de la carte (pixels par mètre) - ajustez pour zoomer/dézoomer")]
    public float mapScale = 0.01f;
    
    [Tooltip("Masquer les marqueurs hors limites (sinon ils sont clampés aux bords)")]
    public bool hideMarkersOutsideBounds = true;
    
    [Header("UI Elements")]
    [Tooltip("Panel contenant la carte")]
    public RectTransform mapPanel;
    
    [Tooltip("Image de fond de la carte")]
    public Image mapBackground;
    
    [Tooltip("Prefab du marqueur d'aéroport")]
    public GameObject airportMarkerPrefab;
    
    [Tooltip("Prefab du marqueur joueur")]
    public GameObject playerMarkerPrefab;
    
    [Tooltip("Couleur de fond de la carte")]
    public Color mapBackgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    
    [Header("Marqueurs")]
    [Tooltip("Sprite/Texture pour marqueur aéroport")]
    public Sprite airportMarkerSprite;
    
    [Tooltip("Sprite/Texture pour marqueur aéroport de référence")]
    public Sprite referenceAirportSprite;
    
    [Tooltip("Sprite/Texture pour marqueur joueur")]
    public Sprite playerMarkerSprite;
    
    [Tooltip("Couleur marqueur aéroport (si pas de sprite)")]
    public Color airportMarkerColor = Color.yellow;
    
    [Tooltip("Couleur marqueur aéroport de référence (si pas de sprite)")]
    public Color referenceAirportColor = Color.green;
    
    [Tooltip("Couleur marqueur joueur (si pas de sprite)")]
    public Color playerMarkerColor = Color.cyan;
    
    [Tooltip("Taille des marqueurs")]
    public float markerSize = 30f;
    
    [Header("Texte")]
    [Tooltip("Prefab de texte pour afficher le nombre d'aéroports")]
    public Text airportCountText;
    
    [Tooltip("Prefab de texte pour afficher la distance")]
    public Text distanceText;
    
    [Header("Optimisation")]
    [Tooltip("Intervalle de mise à jour (secondes)")]
    public float updateInterval = 1f;
    
    [Tooltip("Afficher la carte par défaut")]
    public bool showMapOnStart = true;
    
    [Tooltip("Touche pour afficher/masquer la carte")]
    public KeyCode toggleMapKey = KeyCode.M;
    
    // Données internes
    private GameObject playerMarker;
    private Dictionary<GameObject, GameObject> airportMarkers = new Dictionary<GameObject, GameObject>();
    private float nextUpdateTime;
    private bool mapVisible = true;
    private Canvas mapCanvas;
    private Vector2 mapBounds; // Taille réelle de la carte depuis le background
    
    void Start()
    {
        // Trouver l'avion si non assigné
        if (playerPlane == null)
        {
            GameObject plane = GameObject.FindGameObjectWithTag("Player");
            if (plane != null)
                playerPlane = plane.transform;
        }
        
        // Trouver le WorldManager si non assigné
        if (worldManager == null)
        {
            worldManager = FindObjectOfType<ProceduralWorldManager>();
        }
        
        // Créer la carte UI si nécessaire
        if (mapPanel == null)
        {
            CreateMapUI();
        }
        
        // Récupérer la taille de la carte depuis le background
        UpdateMapBounds();
        
        // Créer le marqueur joueur
        CreatePlayerMarker();
        
        // Configurer la visibilité initiale
        mapVisible = showMapOnStart;
        mapPanel.gameObject.SetActive(mapVisible);
        
        Debug.Log($"AirportMapUI: Carte initialisée ({mapBounds.x}x{mapBounds.y} pixels)");
    }
    
    void Update()
    {
        // Toggle visibilité carte
        if (Input.GetKeyDown(toggleMapKey))
        {
            ToggleMap();
        }
        
        if (!mapVisible || playerPlane == null) return;
        
        // Mise à jour périodique
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            UpdateMap();
        }
    }
    
    /// <summary>
    /// Crée l'interface de la carte
    /// </summary>
    void CreateMapUI()
    {
        // Créer un Canvas si nécessaire
        mapCanvas = GetComponent<Canvas>();
        if (mapCanvas == null)
        {
            GameObject canvasObj = new GameObject("AirportMapCanvas");
            canvasObj.transform.SetParent(transform);
            mapCanvas = canvasObj.AddComponent<Canvas>();
            mapCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mapCanvas.sortingOrder = 100;
            
            CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasObj.AddComponent<GraphicRaycaster>();
        }
        
        // Créer le panel de la carte
        GameObject panelObj = new GameObject("MapPanel");
        panelObj.transform.SetParent(mapCanvas.transform, false);
        mapPanel = panelObj.AddComponent<RectTransform>();
        mapPanel.anchorMin = new Vector2(1, 0);
        mapPanel.anchorMax = new Vector2(1, 0);
        mapPanel.pivot = new Vector2(1, 0);
        mapPanel.anchoredPosition = new Vector2(-20, 20);
        mapPanel.sizeDelta = new Vector2(300f, 300f); // Taille par défaut, sera mise à jour par UpdateMapBounds()
        
        // Ajouter l'image de fond
        mapBackground = panelObj.AddComponent<Image>();
        mapBackground.color = mapBackgroundColor;
        
        // Ajouter un texte pour le compteur d'aéroports
        GameObject textObj = new GameObject("AirportCountText");
        textObj.transform.SetParent(mapPanel, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 1);
        textRect.anchoredPosition = new Vector2(0, -5);
        textRect.sizeDelta = new Vector2(-10, 30);
        
        airportCountText = textObj.AddComponent<Text>();
        airportCountText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        airportCountText.fontSize = 14;
        airportCountText.color = Color.white;
        airportCountText.alignment = TextAnchor.UpperCenter;
        airportCountText.text = "Aéroports: 0";
        
        Debug.Log("AirportMapUI: Interface créée");
    }
    
    /// <summary>
    /// Met à jour les limites de la carte depuis le background
    /// </summary>
    void UpdateMapBounds()
    {
        if (mapBackground != null)
        {
            RectTransform bgRect = mapBackground.GetComponent<RectTransform>();
            if (bgRect != null)
            {
                mapBounds = bgRect.sizeDelta;
            }
        }
        
        // Valeur par défaut si rien n'est trouvé
        if (mapBounds == Vector2.zero)
        {
            mapBounds = new Vector2(300f, 300f);
        }
    }
    
    /// <summary>
    /// Crée le marqueur pour le joueur
    /// </summary>
    void CreatePlayerMarker()
    {
        if (mapBackground == null) return;
        
        GameObject markerObj = new GameObject("PlayerMarker");
        markerObj.transform.SetParent(mapBackground.transform, false);
        
        RectTransform rect = markerObj.AddComponent<RectTransform>();
        // Ancrer au centre du mapPanel
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(markerSize * 1.2f, markerSize * 1.2f);
        rect.anchoredPosition = Vector2.zero; // Position au centre du background
        rect.localPosition = new Vector3(0, 0, 0); // Z = 0 pour être dans le même plan que le background
        
        Image img = markerObj.AddComponent<Image>();
        
        // Utiliser le sprite s'il est fourni, sinon couleur unie
        if (playerMarkerSprite != null)
        {
            img.sprite = playerMarkerSprite;
            img.color = Color.white; // Teinte blanche pour afficher le sprite normalement
        }
        else
        {
            img.color = playerMarkerColor;
        }
        
        markerObj.SetActive(true);
        playerMarker = markerObj;
        
        Debug.Log($"AirportMapUI: Marqueur joueur créé au centre, taille={markerSize * 1.2f}, couleur={img.color}");
    }
    
    /// <summary>
    /// Crée un marqueur pour un aéroport
    /// </summary>
    GameObject CreateAirportMarker(bool isReference = false)
    {
        if (mapBackground == null) return null;
        
        GameObject markerObj = new GameObject(isReference ? "ReferenceAirportMarker" : "AirportMarker");
        markerObj.transform.SetParent(mapBackground.transform, false);
        
        RectTransform rect = markerObj.AddComponent<RectTransform>();
        // Ancrer au centre du mapPanel
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(markerSize, markerSize);
        rect.localPosition = new Vector3(0, 0, 0); // Z = 0 pour être dans le même plan que le background
        
        Image img = markerObj.AddComponent<Image>();
        
        // Utiliser le sprite approprié s'il est fourni, sinon couleur unie
        if (isReference && referenceAirportSprite != null)
        {
            img.sprite = referenceAirportSprite;
            img.color = Color.white;
        }
        else if (!isReference && airportMarkerSprite != null)
        {
            img.sprite = airportMarkerSprite;
            img.color = Color.white;
        }
        else
        {
            img.color = isReference ? referenceAirportColor : airportMarkerColor;
        }
        
        markerObj.SetActive(true);
        Debug.Log($"AirportMapUI: Marqueur aéroport créé, référence={isReference}, couleur={img.color}");
        
        return markerObj;
    }
    
    /// <summary>
    /// Met à jour la carte avec les positions actuelles
    /// </summary>
    void UpdateMap()
    {
        if (playerPlane == null || mapPanel == null) return;
        
        List<Transform> nearbyAirports = new List<Transform>();
        
        // Ajouter l'aéroport de référence s'il existe
        if (referenceAirport != null)
        {
            float distance = Vector3.Distance(playerPlane.position, referenceAirport.position);
            nearbyAirports.Add(referenceAirport); // Toujours afficher l'aéroport de référence
            Debug.Log($"AirportMapUI: Aéroport de référence à {distance:F0}m");
        }
        
        // Chercher tous les aéroports générés dans la scène
        // Méthode 1: Par nom d'objet
        GameObject[] allObjects = GameObject.FindObjectsOfType<GameObject>(true);
        int airportsFound = 0;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.Contains("Airport") || obj.name.Contains("Aeroport") || obj.name == "Airport")
            {
                if (obj == referenceAirport?.gameObject) continue; // Éviter les doublons
                
                float distance = Vector3.Distance(playerPlane.position, obj.transform.position);
                if (distance <= detectionRadius)
                {
                    nearbyAirports.Add(obj.transform);
                    airportsFound++;
                }
            }
        }
        
        Debug.Log($"AirportMapUI: {airportsFound} aéroports trouvés dans un rayon de {detectionRadius/1000f:F1}km");
        
        // Supprimer les marqueurs obsolètes
        List<GameObject> markersToRemove = new List<GameObject>();
        foreach (var kvp in airportMarkers)
        {
            if (kvp.Key == null || !nearbyAirports.Contains(kvp.Key.transform))
            {
                Destroy(kvp.Value);
                markersToRemove.Add(kvp.Key);
            }
        }
        foreach (GameObject key in markersToRemove)
        {
            airportMarkers.Remove(key);
        }
        
        // Créer ou mettre à jour les marqueurs
        foreach (Transform airport in nearbyAirports)
        {
            GameObject marker;
            bool isReference = (airport == referenceAirport);
            
            if (!airportMarkers.ContainsKey(airport.gameObject))
            {
                marker = CreateAirportMarker(isReference);
                airportMarkers[airport.gameObject] = marker;
            }
            else
            {
                marker = airportMarkers[airport.gameObject];
            }
            
            // Mettre à jour la position du marqueur
            UpdateMarkerPosition(marker, airport.position);
        }
        
        // Mettre à jour le marqueur du joueur (toujours au centre)
        if (playerMarker != null)
        {
            RectTransform playerRect = playerMarker.GetComponent<RectTransform>();
            if (playerRect != null)
            {
                playerRect.anchoredPosition = Vector2.zero; // Toujours au centre de la carte
                playerMarker.SetActive(true);
            }
        }
        
        // Mettre à jour le texte
        if (airportCountText != null)
        {
            airportCountText.text = $"Aéroports: {nearbyAirports.Count}\nRayon: {detectionRadius / 1000f:F1} km";
        }
    }
    
    /// <summary>
    /// Met à jour la position d'un marqueur sur la carte
    /// </summary>
    void UpdateMarkerPosition(GameObject marker, Vector3 worldPosition)
    {
        if (marker == null || playerPlane == null) return;
        
        // Calculer la position relative au joueur
        Vector3 relativePos = worldPosition - playerPlane.position;
        
        // Convertir en position 2D sur la carte (vue de dessus)
        float mapX = relativePos.x * mapScale;
        float mapZ = relativePos.z * mapScale;
        
        // Limites de la carte (avec marge pour le marqueur)
        float halfWidth = mapBounds.x / 2f;
        float halfHeight = mapBounds.y / 2f;
        float markerMargin = markerSize / 2f;
        
        // Vérifier si le marqueur est hors limites
        bool isOutOfBounds = (Mathf.Abs(mapX) > halfWidth - markerMargin || 
                              Mathf.Abs(mapZ) > halfHeight - markerMargin);
        
        // Appliquer la position
        RectTransform rect = marker.GetComponent<RectTransform>();
        if (rect != null)
        {
            if (hideMarkersOutsideBounds && isOutOfBounds)
            {
                // Masquer les marqueurs hors limites
                marker.SetActive(false);
            }
            else
            {
                // Afficher et clamper aux bords de la carte
                marker.SetActive(true);
                mapX = Mathf.Clamp(mapX, -halfWidth + markerMargin, halfWidth - markerMargin);
                mapZ = Mathf.Clamp(mapZ, -halfHeight + markerMargin, halfHeight - markerMargin);
                rect.anchoredPosition = new Vector2(mapX, mapZ);
            }
        }
    }
    
    /// <summary>
    /// Affiche/masque la carte
    /// </summary>
    public void ToggleMap()
    {
        mapVisible = !mapVisible;
        if (mapPanel != null)
        {
            mapPanel.gameObject.SetActive(mapVisible);
        }
        Debug.Log($"AirportMapUI: Carte {(mapVisible ? "affichée" : "masquée")}");
    }
    
    /// <summary>
    /// Définit le rayon de détection
    /// </summary>
    public void SetDetectionRadius(float radius)
    {
        detectionRadius = radius;
        UpdateMap();
    }
    
    /// <summary>
    /// Ajuste le zoom de la carte
    /// </summary>
    public void SetMapScale(float scale)
    {
        mapScale = scale;
    }
}
