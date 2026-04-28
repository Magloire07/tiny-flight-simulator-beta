using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Configure le Canvas du menu principal en World Space et construit
/// un environnement 3D stylé autour (sol grille, particules, lumières ambiantes).
///
/// SETUP UNITY :
/// 1. Attacher ce script à un GameObject vide "MenuEnvironment" dans la scène du menu
/// 2. Assigner "menuCanvas" (le Canvas du menu principal) dans l'Inspector
/// 3. S'assurer qu'une Camera "Main Camera" existe dans la scène
/// 4. Lancer - l'environnement se génère automatiquement
///
/// PARAMETRES :
/// - Toutes les couleurs / tailles sont ajustables dans l'Inspector
/// - "Rebuild Environment" (ContextMenu) permet de régénérer sans relancer
/// </summary>
public class MenuEnvironment3D : MonoBehaviour
{
    // -----------------------------------------------------------------------
    //  REFERENCES
    // -----------------------------------------------------------------------
    [Header("Canvas du menu")]
    [Tooltip("Canvas principal du menu (sera converti en World Space)")]
    public Canvas menuCanvas;

    [Tooltip("Distance entre la caméra et le panneau UI (en unités Unity)")]
    public float panelDistance = 3.5f;

    [Tooltip("Hauteur du panneau UI par rapport à l'origine")]
    public float panelHeight = 0.6f;

    // -----------------------------------------------------------------------
    //  SOL / ENVIRONNEMENT
    // -----------------------------------------------------------------------
    [Header("Sol")]
    [Tooltip("Rayon du sol (en unités Unity)")]
    public float floorRadius = 22f;

    [Tooltip("Couleur des lignes de la grille")]
    public Color gridColor = new Color(0.20f, 0.50f, 1.00f, 1f);

    [Tooltip("Couleur du fond du sol")]
    public Color bgColor   = new Color(0.03f, 0.04f, 0.07f, 1f);

    // -----------------------------------------------------------------------
    //  PARTICLES
    // -----------------------------------------------------------------------
    [Header("Particules ambiantes")]
    [Tooltip("Nombre de particules flottantes")]
    [Range(20, 200)]
    public int particleCount = 80;

    [Tooltip("Couleur des particules")]
    public Color particleColor = new Color(0.35f, 0.65f, 1.00f, 0.7f);

    [Tooltip("Rayon de spawn des particules autour du centre")]
    public float particleSpawnRadius = 10f;

    [Tooltip("Hauteur max des particules")]
    public float particleMaxHeight = 5f;

    // -----------------------------------------------------------------------
    //  ECLAIRAGES
    // -----------------------------------------------------------------------
    [Header("Eclairages")]
    [Tooltip("Couleur de la lumière d'ambiance principale (teinte bleue)")]
    public Color ambientLightColor = new Color(0.04f, 0.06f, 0.12f);

    [Tooltip("Couleur de la lumière accentuée sur le panneau")]
    public Color panelLightColor = new Color(0.30f, 0.55f, 1.00f);

    [Tooltip("Intensité de la lumière sur le panneau")]
    [Range(0f, 3f)]
    public float panelLightIntensity = 0.8f;

    // -----------------------------------------------------------------------
    //  PRIVÉ
    // -----------------------------------------------------------------------
    private GameObject    _envRoot;
    private ParticleSystem _particles;

    // -----------------------------------------------------------------------

    void Start()
    {
        BuildEnvironment();
        SetupWorldSpaceCanvas();
    }

    // -----------------------------------------------------------------------
    //  CONSTRUCTION
    // -----------------------------------------------------------------------

    [ContextMenu("Reconstruire l'environnement")]
    public void BuildEnvironment()
    {
        // Nettoyer l'ancienne construction
        if (_envRoot != null)
            DestroyImmediate(_envRoot);

        _envRoot = new GameObject("_MenuEnv3D");

        SetupCamera();
        BuildFloor();
        BuildAmbientLights();
        BuildParticles();

        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = ambientLightColor;
        RenderSettings.fog          = true;
        RenderSettings.fogMode      = FogMode.ExponentialSquared;
        RenderSettings.fogColor     = new Color(0.04f, 0.05f, 0.10f);
        RenderSettings.fogDensity   = 0.04f;
    }

    // -----------------------------------------------------------------------
    //  CAMERA
    // -----------------------------------------------------------------------

