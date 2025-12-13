using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Gère la génération procédurale du monde (villes, routes, lacs, moulins) 
/// en fonction de la position de l'avion
/// </summary>
public class ProceduralWorldManager : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Transform de l'avion à suivre")]
    public Transform playerPlane;
    
    [Tooltip("Terrain actif (laisser vide si génération procédurale)")]
    public Terrain terrain;
    
    [Tooltip("Prefab de TerrainGenerator pour génération procédurale")]
    public GameObject terrainGeneratorPrefab;
    
    [Tooltip("Générer le terrain procéduralement par chunks")]
    public bool generateTerrainProcedurally = false;
    
    [Header("Générateurs")]
    [Tooltip("Prefab contenant ProceduralCityGenerator")]
    public GameObject cityGeneratorPrefab;
    
    [Tooltip("Prefab contenant CountryRoadGenerator")]
    public GameObject roadGeneratorPrefab;
    
    [Tooltip("Prefab contenant LakeGenerator")]
    public GameObject lakeGeneratorPrefab;
    
    [Tooltip("Prefab contenant WindmillGenerator")]
    public GameObject windmillGeneratorPrefab;
    
    [Tooltip("Prefab contenant FarmRoadGenerator")]
    public GameObject farmRoadGeneratorPrefab;
    
    [Tooltip("Prefab d'aéroport")]
    public GameObject airportPrefab;
    
    [Header("Configuration Chunks")]
    [Tooltip("Taille d'un chunk (zone) en mètres")]
    public float chunkSize = 2000f;
    
    [Tooltip("Distance de vision (en chunks) - génère dans ce rayon")]
    [Range(1, 5)]
    public int viewDistance = 3;
    
    [Tooltip("Distance de déchargement (en chunks) - supprime au-delà")]
    [Range(2, 8)]
    public int unloadDistance = 3;
    
    [Header("Densité de Génération (Perlin Noise)")]
    [Tooltip("Seuil Perlin pour villes (0-1) - plus bas = plus de villes")]
    [Range(0f, 1f)]
    public float cityThreshold = 0.05f;
    
    [Tooltip("Seuil Perlin pour lacs (0-1) - plus bas = plus de lacs")]
    [Range(0f, 1f)]
    public float lakeThreshold = 0.4f;
    
    [Tooltip("Seuil Perlin pour moulins (0-1) - plus bas = plus de moulins")]
    [Range(0f, 1f)]
    public float windmillThreshold = 0.25f;
    
    [Tooltip("Seuil Perlin pour routes (0-1)")]
    [Range(0f, 1f)]
    public float countryRoadThreshold = 0.2f;
    
    [Tooltip("Seuil Perlin pour chemins de ferme (0-1)")]
    [Range(0f, 1f)]
    public float farmRoadThreshold = 0.15f;
    
    [Tooltip("Seuil Perlin pour aéroports (0-1) - plus haut = plus rare")]
    [Range(0f, 1f)]
    public float airportThreshold = 0.85f;
    
    [Header("Fréquences Perlin Noise")]
    [Tooltip("Échelle du bruit pour villes (plus petit = zones plus grandes)")]
    public float cityNoiseScale = 0.02f;
    
    [Tooltip("Échelle du bruit pour lacs")]
    public float lakeNoiseScale = 0.03f;
    
    [Tooltip("Échelle du bruit pour moulins")]
    public float windmillNoiseScale = 0.025f;
    
    [Tooltip("Échelle du bruit pour aéroports")]
    public float airportNoiseScale = 0.01f;
    
    [Header("Aéroport de Départ")]
    [Tooltip("Aéroport de référence existant dans la scène (laisser vide pour générer)")]
    public GameObject referenceAirport;
    
    [Tooltip("Générer un aéroport de départ si aucun aéroport de référence")]
    public bool generateStartAirport = true;
    
    [Tooltip("Offset de l'avion sur la piste (en mètres depuis le centre)")]
    public Vector3 planeStartOffset = new Vector3(0f, 2f, -30f);
    
    [Tooltip("Rayon de protection autour de l'aéroport (en chunks)")]
    [Range(0, 2)]
    public int airportProtectionRadius = 0;
    
    [Header("Optimisation")]
    [Tooltip("Vérifier la génération tous les X secondes")]
    public float updateInterval = 2f;
    
    [Tooltip("Graine aléatoire globale (0 = aléatoire)")]
    public int globalSeed = 0;
    
    [Tooltip("Parent pour tous les chunks générés")]
    public Transform worldParent;
    
    // Données internes
    private Dictionary<Vector2Int, ChunkData> loadedChunks = new Dictionary<Vector2Int, ChunkData>();
    private HashSet<Vector2Int> protectedChunks = new HashSet<Vector2Int>(); // Chunks où ne rien générer
    private Vector2Int currentChunkCoord;
    private Vector2Int lastChunkCoord;
    private float nextUpdateTime;
    private System.Random random;

    void Start()
    {
        // Initialiser
        if (playerPlane == null)
        {
            GameObject plane = GameObject.FindGameObjectWithTag("Player");
            if (plane != null)
                playerPlane = plane.transform;
            else
                Debug.LogError("ProceduralWorldManager: Aucun avion trouvé! Assignez playerPlane ou ajoutez le tag 'Player' à l'avion.");
        }
        
        if (terrain == null)
            terrain = Terrain.activeTerrain;
        
        if (worldParent == null)
        {
            GameObject parentObj = new GameObject("ProceduralWorld");
            worldParent = parentObj.transform;
        }
        
        // Initialiser le générateur aléatoire
        if (globalSeed == 0)
            globalSeed = Random.Range(1, 999999);
        
        random = new System.Random(globalSeed);
        Debug.Log($"ProceduralWorldManager: Graine globale = {globalSeed}");
        
        // Utiliser l'aéroport de référence ou générer un nouveau
        if (playerPlane != null)
        {
            if (referenceAirport != null)
            {
                // Utiliser l'aéroport existant comme référence
                PlacePlayerOnReferenceAirport();
                
                // Marquer les chunks autour de la position de la piste (où l'avion est placé) comme protégés
                Vector3 runwayPosition = referenceAirport.transform.position + planeStartOffset;
                Vector2Int runwayChunk = GetChunkCoord(runwayPosition);
                ProtectChunksAroundPoint(runwayChunk, airportProtectionRadius);
                Debug.Log($"ProceduralWorldManager: Zone de {airportProtectionRadius} chunks autour de la piste protégée (position: {runwayPosition})");
            }
            else if (generateStartAirport && airportPrefab != null)
            {
                // Générer un nouvel aéroport
                GenerateStartAirport();
                
                // Marquer les chunks autour de la piste (centre + offset) comme protégés
                Vector3 runwayPosition = Vector3.zero + planeStartOffset;
                Vector2Int startChunk = GetChunkCoord(runwayPosition);
                ProtectChunksAroundPoint(startChunk, airportProtectionRadius);
            }
        }
        
        // Première génération
        if (playerPlane != null)
        {
            currentChunkCoord = GetChunkCoord(playerPlane.position);
            lastChunkCoord = currentChunkCoord;
            UpdateChunks();
        }
    }

    void Update()
    {
        if (playerPlane == null) return;
        
        // Vérification périodique
        if (Time.time >= nextUpdateTime)
        {
            nextUpdateTime = Time.time + updateInterval;
            
            currentChunkCoord = GetChunkCoord(playerPlane.position);
            
            // Si l'avion a changé de chunk, mettre à jour
            if (currentChunkCoord != lastChunkCoord)
            {
                UpdateChunks();
                lastChunkCoord = currentChunkCoord;
            }
        }
    }

    /// <summary>
    /// Met à jour les chunks (génère nouveaux, décharge anciens)
    /// </summary>
    void UpdateChunks()
    {
        // Générer les chunks dans le rayon de vision
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int chunkCoord = currentChunkCoord + new Vector2Int(x, z);
                
                // Si le chunk n'est pas chargé, le générer
                if (!loadedChunks.ContainsKey(chunkCoord))
                {
                    GenerateChunk(chunkCoord);
                }
            }
        }
        
        // Décharger les chunks trop éloignés
        List<Vector2Int> chunksToUnload = new List<Vector2Int>();
        
        foreach (var kvp in loadedChunks)
        {
            Vector2Int chunkCoord = kvp.Key;
            float distance = Vector2Int.Distance(chunkCoord, currentChunkCoord);
            
            if (distance > unloadDistance)
            {
                chunksToUnload.Add(chunkCoord);
            }
        }
        
        foreach (Vector2Int chunkCoord in chunksToUnload)
        {
            UnloadChunk(chunkCoord);
        }
        
        Debug.Log($"ProceduralWorldManager: {loadedChunks.Count} chunks chargés, position actuelle: {currentChunkCoord}");
    }

    /// <summary>
    /// Génère un chunk complet (villes, routes, lacs, moulins)
    /// </summary>
    void GenerateChunk(Vector2Int chunkCoord)
    {
        // Vérifier si ce chunk est protégé (aéroport de départ)
        if (protectedChunks.Contains(chunkCoord))
        {
            Debug.Log($"ProceduralWorldManager: Chunk {chunkCoord} protégé, aucune génération");
            
            // Créer un chunk vide pour marquer comme chargé
            GameObject chunkObj = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}_Protected");
            chunkObj.transform.SetParent(worldParent);
            chunkObj.transform.position = GetChunkWorldPosition(chunkCoord);
            
            ChunkData chunkData = new ChunkData
            {
                coord = chunkCoord,
                parent = chunkObj.transform,
                generatedObjects = new List<GameObject>()
            };
            
            loadedChunks[chunkCoord] = chunkData;
            return; // Ne rien générer
        }
        
        Debug.Log($"ProceduralWorldManager: Génération du chunk {chunkCoord}...");
        
        // Créer le parent du chunk
        GameObject chunkObj2 = new GameObject($"Chunk_{chunkCoord.x}_{chunkCoord.y}");
        chunkObj2.transform.SetParent(worldParent);
        chunkObj2.transform.position = GetChunkWorldPosition(chunkCoord);
        
        ChunkData chunkData2 = new ChunkData
        {
            coord = chunkCoord,
            parent = chunkObj2.transform,
            generatedObjects = new List<GameObject>()
        };
        
        // Seed unique pour ce chunk (basé sur les coordonnées)
        int chunkSeed = GetChunkSeed(chunkCoord);
        System.Random chunkRandom = new System.Random(chunkSeed);
        
        Vector3 chunkCenter = GetChunkWorldPosition(chunkCoord);
        
        // Générer le terrain pour ce chunk si mode procédural
        if (generateTerrainProcedurally && terrainGeneratorPrefab != null)
        {
            GenerateTerrainChunk(chunkCenter, chunkSeed, chunkObj2.transform, chunkData2);
        }
        
        // Évaluer le bruit de Perlin pour ce chunk (génération naturelle)
        float cityNoise = GetPerlinValue(chunkCoord.x, chunkCoord.y, cityNoiseScale, 1000f);
        float lakeNoise = GetPerlinValue(chunkCoord.x, chunkCoord.y, lakeNoiseScale, 2000f);
        float windmillNoise = GetPerlinValue(chunkCoord.x, chunkCoord.y, windmillNoiseScale, 3000f);
        float countryRoadNoise = GetPerlinValue(chunkCoord.x, chunkCoord.y, 0.04f, 4000f);
        float farmRoadNoise = GetPerlinValue(chunkCoord.x, chunkCoord.y, 0.05f, 5000f);
        float airportNoise = GetPerlinValue(chunkCoord.x, chunkCoord.y, airportNoiseScale, 6000f);
        
        bool hasCity = false;
        bool hasAirport = false;
        
        // Générer aéroport si zone très rare (Perlin élevé)
        if (airportNoise > airportThreshold && airportPrefab != null)
        {
            GenerateAirport(chunkCenter, chunkSeed, chunkObj2.transform, chunkData2);
            hasAirport = true;
        }
        
        // Générer ville si le bruit Perlin dépasse le seuil (crée des zones urbaines)
        if (cityNoise > cityThreshold && cityGeneratorPrefab != null && !hasAirport)
        {
            GenerateCity(chunkCenter, chunkSeed, chunkObj2.transform, chunkData2);
            hasCity = true;
        }
        
        // Générer lac si dans une zone lacustre (Perlin)
        if (lakeNoise > lakeThreshold && lakeGeneratorPrefab != null)
        {
            GenerateLake(chunkCenter, chunkSeed, chunkObj2.transform, chunkData2);
        }
        
        // Si pas de ville, générer environnement rural selon Perlin noise
        if (!hasCity)
        {
            // Générer moulins dans zones venteuses (Perlin)
            if (windmillNoise > windmillThreshold && windmillGeneratorPrefab != null)
            {
                GenerateWindmills(chunkCenter, chunkSeed, chunkObj2.transform, chunkData2);
            }
            
            // Générer routes de campagne (Perlin)
            if (countryRoadNoise > countryRoadThreshold && roadGeneratorPrefab != null)
            {
                GenerateCountryRoads(chunkCenter, chunkSeed, chunkObj2.transform, chunkData2);
            }
            
            // Générer chemins de ferme dans zones agricoles (Perlin)
            if (farmRoadNoise > farmRoadThreshold && farmRoadGeneratorPrefab != null)
            {
                GenerateFarmRoads(chunkCenter, chunkSeed, chunkObj2.transform, chunkData2);
            }
        }
        
        loadedChunks.Add(chunkCoord, chunkData2);
        
        Debug.Log($"ProceduralWorldManager: Chunk {chunkCoord} généré avec {chunkData2.generatedObjects.Count} éléments");
    }

    /// <summary>
    /// Génère le terrain procédural pour ce chunk
    /// </summary>
    void GenerateTerrainChunk(Vector3 center, int seed, Transform parent, ChunkData chunkData)
    {
        GameObject terrainObj = Instantiate(terrainGeneratorPrefab, center, Quaternion.identity);
        terrainObj.transform.SetParent(parent);
        terrainObj.name = "TerrainChunk";
        
        TerrainGenerator terrainGen = terrainObj.GetComponent<TerrainGenerator>();
        if (terrainGen != null)
        {
            // Configurer le générateur de terrain pour ce chunk (terrain plus plat)
            terrainGen.mapSize = 255; // Limité à 255 pour éviter l'erreur GPU (max thread groups)
            terrainGen.elevationScale = 3f; // Réduit l'élévation (au lieu de 10)
            terrainGen.scale = 30f; // Augmente l'échelle horizontale pour adoucir
            terrainGen.numErosionIterations = 30000; // Plus d'érosion pour aplanir
            terrainGen.GenerateHeightMap();
        }
        
        chunkData.generatedObjects.Add(terrainObj);
    }

    /// <summary>
    /// Génère une ville dans le chunk
    /// </summary>
    void GenerateCity(Vector3 center, int seed, Transform parent, ChunkData chunkData)
    {
        GameObject cityObj = Instantiate(cityGeneratorPrefab, center, Quaternion.identity);
        cityObj.transform.SetParent(parent);
        cityObj.name = "City";
        
        ProceduralCityGenerator cityGen = cityObj.GetComponent<ProceduralCityGenerator>();
        if (cityGen != null)
        {
            cityGen.seed = seed;
            cityGen.generateOnStart = false;
            cityGen.GenerateCity();
        }
        
        chunkData.generatedObjects.Add(cityObj);
    }

    /// <summary>
    /// Génère un lac dans le chunk
    /// </summary>
    void GenerateLake(Vector3 center, int seed, Transform parent, ChunkData chunkData)
    {
        GameObject lakeObj = Instantiate(lakeGeneratorPrefab, center, Quaternion.identity);
        lakeObj.transform.SetParent(parent);
        lakeObj.name = "Lake";
        
        LakeGenerator lakeGen = lakeObj.GetComponent<LakeGenerator>();
        if (lakeGen != null)
        {
            lakeGen.seed = seed;
            lakeGen.generationCenter = center;
            lakeGen.generationRadius = chunkSize / 2f;
            lakeGen.GenerateLakes();
        }
        
        chunkData.generatedObjects.Add(lakeObj);
    }

    /// <summary>
    /// Génère des moulins dans le chunk
    /// </summary>
    void GenerateWindmills(Vector3 center, int seed, Transform parent, ChunkData chunkData)
    {
        GameObject windmillObj = Instantiate(windmillGeneratorPrefab, center, Quaternion.identity);
        windmillObj.transform.SetParent(parent);
        windmillObj.name = "Windmills";
        
        WindmillGenerator windmillGen = windmillObj.GetComponent<WindmillGenerator>();
        if (windmillGen != null)
        {
            windmillGen.seed = seed;
            windmillGen.generationCenter = center;
            windmillGen.generationRadius = chunkSize / 2f;
            windmillGen.GenerateWindmills();
        }
        
        chunkData.generatedObjects.Add(windmillObj);
    }

    /// <summary>
    /// Génère des routes de campagne dans le chunk
    /// </summary>
    void GenerateCountryRoads(Vector3 center, int seed, Transform parent, ChunkData chunkData)
    {
        GameObject roadObj = Instantiate(roadGeneratorPrefab, center, Quaternion.identity);
        roadObj.transform.SetParent(parent);
        roadObj.name = "CountryRoads";
        
        CountryRoadGenerator roadGen = roadObj.GetComponent<CountryRoadGenerator>();
        if (roadGen != null)
        {
            roadGen.seed = seed;
            roadGen.mainRoadSpacing = chunkSize / 3f;
            roadGen.horizontalMainRoads = 2;
            roadGen.verticalMainRoads = 2;
            roadGen.GenerateCountryRoads();
        }
        
        chunkData.generatedObjects.Add(roadObj);
    }

    /// <summary>
    /// Génère des chemins de ferme dans le chunk
    /// </summary>
    void GenerateFarmRoads(Vector3 center, int seed, Transform parent, ChunkData chunkData)
    {
        GameObject farmRoadObj = Instantiate(farmRoadGeneratorPrefab, center, Quaternion.identity);
        farmRoadObj.transform.SetParent(parent);
        farmRoadObj.name = "FarmRoads";
        
        FarmRoadGenerator farmRoadGen = farmRoadObj.GetComponent<FarmRoadGenerator>();
        if (farmRoadGen != null)
        {
            farmRoadGen.seed = seed;
            farmRoadGen.generationCenter = center;
            farmRoadGen.generationRadius = chunkSize / 2f;
            farmRoadGen.GenerateFarmRoads();
        }
        
        chunkData.generatedObjects.Add(farmRoadObj);
    }

    /// <summary>
    /// Génère un aéroport dans le chunk
    /// </summary>
    void GenerateAirport(Vector3 center, int seed, Transform parent, ChunkData chunkData)
    {
        // Ajuster la hauteur au terrain
        if (terrain != null)
        {
            center.y = terrain.SampleHeight(center);
        }
        
        // Rotation aléatoire pour varier l'orientation
        System.Random airportRandom = new System.Random(seed);
        float rotation = airportRandom.Next(4) * 90f; // 0, 90, 180, ou 270 degrés
        
        GameObject airportObj = Instantiate(airportPrefab, center, Quaternion.Euler(0f, rotation, 0f));
        airportObj.transform.SetParent(parent);
        airportObj.name = "Airport";
        
        chunkData.generatedObjects.Add(airportObj);
        
        Debug.Log($"ProceduralWorldManager: Aéroport généré à {center}");
    }

    /// <summary>
    /// Génère l'aéroport de départ et place l'avion dessus
    /// </summary>
    void GenerateStartAirport()
    {
        // Position de départ (centre du monde ou position actuelle de l'avion)
        Vector3 startPosition = Vector3.zero;
        
        // Ajuster la hauteur au terrain si disponible
        if (terrain != null)
        {
            startPosition.y = terrain.SampleHeight(startPosition);
        }
        
        // Créer l'aéroport
        GameObject startAirport = Instantiate(airportPrefab, startPosition, Quaternion.identity);
        startAirport.transform.SetParent(worldParent);
        startAirport.name = "StartAirport";
        
        // Placer l'avion sur la piste
        Vector3 planePosition = startPosition + planeStartOffset;
        playerPlane.position = planePosition;
        playerPlane.rotation = Quaternion.Euler(0f, 0f, 0f); // Orienté vers le nord
        
        // Réinitialiser la vélocité si c'est un Rigidbody
        Rigidbody planeRb = playerPlane.GetComponent<Rigidbody>();
        if (planeRb != null)
        {
            planeRb.velocity = Vector3.zero;
            planeRb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log($"ProceduralWorldManager: Aéroport de départ créé, avion placé à {planePosition}");
    }

    /// <summary>
    /// Place l'avion sur l'aéroport de référence existant
    /// </summary>
    void PlacePlayerOnReferenceAirport()
    {
        // Utiliser la position de l'aéroport de référence comme base du monde
        Vector3 airportPosition = referenceAirport.transform.position;
        
        // Placer l'avion sur la piste avec l'offset
        Vector3 planePosition = airportPosition + planeStartOffset;
        playerPlane.position = planePosition;
        
        // Aligner l'avion avec la rotation de l'aéroport
        playerPlane.rotation = referenceAirport.transform.rotation;
        
        // Réinitialiser la vélocité si c'est un Rigidbody
        Rigidbody planeRb = playerPlane.GetComponent<Rigidbody>();
        if (planeRb != null)
        {
            planeRb.velocity = Vector3.zero;
            planeRb.angularVelocity = Vector3.zero;
        }
        
        Debug.Log($"ProceduralWorldManager: Avion placé sur l'aéroport de référence à {planePosition}");
    }

    /// <summary>
    /// Protège une zone de chunks autour d'un point
    /// </summary>
    void ProtectChunksAroundPoint(Vector2Int centerChunk, int radius)
    {
        for (int x = -radius; x <= radius; x++)
        {
            for (int z = -radius; z <= radius; z++)
            {
                Vector2Int chunkToProtect = new Vector2Int(centerChunk.x + x, centerChunk.y + z);
                protectedChunks.Add(chunkToProtect);
                Debug.Log($"ProceduralWorldManager: Chunk {chunkToProtect} protégé");
            }
        }
    }

    /// <summary>
    /// Décharge un chunk (détruit tous ses objets)
    /// </summary>
    void UnloadChunk(Vector2Int chunkCoord)
    {
        if (!loadedChunks.ContainsKey(chunkCoord)) return;
        
        ChunkData chunkData = loadedChunks[chunkCoord];
        
        // Détruire tous les objets du chunk
        foreach (GameObject obj in chunkData.generatedObjects)
        {
            if (obj != null)
                Destroy(obj);
        }
        
        // Détruire le parent du chunk
        if (chunkData.parent != null)
            Destroy(chunkData.parent.gameObject);
        
        loadedChunks.Remove(chunkCoord);
        
        Debug.Log($"ProceduralWorldManager: Chunk {chunkCoord} déchargé");
    }

    /// <summary>
    /// Obtient les coordonnées de chunk d'une position mondiale
    /// </summary>
    Vector2Int GetChunkCoord(Vector3 worldPosition)
    {
        int x = Mathf.FloorToInt(worldPosition.x / chunkSize);
        int z = Mathf.FloorToInt(worldPosition.z / chunkSize);
        return new Vector2Int(x, z);
    }

    /// <summary>
    /// Obtient la position mondiale du centre d'un chunk
    /// </summary>
    Vector3 GetChunkWorldPosition(Vector2Int chunkCoord)
    {
        float x = chunkCoord.x * chunkSize + chunkSize / 2f;
        float z = chunkCoord.y * chunkSize + chunkSize / 2f;
        float y = 0f;
        
        if (terrain != null)
        {
            y = terrain.SampleHeight(new Vector3(x, 0f, z));
        }
        
        return new Vector3(x, y, z);
    }

    /// <summary>
    /// Génère une seed unique pour un chunk basée sur ses coordonnées
    /// </summary>
    int GetChunkSeed(Vector2Int chunkCoord)
    {
        // Utiliser une fonction de hachage pour créer une seed unique et cohérente
        int hash = globalSeed;
        hash = hash * 31 + chunkCoord.x;
        hash = hash * 31 + chunkCoord.y;
        return Mathf.Abs(hash);
    }
    
    /// <summary>
    /// Obtient une valeur de Perlin noise pour un chunk (génération naturelle cohérente)
    /// </summary>
    float GetPerlinValue(int chunkX, int chunkZ, float scale, float offset)
    {
        // Ajouter la seed globale pour avoir des mondes différents
        float seedOffsetX = globalSeed * 0.1f;
        float seedOffsetZ = globalSeed * 0.1f;
        
        // Calculer les coordonnées Perlin
        float x = (chunkX + seedOffsetX + offset) * scale;
        float z = (chunkZ + seedOffsetZ + offset) * scale;
        
        // Retourner la valeur Perlin (0-1)
        return Mathf.PerlinNoise(x, z);
    }

    /// <summary>
    /// Nettoie tous les chunks générés
    /// </summary>
    [ContextMenu("Clear All Chunks")]
    public void ClearAllChunks()
    {
        List<Vector2Int> allChunks = new List<Vector2Int>(loadedChunks.Keys);
        
        foreach (Vector2Int chunkCoord in allChunks)
        {
            UnloadChunk(chunkCoord);
        }
        
        Debug.Log("ProceduralWorldManager: Tous les chunks nettoyés");
    }

    /// <summary>
    /// Visualisation en mode édition
    /// </summary>
    void OnDrawGizmos()
    {
        if (playerPlane == null) return;
        
        Vector2Int playerChunk = GetChunkCoord(playerPlane.position);
        
        // Chunk actuel de l'avion
        Gizmos.color = Color.green;
        Vector3 chunkCenter = GetChunkWorldPosition(playerChunk);
        DrawChunkGizmo(chunkCenter, chunkSize);
        
        // Zone de vision (chunks à charger)
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        for (int x = -viewDistance; x <= viewDistance; x++)
        {
            for (int z = -viewDistance; z <= viewDistance; z++)
            {
                Vector2Int coord = playerChunk + new Vector2Int(x, z);
                Vector3 center = GetChunkWorldPosition(coord);
                DrawChunkGizmo(center, chunkSize);
            }
        }
        
        // Zone de déchargement
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        for (int x = -unloadDistance; x <= unloadDistance; x++)
        {
            for (int z = -unloadDistance; z <= unloadDistance; z++)
            {
                if (Mathf.Abs(x) <= viewDistance && Mathf.Abs(z) <= viewDistance)
                    continue;
                
                Vector2Int coord = playerChunk + new Vector2Int(x, z);
                Vector3 center = GetChunkWorldPosition(coord);
                DrawChunkGizmo(center, chunkSize);
            }
        }
    }

    void DrawChunkGizmo(Vector3 center, float size)
    {
        float halfSize = size / 2f;
        Vector3[] corners = new Vector3[4];
        corners[0] = center + new Vector3(-halfSize, 0f, -halfSize);
        corners[1] = center + new Vector3(halfSize, 0f, -halfSize);
        corners[2] = center + new Vector3(halfSize, 0f, halfSize);
        corners[3] = center + new Vector3(-halfSize, 0f, halfSize);
        
        Gizmos.DrawLine(corners[0], corners[1]);
        Gizmos.DrawLine(corners[1], corners[2]);
        Gizmos.DrawLine(corners[2], corners[3]);
        Gizmos.DrawLine(corners[3], corners[0]);
    }

    /// <summary>
    /// Données d'un chunk généré
    /// </summary>
    [System.Serializable]
    public class ChunkData
    {
        public Vector2Int coord;
        public Transform parent;
        public List<GameObject> generatedObjects;
    }
}
