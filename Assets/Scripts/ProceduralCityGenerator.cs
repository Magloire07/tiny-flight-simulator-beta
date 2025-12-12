using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Générateur procédural de ville avec grille et placement de bâtiments
/// </summary>
public class ProceduralCityGenerator : MonoBehaviour
{
    [Header("Dimensions de la Ville")]
    [Tooltip("Nombre de blocs en largeur (axe X)")]
    public int cityWidth = 10;
    
    [Tooltip("Nombre de blocs en longueur (axe Z)")]
    public int cityLength = 10;
    
    [Tooltip("Taille d'un bloc de ville (en mètres)")]
    public float blockSize = 100f;
    
    [Tooltip("Largeur des rues (en mètres)")]
    public float streetWidth = 10f;
    
    [Header("Bâtiments - Assets")]
    [Tooltip("Prefabs de bâtiments disponibles")]
    public GameObject[] buildingPrefabs;
    
    [Tooltip("Prefabs de petits bâtiments (maisons, magasins)")]
    public GameObject[] smallBuildingPrefabs;
    
    [Tooltip("Prefabs de bâtiments moyens")]
    public GameObject[] mediumBuildingPrefabs;
    
    [Tooltip("Prefabs de gratte-ciels")]
    public GameObject[] skyscraperPrefabs;
    
    [Header("Densité")]
    [Tooltip("Probabilité qu'un emplacement ait un bâtiment (0-1)")]
    [Range(0f, 1f)]
    public float buildingDensity = 0.7f;
    
    [Tooltip("Probabilité de gratte-ciel au centre (0-1)")]
    [Range(0f, 1f)]
    public float skyscraperProbabilityCenter = 0.8f;
    
    [Tooltip("Probabilité de gratte-ciel en périphérie (0-1)")]
    [Range(0f, 1f)]
    public float skyscraperProbabilityEdge = 0.1f;
    
    [Header("Routes")]
    [Tooltip("Prefab de segment de route")]
    public GameObject roadPrefab;
    
    [Tooltip("Prefab d'intersection")]
    public GameObject intersectionPrefab;
    
    [Tooltip("Hauteur des routes (Y)")]
    public float roadHeight = 0.1f;
    
    [Header("Espaces Verts")]
    [Tooltip("Prefabs de parcs")]
    public GameObject[] parkPrefabs;
    
    [Tooltip("Probabilité d'avoir un parc au lieu d'un bâtiment (0-1)")]
    [Range(0f, 1f)]
    public float parkProbability = 0.05f;
    
    [Header("Génération")]
    [Tooltip("Graine aléatoire pour la génération (0 = aléatoire)")]
    public int seed = 0;
    
    [Tooltip("Générer automatiquement au démarrage")]
    public bool generateOnStart = true;
    
    [Tooltip("Parent pour organiser la hiérarchie")]
    public Transform cityParent;
    
    [Header("Optimisation")]
    [Tooltip("Combiner les meshes pour optimiser les performances")]
    public bool combineMeshes = false;
    
    [Tooltip("Distance de culling des bâtiments (0 = désactivé)")]
    public float cullingDistance = 2000f;
    
    // Structure de données
    private GridCell[,] cityGrid;
    private List<GameObject> generatedObjects = new List<GameObject>();
    private System.Random random;

    void Start()
    {
        if (generateOnStart)
        {
            GenerateCity();
        }
    }

    /// <summary>
    /// Génère la ville complète
    /// </summary>
    [ContextMenu("Generate City")]
    public void GenerateCity()
    {
        Debug.Log("ProceduralCityGenerator: Début de la génération...");
        
        // Initialiser le générateur aléatoire
        if (seed == 0)
            seed = Random.Range(1, 999999);
        
        random = new System.Random(seed);
        Debug.Log($"ProceduralCityGenerator: Seed = {seed}");
        
        // Nettoyer la ville existante
        ClearCity();
        
        // Créer le parent si nécessaire
        if (cityParent == null)
        {
            GameObject parentObj = new GameObject("ProceduralCity");
            cityParent = parentObj.transform;
            cityParent.position = transform.position;
        }
        
        // Initialiser la grille
        InitializeGrid();
        
        // Générer les routes
        GenerateRoads();
        
        // Générer les bâtiments
        GenerateBuildings();
        
        // Optimisation optionnelle
        if (combineMeshes)
        {
            CombineCityMeshes();
        }
        
        Debug.Log($"ProceduralCityGenerator: Génération terminée! {generatedObjects.Count} objets créés.");
    }

    /// <summary>
    /// Initialise la grille de la ville
    /// </summary>
    void InitializeGrid()
    {
        cityGrid = new GridCell[cityWidth, cityLength];
        
        for (int x = 0; x < cityWidth; x++)
        {
            for (int z = 0; z < cityLength; z++)
            {
                cityGrid[x, z] = new GridCell
                {
                    x = x,
                    z = z,
                    position = GetWorldPosition(x, z),
                    type = CellType.Empty
                };
            }
        }
    }