    void SetupCamera()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        // Caméra à hauteur des yeux, légèrement inclinée vers le bas pour voir le sol
        cam.transform.position = new Vector3(0f, panelHeight, -panelDistance);
        cam.transform.rotation = Quaternion.Euler(8f, 0f, 0f); // légère inclinaison bas
        cam.backgroundColor    = new Color(0.03f, 0.04f, 0.08f);
        cam.clearFlags         = CameraClearFlags.SolidColor;
        cam.nearClipPlane      = 0.1f;
        cam.farClipPlane       = 200f;
    }

    // -----------------------------------------------------------------------
    //  CANVAS WORLD SPACE
    // -----------------------------------------------------------------------

    void SetupWorldSpaceCanvas()
    {
        if (menuCanvas == null)
        {
            Debug.LogWarning("[MenuEnvironment3D] Aucun Canvas assigné !");
            return;
        }

        Camera cam = Camera.main;

        // Convertir en World Space
        menuCanvas.renderMode       = RenderMode.WorldSpace;
        menuCanvas.worldCamera      = cam;

        // Trouver la taille actuelle du RectTransform
        RectTransform rt = menuCanvas.GetComponent<RectTransform>();

        // Mise à l'échelle pour que le panneau ait ~2 unités de haut
        float targetHeight = 2.4f; // unités Unity
        float scale = targetHeight / rt.rect.height;
        menuCanvas.transform.localScale = Vector3.one * scale;

        // Positionner le Canvas devant la caméra (dans la direction +Z)
        if (cam != null)
        {
            menuCanvas.transform.position = new Vector3(0f, panelHeight, 0f);
            menuCanvas.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        }

        // Lumière Point sur le panneau
        GameObject lightObj = new GameObject("PanelLight");
        lightObj.transform.SetParent(_envRoot.transform);
        lightObj.transform.position = menuCanvas.transform.position
                                    + Vector3.back * 1.5f;
        Light pl = lightObj.AddComponent<Light>();
        pl.type      = LightType.Point;
        pl.color     = panelLightColor;
        pl.intensity = panelLightIntensity;
        pl.range     = 6f;
    }

    // -----------------------------------------------------------------------
    //  SOL
    // -----------------------------------------------------------------------

    void BuildFloor()
    {
        // Disque procédural
        int   segments = 64;
        float r        = floorRadius;

        Mesh mesh = new Mesh();
        mesh.name = "FloorDisc";

        Vector3[] verts  = new Vector3[segments + 1];
        int[]     tris   = new int[segments * 3];

        verts[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float a = i / (float)segments * Mathf.PI * 2f;
            verts[i + 1] = new Vector3(Mathf.Cos(a) * r, 0, Mathf.Sin(a) * r);
        }

        for (int i = 0; i < segments; i++)
        {
            tris[i * 3 + 0] = 0;
            tris[i * 3 + 1] = (i + 1) % segments + 1;
            tris[i * 3 + 2] = i + 1;
        }

        mesh.vertices  = verts;
        mesh.triangles = tris;
        mesh.RecalculateNormals();

        GameObject floor = new GameObject("Floor");
        floor.transform.SetParent(_envRoot.transform);
        floor.transform.position = new Vector3(0f, -0.01f, 0f);

        MeshFilter   mf  = floor.AddComponent<MeshFilter>();
        MeshRenderer mr  = floor.AddComponent<MeshRenderer>();
        mf.sharedMesh    = mesh;

        Shader gridShader = Shader.Find("Custom/GridFloor");
        if (gridShader != null)
        {
            Material mat = new Material(gridShader);
            mat.SetColor("_GridColor", gridColor);
            mat.SetColor("_BgColor",   bgColor);
            mat.SetFloat("_FadeRadius", r);
            mat.SetFloat("_GlowRadius", r * 0.3f);
            mr.sharedMaterial = mat;
        }
        else
        {
            // Fallback si shader absent : matériau standard sombre
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = bgColor;
            mr.sharedMaterial = mat;
            Debug.LogWarning("[MenuEnvironment3D] Shader 'Custom/GridFloor' introuvable, sol de remplacement appliqué.");
        }

        mr.shadowCastingMode    = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows       = false;
    }

    // -----------------------------------------------------------------------
    //  PILIERS DÉCORATIFS
    // -----------------------------------------------------------------------

    void BuildPillarRing()
    {
        int   count  = 8;
        float radius = floorRadius * 0.7f;
        float height = 4f;

        Material mat = new Material(Shader.Find("Standard"));
        mat.color         = new Color(0.06f, 0.08f, 0.14f);
        mat.SetFloat("_Metallic",   0.8f);
        mat.SetFloat("_Glossiness", 0.6f);

        // Matériau émissif pour le haut des piliers
        Material topMat = new Material(Shader.Find("Standard"));
        topMat.color                 = new Color(0.10f, 0.20f, 0.40f);
        topMat.SetColor("_EmissionColor", gridColor * 1.5f);
        topMat.EnableKeyword("_EMISSION");

        for (int i = 0; i < count; i++)
        {
            float angle = i / (float)count * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);

            // Corps du pilier
            GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pillar.name = $"Pillar_{i}";
            pillar.transform.SetParent(_envRoot.transform);
            pillar.transform.position   = pos + Vector3.up * (height * 0.5f);
            pillar.transform.localScale = new Vector3(0.18f, height * 0.5f, 0.18f);
            pillar.GetComponent<MeshRenderer>().sharedMaterial = mat;
            DestroyImmediate(pillar.GetComponent<Collider>());

            // Capsule lumineuse au sommet
            GameObject top = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            top.name = $"PillarTop_{i}";
            top.transform.SetParent(_envRoot.transform);
            top.transform.position   = pos + Vector3.up * (height + 0.12f);
            top.transform.localScale = Vector3.one * 0.25f;
            top.GetComponent<MeshRenderer>().sharedMaterial = topMat;
            DestroyImmediate(top.GetComponent<Collider>());

            // Point light sur chaque sommet
            GameObject lightObj = new GameObject($"PillarLight_{i}");
            lightObj.transform.SetParent(_envRoot.transform);
            lightObj.transform.position = pos + Vector3.up * (height + 0.2f);
            Light pl = lightObj.AddComponent<Light>();
            pl.type      = LightType.Point;
            pl.color     = Color.Lerp(gridColor, Color.white, 0.3f);
            pl.intensity = 0.35f;
            pl.range     = 5f;
            pl.shadows   = LightShadows.None;
        }
    }

    // -----------------------------------------------------------------------
    //  ECLAIRAGES AMBIANTS
    // -----------------------------------------------------------------------

    void BuildAmbientLights()
    {
        // Lumière directionnelle très douce (direction du "ciel")
        GameObject dirLightObj = new GameObject("AmbientDir");
        dirLightObj.transform.SetParent(_envRoot.transform);
        dirLightObj.transform.rotation = Quaternion.Euler(55f, 30f, 0f);
        Light dirLight = dirLightObj.AddComponent<Light>();
        dirLight.type      = LightType.Directional;
        dirLight.color     = new Color(0.15f, 0.20f, 0.35f);
        dirLight.intensity = 0.4f;
        dirLight.shadows   = LightShadows.None;

        // Lumière de remplissage (contre-jour bleu pâle)
        GameObject fillObj = new GameObject("FillLight");
        fillObj.transform.SetParent(_envRoot.transform);
        fillObj.transform.rotation = Quaternion.Euler(-30f, -150f, 0f);
        Light fill = fillObj.AddComponent<Light>();
        fill.type      = LightType.Directional;
        fill.color     = new Color(0.08f, 0.14f, 0.25f);
        fill.intensity = 0.25f;
        fill.shadows   = LightShadows.None;
    }

    // -----------------------------------------------------------------------
    //  PARTICULES
    // -----------------------------------------------------------------------

    void BuildParticles()
    {
        GameObject ps = new GameObject("AmbientParticles");
        ps.transform.SetParent(_envRoot.transform);
        ps.transform.position = Vector3.zero;

        _particles = ps.AddComponent<ParticleSystem>();
        var main  = _particles.main;
        main.loop            = true;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(4f, 12f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(0.02f, 0.15f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.015f, 0.06f);
        main.startColor      = new ParticleSystem.MinMaxGradient(
                                    new Color(particleColor.r, particleColor.g, particleColor.b, 0.3f),
                                    new Color(1f, 1f, 1f, 0.6f));
        main.maxParticles    = particleCount;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        // Emission
        var emission = _particles.emission;
        emission.rateOverTime = particleCount / 8f;

        // Forme : sphère aplatie = disque autour du joueur
        var shape = _particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale     = new Vector3(particleSpawnRadius * 2f, particleMaxHeight, particleSpawnRadius * 2f);

        // Vélocité : lente dérive vers le haut
        var velocity = _particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space   = ParticleSystemSimulationSpace.World;
        // X, Y, Z doivent tous être dans le même mode (TwoConstants)
        velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.03f, 0.12f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Taille au cours de la vie : fondu entrée/sortie
        var sizeOL = _particles.sizeOverLifetime;
        sizeOL.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.15f, 1f),
            new Keyframe(0.85f, 1f),
            new Keyframe(1f, 0f));
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        // Renderer
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.material         = CreateParticleMaterial();
        renderer.renderMode       = ParticleSystemRenderMode.Billboard;
        renderer.sortingFudge     = -10f;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows   = false;

        _particles.Play();
    }

    Material CreateParticleMaterial()
    {
        // Utilise Particles/Standard Unlit si disponible, sinon Sprites/Default
        Shader sh = Shader.Find("Particles/Standard Unlit")
                 ?? Shader.Find("Sprites/Default")
                 ?? Shader.Find("Standard");

        Material mat = new Material(sh);
        mat.SetColor("_TintColor", particleColor);
        // Pour "Particles/Standard Unlit" / "Legacy Shaders/Particles/Additive"
        try { mat.SetFloat("_Mode", 4); } catch { }  // Additive blending
        mat.renderQueue = 3000;
        return mat;
    }

    // -----------------------------------------------------------------------
    //  NETTOYAGE
    // -----------------------------------------------------------------------

    void OnDestroy()
    {
        // Remettre le Canvas en Screen Space si l'objet est détruit
        if (menuCanvas != null)
            menuCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
    }
}
