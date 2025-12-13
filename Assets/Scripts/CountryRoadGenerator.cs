using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Générateur de routes de campagne connectant plusieurs villes ou zones
/// </summary>
public class CountryRoadGenerator : MonoBehaviour
{
    [Header("Routes de Campagne")]
    [Tooltip("Prefab de segment de route de campagne")]
    public GameObject countryRoadPrefab;
    
    [Tooltip("Prefab d'intersection de campagne")]
    public GameObject countryIntersectionPrefab;
    
    [Tooltip("Largeur des routes de campagne (en mètres)")]
    public float roadWidth = 8f;
    
    [Tooltip("Longueur d'un segment de route (en mètres)")]
    public float segmentLength = 50f;
    
    [Header("Réseau de Routes")]
    [Tooltip("Points de connexion (villes, zones d'intérêt)")]
    public List<Transform> connectionPoints = new List<Transform>();
    
    [Tooltip("Générer des routes principales automatiquement")]
    public bool generateMainRoads = true;
    
    [Tooltip("Espacement entre les routes principales (en mètres)")]
    public float mainRoadSpacing = 500f;
    
    [Tooltip("Nombre de routes principales horizontales")]
    public int horizontalMainRoads = 3;
    
    [Tooltip("Nombre de routes principales verticales")]
    public int verticalMainRoads = 3;
    
    [Header("Routes Secondaires")]
    [Tooltip("Générer des routes secondaires aléatoires")]
    public bool generateSecondaryRoads = true;
    
    [Tooltip("Nombre de routes secondaires")]
    public int secondaryRoadCount = 10;
    
    [Tooltip("Longueur minimum d'une route secondaire (en mètres)")]
    public float minSecondaryRoadLength = 200f;
    
    [Tooltip("Longueur maximum d'une route secondaire (en mètres)")]
    public float maxSecondaryRoadLength = 800f;
    
    [Header("Terrain")]
    [Tooltip("Suivre le terrain pour les routes")]
    public bool followTerrain = true;
    
    [Tooltip("Hauteur au-dessus du terrain")]
    public float heightAboveTerrain = 0.2f;
    
    [Tooltip("Référence au terrain Unity")]
    public Terrain terrain;
    
    [Header("Génération")]
    [Tooltip("Seed aléatoire (0 = aléatoire)")]
    public int seed = 0;
    
    [Tooltip("Parent pour l'organisation")]
    public Transform roadParent;
    
    // Variables internes
    private List<GameObject> generatedRoads = new List<GameObject>();
    private List<Vector3> intersectionPoints = new List<Vector3>();
    private System.Random random;