    /// <summary>
    /// Calcule la position mondiale d'une cellule de grille
    /// </summary>
    Vector3 GetWorldPosition(int gridX, int gridZ)
    {
        float totalBlockSize = blockSize + streetWidth;
        float offsetX = gridX * totalBlockSize;
        float offsetZ = gridZ * totalBlockSize;
        
        return cityParent.position + new Vector3(offsetX, 0f, offsetZ);
    }

    /// <summary>
    /// Génère le réseau de routes
    /// </summary>
    void GenerateRoads()
    {
        if (roadPrefab == null)
        {
            Debug.LogWarning("ProceduralCityGenerator: Aucun prefab de route assigné!");
            return;
        }
        
        Transform roadsParent = new GameObject("Roads").transform;
        roadsParent.SetParent(cityParent);
        
        float totalBlockSize = blockSize + streetWidth;
        
        // Routes horizontales
        for (int z = 0; z <= cityLength; z++)
        {
            for (int x = 0; x < cityWidth; x++)
            {
                Vector3 roadPos = cityParent.position + new Vector3(
                    x * totalBlockSize + blockSize / 2f,
                    roadHeight,
                    z * totalBlockSize - streetWidth / 2f
                );
                
                CreateRoadSegment(roadPos, 0f, roadsParent);
            }
        }
        
        // Routes verticales
        for (int x = 0; x <= cityWidth; x++)
        {
            for (int z = 0; z < cityLength; z++)
            {
                Vector3 roadPos = cityParent.position + new Vector3(
                    x * totalBlockSize - streetWidth / 2f,
                    roadHeight,
                    z * totalBlockSize + blockSize / 2f
                );
                
                CreateRoadSegment(roadPos, 90f, roadsParent);
            }
        }
        
        // Intersections
        if (intersectionPrefab != null)
        {
            for (int x = 0; x <= cityWidth; x++)
            {
                for (int z = 0; z <= cityLength; z++)
                {
                    Vector3 intersectionPos = cityParent.position + new Vector3(
                        x * totalBlockSize - streetWidth / 2f,
                        roadHeight,
                        z * totalBlockSize - streetWidth / 2f
                    );
                    
                    CreateIntersection(intersectionPos, roadsParent);
                }
            }
        }
    }

    /// <summary>
    /// Crée un segment de route
    /// </summary>
    void CreateRoadSegment(Vector3 position, float rotation, Transform parent)
    {
        GameObject road = Instantiate(roadPrefab, position, Quaternion.Euler(0f, rotation, 0f));
        road.transform.SetParent(parent);
        
        // Ajuster l'échelle selon la taille du bloc
        Vector3 scale = road.transform.localScale;
        scale.x = blockSize / 10f; // Assumer que le prefab fait 10m de base
        scale.z = streetWidth / 10f;
        road.transform.localScale = scale;
        
        generatedObjects.Add(road);
    }

    /// <summary>
    /// Crée une intersection
    /// </summary>
    void CreateIntersection(Vector3 position, Transform parent)
    {
        GameObject intersection = Instantiate(intersectionPrefab, position, Quaternion.identity);
        intersection.transform.SetParent(parent);
        
        // Ajuster l'échelle
        Vector3 scale = intersection.transform.localScale;
        scale.x = streetWidth / 10f;
        scale.z = streetWidth / 10f;
        intersection.transform.localScale = scale;
        
        generatedObjects.Add(intersection);
    }

    /// <summary>
    /// Génère tous les bâtiments
    /// </summary>
    void GenerateBuildings()
    {
        Transform buildingsParent = new GameObject("Buildings").transform;
        buildingsParent.SetParent(cityParent);
        
        Vector2 center = new Vector2(cityWidth / 2f, cityLength / 2f);
        
        for (int x = 0; x < cityWidth; x++)
        {
            for (int z = 0; z < cityLength; z++)
            {
                // Vérifier la densité
                if (random.NextDouble() > buildingDensity)
                    continue;
                
                // Calculer la distance au centre (normalisée)
                float distanceToCenter = Vector2.Distance(new Vector2(x, z), center) / (cityWidth / 2f);
                
                // Décider du type de bâtiment
                Vector3 position = GetWorldPosition(x, z);
                
                // Chance de parc
                if (random.NextDouble() < parkProbability && parkPrefabs.Length > 0)
                {
                    CreatePark(position, buildingsParent);
                    cityGrid[x, z].type = CellType.Park;
                }
                else
                {
                    CreateBuilding(position, distanceToCenter, buildingsParent);
                    cityGrid[x, z].type = CellType.Building;
                }
            }
        }
    }

