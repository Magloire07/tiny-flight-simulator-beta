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
    public float buildingDensity = 1f;
    
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
    
    [Header("Bordures de Routes")]
    [Tooltip("Prefabs de panneaux de signalisation")]
    public GameObject[] signPrefabs;
    
    [Tooltip("Prefabs d'arbres urbains")]
    public GameObject[] streetTreePrefabs;
    
    [Tooltip("Espacement entre les panneaux (en mètres)")]
    public float signSpacing = 15f;
    
    [Tooltip("Espacement entre les arbres (en mètres)")]
    public float treeSpacing = 8f;
    
    [Tooltip("Distance du bord de la route pour les arbres (en mètres)")]
    public float treeOffset = 6f;
    
    [Tooltip("Distance du bord de la route pour les panneaux (en mètres)")]
    public float signOffset = 2f;
    
    [Header("Espaces Verts")]
    [Tooltip("Prefabs de sols variés (herbe verte, terre marron, etc.)")]
    public GameObject[] groundPrefabs;
    
    [Tooltip("Prefabs de parcs")]
    public GameObject[] parkPrefabs;
    
    [Tooltip("Probabilité d'avoir un parc au lieu d'un bâtiment (0-1)")]
    [Range(0f, 1f)]
    public float parkProbability = 0.02f;
    
    [Header("Génération")]
    [Tooltip("Graine aléatoire pour la génération (0 = aléatoire)")]
    public int seed = 0;
    
    [Tooltip("Générer automatiquement au démarrage")]
    public bool generateOnStart = false;
    
    [Tooltip("Parent pour organiser la hiérarchie")]
    public Transform cityParent;
    
    [Header("Optimisation")]
    [Tooltip("Combiner les meshes pour optimiser les performances")]
    public bool combineMeshes = false;
    
    [Tooltip("Distance de culling des bâtiments (0 = désactivé)")]
    public float cullingDistance = 2000f;
    
    [Header("Connexion Routes Campagne")]
    [Tooltip("Créer un point de connexion pour les routes de campagne")]
    public bool createConnectionPoint = true;
    
    [Tooltip("Point de connexion créé (pour CountryRoadGenerator)")]
    public Transform cityConnectionPoint;
    
    [Tooltip("Hauteur du point de connexion")]
    public float connectionPointHeight = 5f;
    
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
        
        // Vérifier les prefabs essentiels
        if (roadPrefab == null)
        {
            Debug.LogError("ProceduralCityGenerator: ERREUR - roadPrefab n'est pas assigné! Assignez un prefab de route dans l'Inspector.");
            return;
        }
        
        if (buildingPrefabs.Length == 0 && smallBuildingPrefabs.Length == 0 && 
            mediumBuildingPrefabs.Length == 0 && skyscraperPrefabs.Length == 0)
        {
            Debug.LogError("ProceduralCityGenerator: ERREUR - Aucun prefab de bâtiment assigné! Assignez au moins un tableau de bâtiments dans l'Inspector.");
            return;
        }
        
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
        
        // Générer les panneaux et arbres en bordure de routes
        GenerateRoadsideElements();
        
        // Générer les bâtiments
        GenerateBuildings();
        
        // Créer le point de connexion pour les routes de campagne
        if (createConnectionPoint)
        {
            CreateCityConnectionPoint();
        }
        
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
    /// Obtient la position centrale d'un bloc (pour placer les bâtiments)
    /// </summary>
    Vector3 GetBlockCenterPosition(int gridX, int gridZ)
    {
        float totalBlockSize = blockSize + streetWidth;
        float offsetX = gridX * totalBlockSize + blockSize / 2f;
        float offsetZ = gridZ * totalBlockSize + blockSize / 2f;
        
        return cityParent.position + new Vector3(offsetX, 0f, offsetZ);
    }

    /// <summary>
    /// Génère le réseau de routes
    /// </summary>
    void GenerateRoads()
    {
        Debug.Log("ProceduralCityGenerator: Génération des routes...");
        
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
    /// Génère les panneaux et arbres le long des routes
    /// </summary>
    void GenerateRoadsideElements()
    {
        Debug.Log("ProceduralCityGenerator: Génération des panneaux et arbres...");
        
        Transform roadsideParent = new GameObject("RoadsideElements").transform;
        roadsideParent.SetParent(cityParent);
        
        float totalBlockSize = blockSize + streetWidth;
        
        // Panneaux et arbres le long des routes horizontales
        for (int z = 0; z <= cityLength; z++)
        {
            float roadZ = z * totalBlockSize - streetWidth / 2f;
            
            // Le long de cette route horizontale
            for (float x = 0; x < cityWidth * totalBlockSize; x += treeSpacing)
            {
                Vector3 position = cityParent.position + new Vector3(x, roadHeight, roadZ);
                
                // Arbres des deux côtés de la route (plus loin)
                if (streetTreePrefabs != null && streetTreePrefabs.Length > 0 && random.NextDouble() < 0.8f)
                {
                    PlaceStreetTree(position + Vector3.forward * treeOffset, roadsideParent);
                    PlaceStreetTree(position + Vector3.back * treeOffset, roadsideParent);
                }
            }
            
            // Panneaux moins fréquents (plus près de la route)
            for (float x = 0; x < cityWidth * totalBlockSize; x += signSpacing)
            {
                Vector3 position = cityParent.position + new Vector3(x, roadHeight, roadZ);
                
                if (signPrefabs != null && signPrefabs.Length > 0 && random.NextDouble() < 0.6f)
                {
                    PlaceSign(position + Vector3.forward * signOffset, 180f, roadsideParent);
                }
            }
        }
        
        // Panneaux et arbres le long des routes verticales
        for (int x = 0; x <= cityWidth; x++)
        {
            float roadX = x * totalBlockSize - streetWidth / 2f;
            
            // Le long de cette route verticale
            for (float z = 0; z < cityLength * totalBlockSize; z += treeSpacing)
            {
                Vector3 position = cityParent.position + new Vector3(roadX, roadHeight, z);
                
                // Arbres des deux côtés de la route (plus loin)
                if (streetTreePrefabs != null && streetTreePrefabs.Length > 0 && random.NextDouble() < 0.8f)
                {
                    PlaceStreetTree(position + Vector3.right * treeOffset, roadsideParent);
                    PlaceStreetTree(position + Vector3.left * treeOffset, roadsideParent);
                }
            }
            
            // Panneaux moins fréquents (plus près de la route)
            for (float z = 0; z < cityLength * totalBlockSize; z += signSpacing)
            {
                Vector3 position = cityParent.position + new Vector3(roadX, roadHeight, z);
                
                if (signPrefabs != null && signPrefabs.Length > 0 && random.NextDouble() < 0.6f)
                {
                    PlaceSign(position + Vector3.right * signOffset, 90f, roadsideParent);
                }
            }
        }
    }
    
    /// <summary>
    /// Place un arbre en bordure de route
    /// </summary>
    void PlaceStreetTree(Vector3 position, Transform parent)
    {
        GameObject treePrefab = streetTreePrefabs[random.Next(streetTreePrefabs.Length)];
        GameObject tree = Instantiate(treePrefab, position, Quaternion.identity);
        tree.transform.SetParent(parent);
        
        // Rotation aléatoire
        float rotation = (float)random.NextDouble() * 360f;
        tree.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        
        // Légère variation d'échelle
        float scale = 0.9f + (float)random.NextDouble() * 0.2f; // 0.9 à 1.1
        tree.transform.localScale *= scale;
        
        generatedObjects.Add(tree);
    }
    
    /// <summary>
    /// Place un panneau de signalisation
    /// </summary>
    void PlaceSign(Vector3 position, float baseRotation, Transform parent)
    {
        GameObject signPrefab = signPrefabs[random.Next(signPrefabs.Length)];
        
        // Rotation vers la route
        float randomOffset = ((float)random.NextDouble() - 0.5f) * 30f; // ±15°
        Quaternion rotation = Quaternion.Euler(0f, baseRotation + randomOffset, 0f);
        
        GameObject sign = Instantiate(signPrefab, position, rotation);
        sign.transform.SetParent(parent);
        
        generatedObjects.Add(sign);
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
        Debug.Log("ProceduralCityGenerator: Génération des bâtiments...");
        
        Transform buildingsParent = new GameObject("Buildings").transform;
        buildingsParent.SetParent(cityParent);
        
        Transform groundParent = new GameObject("GroundBlocks").transform;
        groundParent.SetParent(cityParent);
        
        Vector2 center = new Vector2(cityWidth / 2f, cityLength / 2f);
        
        for (int x = 0; x < cityWidth; x++)
        {
            for (int z = 0; z < cityLength; z++)
            {
                // Position centrale du bloc
                Vector3 blockCenter = GetBlockCenterPosition(x, z);
                
                // 1. TOUJOURS créer un sol (vert/marron) pour CHAQUE bloc - pas de probabilité
                CreateGroundBlock(blockCenter, groundParent);
                
                // Calculer la distance au centre (normalisée)
                float distanceToCenter = Vector2.Distance(new Vector2(x, z), center) / (cityWidth / 2f);
                
                // 2. TOUJOURS ajouter des bâtiments par-dessus le sol - 100% densité
                // Chance de parc (ajoute des éléments décoratifs) - très rare
                if (random.NextDouble() < parkProbability && parkPrefabs.Length > 0)
                {
                    CreatePark(blockCenter, buildingsParent);
                    cityGrid[x, z].type = CellType.Park;
                }
                else
                {
                    // Placer les bâtiments de manière hétérogène avec chevauchements minimes
                    int buildingsInBlock = random.Next(50, 80); // Maximum: 50-80 bâtiments par bloc
                    float minBuildingSpacing = 5f; // Distance minimale entre bâtiments
                    List<Vector3> placedPositions = new List<Vector3>();
                    
                    int attempts = 0;
                    int maxAttempts = buildingsInBlock * 10;
                    
                    while (placedPositions.Count < buildingsInBlock && attempts < maxAttempts)
                    {
                        attempts++;
                        
                        // Position complètement aléatoire dans le bloc
                        float offsetX = ((float)random.NextDouble() - 0.5f) * blockSize * 0.9f;
                        float offsetZ = ((float)random.NextDouble() - 0.5f) * blockSize * 0.9f;
                        Vector3 position = blockCenter + new Vector3(offsetX, 0f, offsetZ);
                        
                        // Vérifier la distance minimale avec les autres bâtiments
                        bool tooClose = false;
                        foreach (Vector3 existing in placedPositions)
                        {
                            float distance = Vector2.Distance(
                                new Vector2(position.x, position.z),
                                new Vector2(existing.x, existing.z)
                            );
                            if (distance < minBuildingSpacing)
                            {
                                tooClose = true;
                                break;
                            }
                        }
                        
                        if (!tooClose)
                        {
                            CreateBuilding(position, distanceToCenter, buildingsParent);
                            placedPositions.Add(position);
                        }
                    }
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
        // Favoriser les petits et moyens bâtiments (densité urbaine réaliste)
        double rand = random.NextDouble();
        
        // Au centre : 50% petits, 30% moyens, 20% gratte-ciels
        // En périphérie : 70% petits, 25% moyens, 5% gratte-ciels
        float smallProb = Mathf.Lerp(0.5f, 0.7f, distanceToCenter);
        float mediumProb = Mathf.Lerp(0.3f, 0.25f, distanceToCenter);
        // Le reste = gratte-ciels
        
        if (rand < smallProb && smallBuildingPrefabs.Length > 0)
        {
            // Petit bâtiment (50-70%)
            return smallBuildingPrefabs[random.Next(smallBuildingPrefabs.Length)];
        }
        else if (rand < smallProb + mediumProb && mediumBuildingPrefabs.Length > 0)
        {
            // Bâtiment moyen (25-30%)
            return mediumBuildingPrefabs[random.Next(mediumBuildingPrefabs.Length)];
        }
        else if (skyscraperPrefabs.Length > 0)
        {
            // Gratte-ciel (5-20%)
            return skyscraperPrefabs[random.Next(skyscraperPrefabs.Length)];
        }
        else if (buildingPrefabs.Length > 0)
        {
            // Fallback sur le tableau général
            return buildingPrefabs[random.Next(buildingPrefabs.Length)];
        }
        
        return null;
    }

    /// <summary>
    /// Crée un bloc de sol/herbe qui couvre tout le bloc
    /// </summary>
    void CreateGroundBlock(Vector3 centerPosition, Transform parent)
    {
        if (groundPrefabs == null || groundPrefabs.Length == 0)
        {
            Debug.LogWarning("ProceduralCityGenerator: Aucun groundPrefab assigné! Les blocs n'auront pas de sol.");
            return;
        }
        
        // Choisir un prefab de sol aléatoire pour créer de la variété (vert, marron, etc.)
        GameObject selectedGroundPrefab = groundPrefabs[random.Next(groundPrefabs.Length)];
        
        // Position légèrement en dessous pour être sous les bâtiments
        Vector3 groundPosition = centerPosition + Vector3.down * 0.1f;
        
        GameObject ground = Instantiate(selectedGroundPrefab, groundPosition, Quaternion.identity);
        ground.transform.SetParent(parent);
        ground.name = "GroundBlock";
        
        // Rotation aléatoire pour plus de variété
        float rotation = random.Next(4) * 90f; // 0°, 90°, 180°, 270°
        ground.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        
        // Ajuster l'échelle pour couvrir le bloc sans cacher les routes
        // Le prefab Env_GrassLand_Plain fait 5m de base (scale 20 = 100m)
        Vector3 scale = ground.transform.localScale;
        float targetSize = blockSize + streetWidth * 0.1f; // Petit débordement pour éviter les espaces mais laisse les routes visibles
        scale.x = targetSize / 5f; // Diviser par 5m (taille de base du prefab)
        scale.z = targetSize / 5f;
        ground.transform.localScale = scale;
        
        generatedObjects.Add(ground);
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
            // Détruire tous les enfants restants (sauf le point de connexion)
            for (int i = cityParent.childCount - 1; i >= 0; i--)
            {
                Transform child = cityParent.GetChild(i);
                if (child != cityConnectionPoint) // Garder le point de connexion
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }
        
        Debug.Log("ProceduralCityGenerator: Ville nettoyée");
    }

    /// <summary>
    /// Crée un point de connexion au centre de la ville pour les routes de campagne
    /// </summary>
    void CreateCityConnectionPoint()
    {
        // Calculer le centre de la ville
        float totalBlockSize = blockSize + streetWidth;
        Vector3 centerOffset = new Vector3(
            (cityWidth - 1) * totalBlockSize / 2f,
            connectionPointHeight,
            (cityLength - 1) * totalBlockSize / 2f
        );
        
        Vector3 centerPosition = cityParent.position + centerOffset;
        
        // Créer ou mettre à jour le point de connexion
        if (cityConnectionPoint == null)
        {
            GameObject connectionObj = new GameObject("CityConnectionPoint");
            cityConnectionPoint = connectionObj.transform;
            cityConnectionPoint.SetParent(cityParent);
        }
        
        cityConnectionPoint.position = centerPosition;
        
        Debug.Log($"ProceduralCityGenerator: Point de connexion créé au centre de la ville: {centerPosition}");
    }
    
    /// <summary>
    /// Obtient le point de connexion de la ville
    /// </summary>
    public Transform GetConnectionPoint()
    {
        return cityConnectionPoint;
    }
    
    /// <summary>
    /// Combine les meshes pour optimisation
    /// </summary>
    void CombineCityMeshes()
    {
        // TODO: Implémenter la combinaison de meshes pour optimisation
        Debug.Log("ProceduralCityGenerator: Combinaison de meshes non encore implémentée");
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
            
            // Dessiner le point de connexion
            if (createConnectionPoint)
            {
                Gizmos.color = Color.cyan;
                Vector3 centerOffset = new Vector3(
                    (cityWidth - 1) * totalBlockSize / 2f,
                    connectionPointHeight,
                    (cityLength - 1) * totalBlockSize / 2f
                );
                Vector3 centerPosition = transform.position + centerOffset;
                Gizmos.DrawWireSphere(centerPosition, 30f);
                Gizmos.DrawLine(centerPosition, centerPosition + Vector3.down * connectionPointHeight);
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
}
