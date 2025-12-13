using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Génère des chemins de ferme et routes agricoles avec un style organique
/// </summary>
public class FarmRoadGenerator : MonoBehaviour
{
    [Header("Références Terrain")]
    [Tooltip("Terrain sur lequel générer les chemins")]
    public Terrain terrain;
    
    [Header("Chemins de Ferme")]
    [Tooltip("Prefab de route de ferme")]
    public GameObject farmPathPrefab;
    
    [Tooltip("Largeur des routes de ferme (en mètres)")]
    public float farmPathWidth = 6f;
    
    [Header("Terrain Agricole")]
    [Tooltip("Prefabs de terrain agricole (vert/marron)")]
    public GameObject[] farmGroundPrefabs;
    
    [Tooltip("Taille d'un bloc de terrain (en mètres)")]
    public float blockSize = 80f;
    
    [Header("Zone de Génération")]
    [Tooltip("Centre de la zone de génération")]
    public Vector3 generationCenter = Vector3.zero;
    
    [Tooltip("Taille de la zone (rayon en mètres)")]
    public float generationRadius = 1000f;
    
    [Header("Grille de Ferme")]
    [Tooltip("Nombre de lignes de la grille")]
    [Range(3, 15)]
    public int gridRows = 6;
    
    [Tooltip("Nombre de colonnes de la grille")]
    [Range(3, 15)]
    public int gridColumns = 6;
    
    [Header("Style de Chemins")]
    [Tooltip("Longueur des segments de route")]
    public float segmentLength = 20f;
    
    [Header("Intersections")]
    [Tooltip("Prefab d'intersection de chemins")]
    public GameObject farmIntersectionPrefab;
    
    [Tooltip("Distance minimale pour créer une intersection")]
    public float intersectionDetectionDistance = 15f;
    
    [Header("Connexions")]
    [Tooltip("Points de connexion aux routes de campagne")]
    public List<Transform> countryRoadConnectionPoints = new List<Transform>();
    
    [Tooltip("Créer des connexions vers les routes de campagne")]
    public bool connectToCountryRoads = true;
    
    [Header("Terrain Following")]
    [Tooltip("Suivre le terrain")]
    public bool followTerrain = true;
    
    [Tooltip("Hauteur au-dessus du terrain (en mètres)")]
    public float heightAboveTerrain = 0.1f;
    
    [Header("Optimisation")]
    [Tooltip("Parent pour organiser les objets générés")]
    public Transform farmRoadsParent;
    
    [Tooltip("Graine aléatoire (0 = aléatoire)")]
    public int seed = 0;
    
    // Données internes
    private List<GameObject> generatedObjects = new List<GameObject>();
    private List<Vector3> pathPoints = new List<Vector3>();
    private List<Vector3> intersectionPoints = new List<Vector3>();
    private System.Random random;

