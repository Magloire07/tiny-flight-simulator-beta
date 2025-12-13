using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Génère des lacs avec végétation environnante (arbres, rochers, herbes)
/// </summary>
public class LakeGenerator : MonoBehaviour
{
    [Header("Références Terrain")]
    [Tooltip("Terrain sur lequel générer les lacs")]
    public Terrain terrain;
    
    [Header("Lac - Eau")]
    [Tooltip("Prefab de surface d'eau (plan avec shader eau)")]
    public GameObject waterSurfacePrefab;
    
    [Tooltip("Hauteur de l'eau par rapport au sol")]
    public float waterHeight = 0.5f;
    
    [Header("Dimensions des Lacs")]
    [Tooltip("Nombre de lacs à générer")]
    [Range(1, 20)]
    public int lakeCount = 5;
    
    [Tooltip("Taille minimale d'un lac (rayon en mètres)")]
    public float minLakeRadius = 30f;
    
    [Tooltip("Taille maximale d'un lac (rayon en mètres)")]
    public float maxLakeRadius = 100f;
    
    [Tooltip("Irrégularité du contour (0-1, 0=cercle parfait, 1=très irrégulier)")]
    [Range(0f, 1f)]
    public float shapeIrregularity = 0.3f;
    
    [Header("Végétation - Arbres")]
    [Tooltip("Prefabs d'arbres autour du lac")]
    public GameObject[] treePrefabs;
    
    [Tooltip("Nombre d'arbres par lac")]
    [Range(10, 200)]
    public int treesPerLake = 50;
    
    [Tooltip("Distance minimale du bord du lac")]
    public float treeMinDistance = 2f;
    
    [Tooltip("Distance maximale du bord du lac")]
    public float treeMaxDistance = 20f;
    
    [Tooltip("Variation d'échelle des arbres (0.5 = 50% à 150%)")]
    [Range(0f, 0.5f)]
    public float treeScaleVariation = 0.2f;
    
    [Header("Végétation - Buissons")]
    [Tooltip("Prefabs de buissons")]
    public GameObject[] bushPrefabs;
    
    [Tooltip("Nombre de buissons par lac")]
    [Range(0, 100)]
    public int bushesPerLake = 30;
    
    [Tooltip("Distance minimale du bord")]
    public float bushMinDistance = 2f;
    
    [Tooltip("Distance maximale du bord")]
    public float bushMaxDistance = 20f;
    
    [Header("Décoration - Rochers")]
    [Tooltip("Prefabs de rochers")]
    public GameObject[] rockPrefabs;
    
    [Tooltip("Nombre de rochers par lac")]
    [Range(0, 50)]
    public int rocksPerLake = 15;
    
    [Tooltip("Distance minimale du bord")]
    public float rockMinDistance = 1f;
    
    [Tooltip("Distance maximale du bord")]
    public float rockMaxDistance = 15f;
    
    [Header("Zone de Génération")]
    [Tooltip("Centre de la zone de génération")]
    public Vector3 generationCenter = Vector3.zero;
    
    [Tooltip("Rayon de la zone de génération")]
    public float generationRadius = 2000f;
    
    [Tooltip("Distance minimale entre deux lacs")]
    public float minLakeSpacing = 50f;
    
    [Header("Optimisation")]
    [Tooltip("Parent pour organiser les objets générés")]
    public Transform lakesParent;
    
    [Tooltip("Graine aléatoire (0 = aléatoire)")]
    public int seed = 0;
    
    // Données internes
    private List<GameObject> generatedObjects = new List<GameObject>();
    private List<LakeData> generatedLakes = new List<LakeData>();
    private System.Random random;

    void Start()
    {
        if (terrain == null)
            terrain = Terrain.activeTerrain;
    }