    /// <summary>
    /// Crée un bâtiment selon la distance au centre
    /// </summary>
    void CreateBuilding(Vector3 position, float distanceToCenter, Transform parent)
    {
        GameObject buildingPrefab = SelectBuildingPrefab(distanceToCenter);
        
        if (buildingPrefab == null)
        {
            Debug.LogWarning("ProceduralCityGenerator: Aucun prefab de bâtiment disponible!");
            return;
        }
        
        // Rotation aléatoire (multiples de 90°)
        int rotationIndex = random.Next(4);
        float rotation = rotationIndex * 90f;
        
        GameObject building = Instantiate(buildingPrefab, position, Quaternion.Euler(0f, rotation, 0f));
        building.transform.SetParent(parent);
        
        // Variation d'échelle aléatoire
        float scaleVariation = 0.9f + (float)random.NextDouble() * 0.2f; // 0.9 à 1.1
        building.transform.localScale *= scaleVariation;
        
        generatedObjects.Add(building);
    }

    /// <summary>
    /// Sélectionne un prefab de bâtiment selon la distance au centre
    /// </summary>
    GameObject SelectBuildingPrefab(float distanceToCenter)
    {
        // Probabilité de gratte-ciel diminue avec la distance
        float skyscraperProb = Mathf.Lerp(skyscraperProbabilityCenter, skyscraperProbabilityEdge, distanceToCenter);
        
        if (random.NextDouble() < skyscraperProb && skyscraperPrefabs.Length > 0)
        {
            // Gratte-ciel
            return skyscraperPrefabs[random.Next(skyscraperPrefabs.Length)];
        }
        else if (distanceToCenter < 0.5f && mediumBuildingPrefabs.Length > 0)
        {
            // Bâtiment moyen (zone intermédiaire)
            return mediumBuildingPrefabs[random.Next(mediumBuildingPrefabs.Length)];
        }
        else if (smallBuildingPrefabs.Length > 0)
        {
            // Petit bâtiment (périphérie)
            return smallBuildingPrefabs[random.Next(smallBuildingPrefabs.Length)];
        }
        else if (buildingPrefabs.Length > 0)
        {
            // Fallback sur le tableau général
            return buildingPrefabs[random.Next(buildingPrefabs.Length)];
        }
        
        return null;
    }

    /// <summary>
    /// Crée un parc
    /// </summary>
    void CreatePark(Vector3 position, Transform parent)
    {
        if (parkPrefabs.Length == 0) return;
        
        GameObject parkPrefab = parkPrefabs[random.Next(parkPrefabs.Length)];
        GameObject park = Instantiate(parkPrefab, position, Quaternion.identity);
        park.transform.SetParent(parent);
        
        generatedObjects.Add(park);
    }

    /// <summary>
    /// Nettoie la ville existante
    /// </summary>
    [ContextMenu("Clear City")]
    public void ClearCity()
    {
        foreach (GameObject obj in generatedObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        
        generatedObjects.Clear();
        
        if (cityParent != null && cityParent.childCount > 0)
        {
            // Détruire tous les enfants restants
            for (int i = cityParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(cityParent.GetChild(i).gameObject);
            }
        }
        
        Debug.Log("ProceduralCityGenerator: Ville nettoyée");
    }

    /// <summary>
    /// Combine les meshes pour optimisation
    /// </summary>
    void CombineCityMeshes()
    {
        // TODO: Implémenter la combinaison de meshes
        Debug.Log("ProceduralCityGenerator: Combinaison de meshes non implémentée");
    }

    /// <summary>
    /// Affiche la grille en mode édition
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying && cityWidth > 0 && cityLength > 0)
        {
            Gizmos.color = Color.yellow;
            float totalBlockSize = blockSize + streetWidth;
            
            // Dessiner la grille
            for (int x = 0; x <= cityWidth; x++)
            {
                Vector3 start = transform.position + new Vector3(x * totalBlockSize - streetWidth / 2f, 0f, -streetWidth / 2f);
                Vector3 end = start + new Vector3(0f, 0f, cityLength * totalBlockSize + streetWidth);
                Gizmos.DrawLine(start, end);
            }
            
            for (int z = 0; z <= cityLength; z++)
            {
                Vector3 start = transform.position + new Vector3(-streetWidth / 2f, 0f, z * totalBlockSize - streetWidth / 2f);
                Vector3 end = start + new Vector3(cityWidth * totalBlockSize + streetWidth, 0f, 0f);
                Gizmos.DrawLine(start, end);
            }
        }
    }
}

/// <summary>
/// Structure de cellule de grille
/// </summary>
[System.Serializable]
public class GridCell
{
    public int x;
    public int z;
    public Vector3 position;
    public CellType type;
}

/// <summary>
/// Types de cellules
/// </summary>
public enum CellType
{
    Empty,
    Building,
    Road,
    Park,
    Intersection
}