    void Start()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;
    }

    /// <summary>
    /// Génère la grille de ferme avec routes et blocs agricoles
    /// </summary>
    [ContextMenu("Generate Farm Roads")]
    public void GenerateFarmRoads()
    {
        Debug.Log("FarmRoadGenerator: Début de la génération de la grille de ferme...");
        
        // Initialiser le générateur aléatoire
        if (seed == 0)
            seed = Random.Range(1, 999999);
        
        random = new System.Random(seed);
        Debug.Log($"FarmRoadGenerator: Seed = {seed}");
        
        // Nettoyer les chemins existants
        ClearFarmRoads();
        
        // Créer le parent si nécessaire
        if (farmRoadsParent == null)
        {
            GameObject parentObj = new GameObject("FarmRoads");
            farmRoadsParent = parentObj.transform;
        }
        
        // Générer la grille de routes et blocs agricoles
        GenerateFarmGrid();
        
        // Connecter aux routes de campagne si demandé
        if (connectToCountryRoads && countryRoadConnectionPoints.Count > 0)
        {
            ConnectToCountryRoads();
        }
        
        Debug.Log($"FarmRoadGenerator: Génération terminée! {generatedObjects.Count} objets créés.");
    }

    /// <summary>
    /// Génère la grille de ferme complète (routes + blocs agricoles)
    /// </summary>
    void GenerateFarmGrid()
    {
        float totalBlockSize = blockSize + farmPathWidth;
        
        // Calculer le décalage pour centrer la grille
        float gridWidth = gridColumns * totalBlockSize;
        float gridHeight = gridRows * totalBlockSize;
        Vector3 gridOffset = new Vector3(-gridWidth / 2f, 0f, -gridHeight / 2f);
        
        // Générer les blocs agricoles
        Transform blocksParent = new GameObject("FarmBlocks").transform;
        blocksParent.SetParent(farmRoadsParent);
        
        for (int row = 0; row < gridRows; row++)
        {
            for (int col = 0; col < gridColumns; col++)
            {
                Vector3 blockPosition = generationCenter + gridOffset + new Vector3(
                    col * totalBlockSize + blockSize / 2f,
                    0f,
                    row * totalBlockSize + blockSize / 2f
                );
                
                CreateFarmBlock(blockPosition, row, col, blocksParent);
            }
        }
        
        // Générer les routes horizontales
        Transform roadsParent = new GameObject("FarmRoads").transform;
        roadsParent.SetParent(farmRoadsParent);
        
        for (int row = 0; row <= gridRows; row++)
        {
            Vector3 startPos = generationCenter + gridOffset + new Vector3(0f, 0f, row * totalBlockSize);
            Vector3 endPos = startPos + new Vector3(gridWidth, 0f, 0f);
            GenerateStraightRoad(startPos, endPos, roadsParent);
        }
        
        // Générer les routes verticales
        for (int col = 0; col <= gridColumns; col++)
        {
            Vector3 startPos = generationCenter + gridOffset + new Vector3(col * totalBlockSize, 0f, 0f);
            Vector3 endPos = startPos + new Vector3(0f, 0f, gridHeight);
            GenerateStraightRoad(startPos, endPos, roadsParent);
        }
        
        // Générer les intersections
        Transform intersectionsParent = new GameObject("Intersections").transform;
        intersectionsParent.SetParent(farmRoadsParent);
        
        for (int row = 0; row <= gridRows; row++)
        {
            for (int col = 0; col <= gridColumns; col++)
            {
                Vector3 intersectionPos = generationCenter + gridOffset + new Vector3(
                    col * totalBlockSize,
                    0f,
                    row * totalBlockSize
                );
                
                CreateIntersection(intersectionPos, intersectionsParent);
            }
        }
    }

    /// <summary>
    /// Crée un bloc de terrain agricole
    /// </summary>
    void CreateFarmBlock(Vector3 center, int row, int col, Transform parent)
    {
        if (farmGroundPrefabs == null || farmGroundPrefabs.Length == 0)
        {
            Debug.LogWarning("FarmRoadGenerator: Aucun prefab de terrain agricole assigné!");
            return;
        }
        
        // Ajuster la hauteur au terrain
        if (terrain != null && followTerrain)
        {
            center.y = terrain.SampleHeight(center) + heightAboveTerrain;
        }
        
        // Sélectionner un prefab aléatoire
        GameObject prefab = farmGroundPrefabs[random.Next(farmGroundPrefabs.Length)];
        
        // Créer le bloc
        GameObject block = Instantiate(prefab, center, Quaternion.identity);
        block.transform.SetParent(parent);
        block.name = $"FarmBlock_{row}_{col}";
        
        // Ajuster l'échelle pour couvrir l'espace (légère extension pour éviter les trous)
        float targetSize = blockSize + farmPathWidth * 0.15f;
        float scale = targetSize / 5f; // Le prefab fait 5m de base
        block.transform.localScale = new Vector3(scale, 1f, scale);
        
        // Rotation aléatoire pour varier l'apparence
        float rotation = random.Next(4) * 90f;
        block.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        
        generatedObjects.Add(block);
    }
    
    /// <summary>
    /// Génère une route droite entre deux points
    /// </summary>
    void GenerateStraightRoad(Vector3 start, Vector3 end, Transform parent)
    {
        if (farmPathPrefab == null)
        {
            Debug.LogWarning("FarmRoadGenerator: Aucun prefab de route assigné!");
            return;
        }
        
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        int segmentCount = Mathf.CeilToInt(distance / segmentLength);
        
        // Calculer la rotation une seule fois (ajout de 90° pour aligner le prefab)
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + 90f;
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        
        for (int i = 0; i < segmentCount; i++)
        {
            // Positionner le segment le long de la ligne
            Vector3 position = start + direction * (i * segmentLength);
            
            // Ajuster la hauteur au terrain
            if (terrain != null && followTerrain)
            {
                position.y = terrain.SampleHeight(position) + heightAboveTerrain;
            }
            
            // Créer le segment
            GameObject segment = Instantiate(farmPathPrefab, position, rotation);
            segment.transform.SetParent(parent);
            
            generatedObjects.Add(segment);
            pathPoints.Add(position);
        }
    }
    
    /// <summary>
    /// Crée une intersection
    /// </summary>
    void CreateIntersection(Vector3 position, Transform parent)
    {
        if (farmIntersectionPrefab == null) return;
        
        // Ajuster la hauteur au terrain
        if (terrain != null && followTerrain)
        {
            position.y = terrain.SampleHeight(position) + heightAboveTerrain;
        }
        
        GameObject intersection = Instantiate(farmIntersectionPrefab, position, Quaternion.identity);
        intersection.transform.SetParent(parent);
        
        generatedObjects.Add(intersection);
        intersectionPoints.Add(position);
    }

    /// <summary>
    /// Génère un chemin courbé segment par segment
    /// </summary>
    void GenerateCurvedPath(Vector3 startPoint, Vector3 initialDirection, float totalLength, float width, GameObject pathPrefab, Transform parent, float curvature = 0.3f)
    {
        if (pathPrefab == null) return;
        
        Vector3 currentPoint = startPoint;
        Vector3 currentDirection = initialDirection.normalized;
        float distanceCovered = 0f;
        
        List<Vector3> pathSegmentPoints = new List<Vector3>();
        pathSegmentPoints.Add(currentPoint);
        
        while (distanceCovered < totalLength)
        {
            // Variation de direction (sinuosité)
            float angleVariation = ((float)random.NextDouble() - 0.5f) * curvature * 60f;
            currentDirection = Quaternion.Euler(0f, angleVariation, 0f) * currentDirection;
            
            // Prochain point
            Vector3 nextPoint = currentPoint + currentDirection * segmentLength;
            
            // Ajuster la hauteur selon le terrain
            if (followTerrain && terrain != null)
            {
                nextPoint.y = terrain.SampleHeight(nextPoint) + heightAboveTerrain;
            }
            
            // Vérifier si on sort de la zone de génération
            if (Vector3.Distance(nextPoint, generationCenter) > generationRadius)
            {
                break;
            }
            
            pathSegmentPoints.Add(nextPoint);
            pathPoints.Add(nextPoint);
            
            // Créer le segment de chemin
            CreatePathSegment(currentPoint, nextPoint, width, pathPrefab, parent);
            
            currentPoint = nextPoint;
            distanceCovered += segmentLength;
        }
    }

    /// <summary>
    /// Crée un segment de chemin entre deux points
    /// </summary>
    void CreatePathSegment(Vector3 start, Vector3 end, float width, GameObject pathPrefab, Transform parent)
    {
        Vector3 midPoint = (start + end) / 2f;
        Vector3 direction = (end - start).normalized;
        float distance = Vector3.Distance(start, end);
        
        // Calculer la rotation
        float angle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
        
        // Créer le segment
        GameObject segment = Instantiate(pathPrefab, midPoint, rotation);
        segment.transform.SetParent(parent);
        
        // Ajuster l'échelle
        Vector3 scale = segment.transform.localScale;
        scale.x = width / 10f; // Assumer que le prefab fait 10m de base
        scale.z = distance / 10f;
        segment.transform.localScale = scale;
        
        generatedObjects.Add(segment);
    }

    /// <summary>
    /// Connecte les chemins de ferme aux routes de campagne
    /// </summary>
    void ConnectToCountryRoads()
    {
        Transform connectionsParent = new GameObject("CountryRoadConnections").transform;
        connectionsParent.SetParent(farmRoadsParent);
        
        foreach (Transform connectionPoint in countryRoadConnectionPoints)
        {
            if (connectionPoint == null) continue;
            
            // Trouver le point de chemin de ferme le plus proche
            Vector3 closestFarmPoint = FindClosestPathPoint(connectionPoint.position);
            
            if (closestFarmPoint != Vector3.zero)
            {
                // Créer une connexion directe
                GenerateConnectionPath(connectionPoint.position, closestFarmPoint, connectionsParent);
            }
        }
    }

    /// <summary>
    /// Génère un chemin de connexion direct entre deux points
    /// </summary>
    void GenerateConnectionPath(Vector3 start, Vector3 end, Transform parent)
    {
        Vector3 direction = (end - start).normalized;
        float totalDistance = Vector3.Distance(start, end);
        int segmentCount = Mathf.CeilToInt(totalDistance / segmentLength);
        
        Vector3 currentPoint = start;
        
        for (int i = 0; i < segmentCount; i++)
        {
            float segmentDist = Mathf.Min(segmentLength, totalDistance - i * segmentLength);
            Vector3 nextPoint = currentPoint + direction * segmentDist;
            
            // Ajuster la hauteur
            if (followTerrain && terrain != null)
            {
                nextPoint.y = terrain.SampleHeight(nextPoint) + heightAboveTerrain;
            }
            
            CreatePathSegment(currentPoint, nextPoint, farmPathWidth, farmPathPrefab, parent);
            pathPoints.Add(nextPoint);
            
            currentPoint = nextPoint;
        }
    }

    /// <summary>
    /// Trouve le point de chemin le plus proche d'une position donnée
    /// </summary>
    Vector3 FindClosestPathPoint(Vector3 position)
    {
        if (pathPoints.Count == 0) return Vector3.zero;
        
        Vector3 closest = pathPoints[0];
        float minDistance = Vector3.Distance(position, closest);
        
        foreach (Vector3 point in pathPoints)
        {
            float distance = Vector3.Distance(position, point);
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = point;
            }
        }
        
        return closest;
    }

    /// <summary>
    /// Place les intersections là où les chemins se croisent
    /// </summary>
    void PlaceIntersections()
    {
        if (farmIntersectionPrefab == null) return;
        
        Transform intersectionsParent = new GameObject("Intersections").transform;
        intersectionsParent.SetParent(farmRoadsParent);
        
        // Détecter les points proches (potentielles intersections)
        for (int i = 0; i < pathPoints.Count; i++)
        {
            for (int j = i + 1; j < pathPoints.Count; j++)
            {
                float distance = Vector3.Distance(pathPoints[i], pathPoints[j]);
                
                if (distance < intersectionDetectionDistance && distance > 1f)
                {
                    // Point d'intersection trouvé
                    Vector3 intersectionPos = (pathPoints[i] + pathPoints[j]) / 2f;
                    
                    // Vérifier qu'on n'a pas déjà une intersection proche
                    bool tooClose = false;
                    foreach (Vector3 existingIntersection in intersectionPoints)
                    {
                        if (Vector3.Distance(intersectionPos, existingIntersection) < intersectionDetectionDistance)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                    
                    if (!tooClose)
                    {
                        CreateIntersection(intersectionPos, intersectionsParent);
                        intersectionPoints.Add(intersectionPos);
                    }
                }
            }
        }
        
        Debug.Log($"FarmRoadGenerator: {intersectionPoints.Count} intersections créées");
    }

    /// <summary>
    /// Obtient un point aléatoire dans la zone de génération
    /// </summary>
    Vector3 GetRandomPointInGenerationZone()
    {
        float angle = (float)random.NextDouble() * 360f;
        float distance = (float)random.NextDouble() * generationRadius;
        
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
        Vector3 point = generationCenter + offset;
        
        // Ajuster la hauteur
        if (followTerrain && terrain != null)
        {
            point.y = terrain.SampleHeight(point) + heightAboveTerrain;
        }
        
        return point;
    }

    /// <summary>
    /// Nettoie tous les chemins générés
    /// </summary>
    [ContextMenu("Clear Farm Roads")]
    public void ClearFarmRoads()
    {
        foreach (GameObject obj in generatedObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        
        generatedObjects.Clear();
        pathPoints.Clear();
        intersectionPoints.Clear();
        
        if (farmRoadsParent != null && farmRoadsParent.childCount > 0)
        {
            for (int i = farmRoadsParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(farmRoadsParent.GetChild(i).gameObject);
            }
        }
        
        Debug.Log("FarmRoadGenerator: Chemins de ferme nettoyés");
    }

    /// <summary>
    /// Affiche la zone de génération en mode édition
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Zone de génération
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(generationCenter, generationRadius);
        
        // Points de connexion aux routes de campagne
        if (connectToCountryRoads && countryRoadConnectionPoints.Count > 0)
        {
            Gizmos.color = Color.yellow;
            foreach (Transform connectionPoint in countryRoadConnectionPoints)
            {
                if (connectionPoint != null)
                {
                    Gizmos.DrawWireSphere(connectionPoint.position, 20f);
                }
            }
        }
        
        // Points de chemin générés
        Gizmos.color = Color.cyan;
        foreach (Vector3 point in pathPoints)
        {
            Gizmos.DrawSphere(point, 2f);
        }
        
        // Intersections
        Gizmos.color = Color.magenta;
        foreach (Vector3 intersection in intersectionPoints)
        {
            Gizmos.DrawWireSphere(intersection, 5f);
        }
    }
}