    /// <summary>
    /// Génère tous les lacs avec leur végétation
    /// </summary>
    [ContextMenu("Generate Lakes")]
    public void GenerateLakes()
    {
        Debug.Log("LakeGenerator: Début de la génération des lacs...");
        
        // Initialiser le générateur aléatoire
        if (seed == 0)
            seed = Random.Range(1, 999999);
        
        random = new System.Random(seed);
        Debug.Log($"LakeGenerator: Seed = {seed}");
        
        // Nettoyer les lacs existants
        ClearLakes();
        
        // Créer le parent si nécessaire
        if (lakesParent == null)
        {
            GameObject parentObj = new GameObject("Lakes");
            lakesParent = parentObj.transform;
        }
        
        // Générer les lacs
        for (int i = 0; i < lakeCount; i++)
        {
            GenerateSingleLake(i);
        }
        
        Debug.Log($"LakeGenerator: {generatedLakes.Count} lacs générés avec {generatedObjects.Count} objets au total.");
    }

    /// <summary>
    /// Génère un lac individuel avec sa végétation
    /// </summary>
    void GenerateSingleLake(int lakeIndex)
    {
        // Trouver une position valide
        Vector3 lakePosition = FindValidLakePosition();
        if (lakePosition == Vector3.zero)
        {
            Debug.LogWarning($"LakeGenerator: Impossible de trouver une position valide pour le lac {lakeIndex}");
            return;
        }
        
        // Taille du lac
        float lakeRadius = Mathf.Lerp(minLakeRadius, maxLakeRadius, (float)random.NextDouble());
        
        // Créer le parent du lac
        GameObject lakeParentObj = new GameObject($"Lake_{lakeIndex}");
        lakeParentObj.transform.SetParent(lakesParent);
        lakeParentObj.transform.position = lakePosition;
        
        // Créer la surface d'eau
        CreateWaterSurface(lakePosition, lakeRadius, lakeParentObj.transform);
        
        // Enregistrer les données du lac
        LakeData lakeData = new LakeData
        {
            center = lakePosition,
            radius = lakeRadius,
            parent = lakeParentObj.transform
        };
        generatedLakes.Add(lakeData);
        
        // Générer la végétation autour
        GenerateTreesAroundLake(lakeData);
        GenerateBushesAroundLake(lakeData);
        GenerateRocksAroundLake(lakeData);
        
        Debug.Log($"LakeGenerator: Lac {lakeIndex} créé à {lakePosition} (rayon: {lakeRadius}m)");
    }

    /// <summary>
    /// Trouve une position valide pour un lac (évite les chevauchements)
    /// </summary>
    Vector3 FindValidLakePosition()
    {
        int maxAttempts = 50;
        
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            // Position aléatoire dans la zone
            float angle = (float)random.NextDouble() * 360f;
            float distance = (float)random.NextDouble() * generationRadius;
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
            Vector3 position = generationCenter + offset;
            
            // Ajuster la hauteur selon le terrain
            if (terrain != null)
            {
                position.y = terrain.SampleHeight(position);
            }
            
            // Vérifier la distance avec les lacs existants
            bool validPosition = true;
            foreach (LakeData existingLake in generatedLakes)
            {
                float distance2D = Vector2.Distance(
                    new Vector2(position.x, position.z),
                    new Vector2(existingLake.center.x, existingLake.center.z)
                );
                
                if (distance2D < minLakeSpacing)
                {
                    validPosition = false;
                    break;
                }
            }
            
            if (validPosition)
                return position;
        }
        