    void Start()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;
    }

    /// <summary>
    /// Génère le réseau de routes de campagne
    /// </summary>
    [ContextMenu("Generate Country Roads")]
    public void GenerateCountryRoads()
    {
        Debug.Log("CountryRoadGenerator: Début de la génération...");
        
        // Initialiser
        if (seed == 0)
            seed = Random.Range(1, 999999);
        random = new System.Random(seed);
        
        ClearRoads();
        
        if (roadParent == null)
        {
            GameObject parentObj = new GameObject("CountryRoads");
            roadParent = parentObj.transform;
        }
        
        // Générer les routes principales
        if (generateMainRoads)
        {
            GenerateMainRoadNetwork();
        }
        
        // Générer les routes secondaires
        if (generateSecondaryRoads)
        {
            GenerateSecondaryRoadNetwork();
        }
        
        // Connecter les points définis
        ConnectDefinedPoints();
        
        // Placer les intersections
        PlaceIntersections();
        
        Debug.Log($"CountryRoadGenerator: {generatedRoads.Count} segments générés, {intersectionPoints.Count} intersections");
    }

    /// <summary>
    /// Génère le réseau principal de routes (grille)
    /// </summary>
    void GenerateMainRoadNetwork()
    {
        Transform mainRoadsParent = new GameObject("MainRoads").transform;
        mainRoadsParent.SetParent(roadParent);
        
        float halfWidth = (horizontalMainRoads - 1) * mainRoadSpacing / 2f;
        float halfLength = (verticalMainRoads - 1) * mainRoadSpacing / 2f;
        
        // Routes horizontales
        for (int i = 0; i < horizontalMainRoads; i++)
        {
            float zPos = -halfLength + i * mainRoadSpacing;
            Vector3 start = new Vector3(-halfWidth, 0f, zPos);
            Vector3 end = new Vector3(halfWidth, 0f, zPos);
            
            GenerateRoadBetweenPoints(start, end, mainRoadsParent);
        }
        
        // Routes verticales
        for (int i = 0; i < verticalMainRoads; i++)
        {
            float xPos = -halfWidth + i * mainRoadSpacing;
            Vector3 start = new Vector3(xPos, 0f, -halfLength);
            Vector3 end = new Vector3(xPos, 0f, halfLength);
            
            GenerateRoadBetweenPoints(start, end, mainRoadsParent);
        }
        
        // Marquer les intersections
        for (int x = 0; x < verticalMainRoads; x++)
        {
            for (int z = 0; z < horizontalMainRoads; z++)
            {
                float xPos = -halfWidth + x * mainRoadSpacing;
                float zPos = -halfLength + z * mainRoadSpacing;
                Vector3 intersection = new Vector3(xPos, 0f, zPos);
                
                if (followTerrain && terrain != null)
                    intersection.y = terrain.SampleHeight(intersection) + heightAboveTerrain;
                
                intersectionPoints.Add(intersection);
            }
        }
    }

    /// <summary>
    /// Génère des routes secondaires aléatoires
    /// </summary>
    void GenerateSecondaryRoadNetwork()
    {
        Transform secondaryRoadsParent = new GameObject("SecondaryRoads").transform;
        secondaryRoadsParent.SetParent(roadParent);
        
        for (int i = 0; i < secondaryRoadCount; i++)
        {
            // Point de départ aléatoire (souvent près d'une route principale)
            Vector3 start = GetRandomPointNearMainRoad();
            
            // Direction aléatoire
            float angle = (float)random.NextDouble() * 360f;
            Vector3 direction = Quaternion.Euler(0f, angle, 0f) * Vector3.forward;
            
            // Longueur aléatoire
            float length = Mathf.Lerp(minSecondaryRoadLength, maxSecondaryRoadLength, (float)random.NextDouble());
            
            Vector3 end = start + direction * length;
            
            GenerateRoadBetweenPoints(start, end, secondaryRoadsParent);
        }
    }

    /// <summary>
    /// Génère une route entre deux points
    /// </summary>
    void GenerateRoadBetweenPoints(Vector3 start, Vector3 end, Transform parent)
    {
        if (countryRoadPrefab == null)
        {
            Debug.LogWarning("CountryRoadGenerator: Aucun prefab de route assigné!");
            return;
        }
        
        // Calculer la distance et le nombre de segments
        float distance = Vector3.Distance(start, end);
        int segmentCount = Mathf.CeilToInt(distance / segmentLength);
        
        Vector3 direction = (end - start).normalized;
        
        // Calculer la rotation une seule fois
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 90f;
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        
        for (int i = 0; i < segmentCount; i++)
        {
            // Positionner le segment le long de la ligne
            Vector3 position = start + direction * (i * segmentLength);
            
            // Ajuster la hauteur selon le terrain
            if (followTerrain && terrain != null)
            {
                position.y = terrain.SampleHeight(position) + heightAboveTerrain;
            }
            
            // Créer le segment
            GameObject roadSegment = Instantiate(countryRoadPrefab, position, rotation);
            roadSegment.transform.SetParent(parent);
            
            generatedRoads.Add(roadSegment);
        }
    }

    /// <summary>
    /// Connecte les points de connexion définis
    /// </summary>
    void ConnectDefinedPoints()
    {
        if (connectionPoints.Count < 2) return;
        
        Transform connectionsParent = new GameObject("Connections").transform;
        connectionsParent.SetParent(roadParent);
        
        // Connecter chaque point au plus proche
        for (int i = 0; i < connectionPoints.Count; i++)
        {
            if (connectionPoints[i] == null) continue;
            
            // Trouver le point le plus proche
            int closestIndex = -1;
            float closestDistance = float.MaxValue;
            
            for (int j = 0; j < connectionPoints.Count; j++)
            {
                if (i == j || connectionPoints[j] == null) continue;
                
                float distance = Vector3.Distance(connectionPoints[i].position, connectionPoints[j].position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestIndex = j;
                }
            }
            
            if (closestIndex != -1)
            {
                GenerateRoadBetweenPoints(
                    connectionPoints[i].position,
                    connectionPoints[closestIndex].position,
                    connectionsParent
                );
            }
        }
    }

    /// <summary>
    /// Place les intersections aux points d'intersection
    /// </summary>
    void PlaceIntersections()
    {
        if (countryIntersectionPrefab == null) return;
        
        Transform intersectionsParent = new GameObject("Intersections").transform;
        intersectionsParent.SetParent(roadParent);
        
        foreach (Vector3 point in intersectionPoints)
        {
            GameObject intersection = Instantiate(countryIntersectionPrefab, point, Quaternion.identity);
            intersection.transform.SetParent(intersectionsParent);
            
            // Ajuster l'échelle
            Vector3 scale = intersection.transform.localScale;
            scale.x = roadWidth / 10f;
            scale.z = roadWidth / 10f;
            intersection.transform.localScale = scale;
            
            generatedRoads.Add(intersection);
        }
    }

    /// <summary>
    /// Obtient un point aléatoire près d'une route principale
    /// </summary>
    Vector3 GetRandomPointNearMainRoad()
    {
        if (intersectionPoints.Count == 0)
        {
            // Point complètement aléatoire
            float x = ((float)random.NextDouble() - 0.5f) * mainRoadSpacing * horizontalMainRoads;
            float z = ((float)random.NextDouble() - 0.5f) * mainRoadSpacing * verticalMainRoads;
            return new Vector3(x, 0f, z);
        }
        
        // Prendre une intersection aléatoire et s'en écarter
        Vector3 basePoint = intersectionPoints[random.Next(intersectionPoints.Count)];
        float offsetDistance = 50f + (float)random.NextDouble() * 200f;
        float angle = (float)random.NextDouble() * 360f;
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * offsetDistance;
        
        return basePoint + offset;
    }

    /// <summary>
    /// Nettoie les routes générées
    /// </summary>
    [ContextMenu("Clear Roads")]
    public void ClearRoads()
    {
        foreach (GameObject road in generatedRoads)
        {
            if (road != null)
                DestroyImmediate(road);
        }
        
        generatedRoads.Clear();
        intersectionPoints.Clear();
        
        if (roadParent != null)
        {
            for (int i = roadParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(roadParent.GetChild(i).gameObject);
            }
        }
        
        Debug.Log("CountryRoadGenerator: Routes nettoyées");
    }

    /// <summary>
    /// Visualisation en mode édition
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Dessiner les points de connexion
        Gizmos.color = Color.cyan;
        foreach (Transform point in connectionPoints)
        {
            if (point != null)
            {
                Gizmos.DrawWireSphere(point.position, 20f);
            }
        }
        
        // Dessiner le réseau principal prévu
        if (generateMainRoads && !Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            float halfWidth = (horizontalMainRoads - 1) * mainRoadSpacing / 2f;
            float halfLength = (verticalMainRoads - 1) * mainRoadSpacing / 2f;
            
            // Lignes horizontales
            for (int i = 0; i < horizontalMainRoads; i++)
            {
                float z = -halfLength + i * mainRoadSpacing;
                Gizmos.DrawLine(
                    new Vector3(-halfWidth, 0f, z),
                    new Vector3(halfWidth, 0f, z)
                );
            }
            
            // Lignes verticales
            for (int i = 0; i < verticalMainRoads; i++)
            {
                float x = -halfWidth + i * mainRoadSpacing;
                Gizmos.DrawLine(
                    new Vector3(x, 0f, -halfLength),
                    new Vector3(x, 0f, halfLength)
                );
            }
        }
    }
}