        return Vector3.zero;
    }

    /// <summary>
    /// Crée la surface d'eau du lac avec une forme organique
    /// </summary>
    void CreateWaterSurface(Vector3 center, float radius, Transform parent)
    {
        if (waterSurfacePrefab == null)
        {
            Debug.LogWarning("LakeGenerator: Aucun prefab de surface d'eau assigné!");
            return;
        }
        
        // Créer un parent pour tous les segments d'eau
        Transform waterParent = new GameObject("WaterSurface").transform;
        waterParent.SetParent(parent);
        waterParent.position = center;
        
        // Nombre de segments pour former le lac (basé sur la taille)
        int segmentCount = Mathf.CeilToInt(radius / 20f); // Un segment tous les 20m environ
        segmentCount = Mathf.Max(3, Mathf.Min(segmentCount, 8)); // Entre 3 et 8 segments
        
        // Créer un segment central
        CreateWaterSegment(center, radius * 0.6f, waterParent);
        
        // Créer des segments secondaires autour pour former une masse irrégulière
        for (int i = 0; i < segmentCount; i++)
        {
            float angle = (i / (float)segmentCount) * 360f + (float)random.NextDouble() * 30f;
            float distance = radius * (0.3f + (float)random.NextDouble() * 0.4f); // 30% à 70% du rayon
            float segmentRadius = radius * (0.4f + (float)random.NextDouble() * 0.3f); // 40% à 70% du rayon
            
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
            Vector3 segmentPosition = center + offset;
            
            CreateWaterSegment(segmentPosition, segmentRadius, waterParent);
        }
    }
    
    /// <summary>
    /// Crée un segment individuel de surface d'eau
    /// </summary>
    void CreateWaterSegment(Vector3 position, float segmentRadius, Transform parent)
    {
        // Position de l'eau
        Vector3 waterPosition = position + Vector3.up * waterHeight;
        
        // Créer la surface
        GameObject water = Instantiate(waterSurfacePrefab, waterPosition, Quaternion.identity);
        water.transform.SetParent(parent);
        
        // Ajuster l'échelle selon le rayon du segment
        float scale = segmentRadius * 2f / 10f; // Assumer que le prefab fait 10m de base
        water.transform.localScale = new Vector3(scale, 1f, scale);
        
        // Rotation aléatoire pour varier l'apparence
        float rotation = (float)random.NextDouble() * 360f;
        water.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        
        generatedObjects.Add(water);
    }

    /// <summary>
    /// Génère les arbres autour du lac
    /// </summary>
    void GenerateTreesAroundLake(LakeData lake)
    {
        if (treePrefabs == null || treePrefabs.Length == 0) return;
        
        Transform treesParent = new GameObject("Trees").transform;
        treesParent.SetParent(lake.parent);
        
        for (int i = 0; i < treesPerLake; i++)
        {
            // Position aléatoire autour du lac
            Vector3 position = GetRandomPositionAroundLake(lake, treeMinDistance, treeMaxDistance);
            
            if (position == Vector3.zero) continue;
            
            // Choisir un prefab aléatoire
            GameObject treePrefab = treePrefabs[random.Next(treePrefabs.Length)];
            
            // Créer l'arbre
            GameObject tree = Instantiate(treePrefab, position, Quaternion.identity);
            tree.transform.SetParent(treesParent);
            
            // Rotation aléatoire
            float rotation = (float)random.NextDouble() * 360f;
            tree.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            
            // Variation d'échelle
            float scaleVariation = 1f + ((float)random.NextDouble() - 0.5f) * 2f * treeScaleVariation;
            tree.transform.localScale *= scaleVariation;
            
            generatedObjects.Add(tree);
        }
    }

    /// <summary>
    /// Génère les buissons autour du lac
    /// </summary>
    void GenerateBushesAroundLake(LakeData lake)
    {
        if (bushPrefabs == null || bushPrefabs.Length == 0) return;
        
        Transform bushesParent = new GameObject("Bushes").transform;
        bushesParent.SetParent(lake.parent);
        
        for (int i = 0; i < bushesPerLake; i++)
        {
            Vector3 position = GetRandomPositionAroundLake(lake, bushMinDistance, bushMaxDistance);
            
            if (position == Vector3.zero) continue;
            
            GameObject bushPrefab = bushPrefabs[random.Next(bushPrefabs.Length)];
            GameObject bush = Instantiate(bushPrefab, position, Quaternion.identity);
            bush.transform.SetParent(bushesParent);
            
            // Rotation et échelle aléatoires
            float rotation = (float)random.NextDouble() * 360f;
            bush.transform.rotation = Quaternion.Euler(0f, rotation, 0f);
            
            float scaleVariation = 0.8f + (float)random.NextDouble() * 0.4f; // 0.8 à 1.2
            bush.transform.localScale *= scaleVariation;
            
            generatedObjects.Add(bush);
        }
    }

    /// <summary>
    /// Génère les rochers autour du lac
    /// </summary>
    void GenerateRocksAroundLake(LakeData lake)
    {
        if (rockPrefabs == null || rockPrefabs.Length == 0) return;
        
        Transform rocksParent = new GameObject("Rocks").transform;
        rocksParent.SetParent(lake.parent);
        
        for (int i = 0; i < rocksPerLake; i++)
        {
            Vector3 position = GetRandomPositionAroundLake(lake, rockMinDistance, rockMaxDistance);
            
            if (position == Vector3.zero) continue;
            
            GameObject rockPrefab = rockPrefabs[random.Next(rockPrefabs.Length)];
            GameObject rock = Instantiate(rockPrefab, position, Quaternion.identity);
            rock.transform.SetParent(rocksParent);
            
            // Rotation aléatoire complète (aussi sur X et Z pour les rochers)
            float rotX = (float)random.NextDouble() * 30f - 15f; // -15° à +15°
            float rotY = (float)random.NextDouble() * 360f;
            float rotZ = (float)random.NextDouble() * 30f - 15f; // -15° à +15°
            rock.transform.rotation = Quaternion.Euler(rotX, rotY, rotZ);
            
            // Variation d'échelle
            float scaleVariation = 0.7f + (float)random.NextDouble() * 0.6f; // 0.7 à 1.3
            rock.transform.localScale *= scaleVariation;
            
            generatedObjects.Add(rock);
        }
    }

    /// <summary>
    /// Obtient une position aléatoire autour du lac
    /// </summary>
    Vector3 GetRandomPositionAroundLake(LakeData lake, float minDistance, float maxDistance)
    {
        // Angle aléatoire
        float angle = (float)random.NextDouble() * 360f;
        
        // Distance aléatoire dans la plage
        float distance = lake.radius + Mathf.Lerp(minDistance, maxDistance, (float)random.NextDouble());
        
        // Ajouter de l'irrégularité au contour
        float irregularity = ((float)random.NextDouble() - 0.5f) * shapeIrregularity * lake.radius;
        distance += irregularity;
        
        // Calculer la position
        Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * distance;
        Vector3 position = lake.center + offset;
        
        // Ajuster la hauteur selon le terrain
        if (terrain != null)
        {
            position.y = terrain.SampleHeight(position);
        }
        
        return position;
    }

    /// <summary>
    /// Nettoie tous les lacs générés
    /// </summary>
    [ContextMenu("Clear Lakes")]
    public void ClearLakes()
    {
        foreach (GameObject obj in generatedObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        
        generatedObjects.Clear();
        generatedLakes.Clear();
        
        if (lakesParent != null && lakesParent.childCount > 0)
        {
            for (int i = lakesParent.childCount - 1; i >= 0; i--)
            {
                DestroyImmediate(lakesParent.GetChild(i).gameObject);
            }
        }
        
        Debug.Log("LakeGenerator: Lacs nettoyés");
    }

    /// <summary>
    /// Affiche les zones de génération en mode édition
    /// </summary>
    void OnDrawGizmosSelected()
    {
        // Zone de génération globale
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(generationCenter, generationRadius);
        
        // Lacs générés
        foreach (LakeData lake in generatedLakes)
        {
            // Contour du lac
            Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
            Gizmos.DrawWireSphere(lake.center, lake.radius);
            
            // Zone de végétation
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(lake.center, lake.radius + treeMaxDistance);
        }
    }

    /// <summary>
    /// Données d'un lac généré
    /// </summary>
    [System.Serializable]
    public class LakeData
    {
        public Vector3 center;
        public float radius;
        public Transform parent;
    }
}
