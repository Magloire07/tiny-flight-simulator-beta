using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.EventSystems;

/// <summary>
/// Affiche un point d'impact visuel (dot + halo) à l'endroit où le laser
/// XR touche une surface (3D ou UI World Space).
/// S'auto-attache à tous les XRRayInteractor de la scène au démarrage.
/// </summary>
[RequireComponent(typeof(XRRayInteractor))]
public class XRLaserImpactDot : MonoBehaviour
{
    /// <summary>
    /// S'attache automatiquement à chaque XRRayInteractor présent dans la scène,
    /// quelle que soit la scène (MainMenu, Flight Demo, etc.).
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoAttach()
    {
        var interactors = FindObjectsOfType<XRRayInteractor>(true);
        foreach (var ri in interactors)
        {
            if (ri.GetComponent<XRLaserImpactDot>() == null)
                ri.gameObject.AddComponent<XRLaserImpactDot>();
        }
    }


    [Header("Apparence")]
    public Color dotColor      = new Color(1f, 0.25f, 0.15f, 1f);
    public Color haloColor     = new Color(1f, 0.40f, 0.20f, 0.30f);

    [Range(0.002f, 0.06f)]
    public float dotRadius     = 0.014f;
    [Range(2f, 8f)]
    public float haloScale     = 4f;
    public float surfaceOffset = 0.003f;
    [Range(1f, 30f)]
    public float fadeSpeed     = 14f;

    [Header("Raycast")]
    public float     maxRayLength = 30f;
    public LayerMask hitLayers    = ~0;

    // -----------------------------------------------------------------------

    private XRRayInteractor _interactor;
    private Transform       _dotRoot;
    private SpriteRenderer  _srDot;
    private SpriteRenderer  _srHalo;
    private float           _alpha;

    // -----------------------------------------------------------------------

    void Awake()
    {
        _interactor = GetComponent<XRRayInteractor>();
        CreateVisuals();
    }

    void OnDisable()
    {
        if (_dotRoot != null) _dotRoot.gameObject.SetActive(false);
        _alpha = 0f;
    }

    void OnEnable()
    {
        _alpha = 0f;
    }

    void Update()
    {
        bool gotHit = GetHit(out Vector3 pos, out Vector3 normal);

        _alpha = Mathf.Lerp(_alpha, gotHit ? 1f : 0f, Time.deltaTime * fadeSpeed);

        if (_alpha < 0.01f)
        {
            _dotRoot.gameObject.SetActive(false);
            return;
        }

        _dotRoot.gameObject.SetActive(true);
        _dotRoot.position = pos + normal * surfaceOffset;

        Vector3 up = Mathf.Abs(Vector3.Dot(normal, Vector3.up)) > 0.99f
                     ? Vector3.forward : Vector3.up;
        _dotRoot.rotation = Quaternion.LookRotation(-normal, up);

        Color dc = dotColor;  dc.a = _alpha;
        Color hc = haloColor; hc.a = _alpha * haloColor.a;
        _srDot.color  = dc;
        _srHalo.color = hc;
    }

    // -----------------------------------------------------------------------
    //  LECTURE DU HIT
    // -----------------------------------------------------------------------

    bool GetHit(out Vector3 position, out Vector3 normal)
    {
        position = Vector3.zero;
        normal   = Vector3.up;

        if (_interactor == null) return false;

        // 1) Hit sur UI — utilise l'API directe de XRRayInteractor (XR Toolkit 3.x)
        if (_interactor.TryGetCurrentUIRaycastResult(out RaycastResult uiHit) && uiHit.isValid)
        {
            // worldPosition peut être zéro sur certaines versions du toolkit ;
            // dans ce cas on reconstitue la position via la distance du hit
            if (uiHit.worldPosition != Vector3.zero)
                position = uiHit.worldPosition;
            else
                position = _interactor.rayOriginTransform != null
                           ? _interactor.rayOriginTransform.position + _interactor.rayOriginTransform.forward * uiHit.distance
                           : transform.position + transform.forward * uiHit.distance;

            normal = (uiHit.worldNormal != Vector3.zero) ? uiHit.worldNormal : -transform.forward;
            return true;
        }

        // 2) Hit sur surface 3D via Physics.Raycast depuis la ligne du rayon
        Vector3[] pts  = null;
        int       count = 0;
        if (_interactor.GetLinePoints(ref pts, out count) && count >= 2)
        {
            Vector3 origin = pts[0];
            Vector3 dir    = (pts[count - 1] - origin).normalized;
            float   len    = Mathf.Min(Vector3.Distance(pts[0], pts[count - 1]), maxRayLength);

            if (Physics.Raycast(origin, dir, out RaycastHit hit3D, len, hitLayers,
                                QueryTriggerInteraction.Collide))
            {
                position = hit3D.point;
                normal   = hit3D.normal;
                return true;
            }
        }

        return false;
    }

    // -----------------------------------------------------------------------
    //  CONSTRUCTION DES VISUELS
    // -----------------------------------------------------------------------

    void CreateVisuals()
    {
        _dotRoot = new GameObject("XR_ImpactDot").transform;
        _dotRoot.gameObject.SetActive(false);

        // Halo (doux, arrière)
        GameObject haloGO = new GameObject("Halo");
        haloGO.transform.SetParent(_dotRoot, false);
        haloGO.transform.localScale = Vector3.one * dotRadius * haloScale * 2f;
        _srHalo = haloGO.AddComponent<SpriteRenderer>();
        _srHalo.sprite       = MakeCircle(128, soft: true);
        _srHalo.color        = haloColor;
        _srHalo.sortingOrder = 10;
        _srHalo.material     = AdditiveMat();

        // Point central (dur, avant)
        GameObject dotGO = new GameObject("Dot");
        dotGO.transform.SetParent(_dotRoot, false);
        dotGO.transform.localPosition = new Vector3(0, 0, -0.001f);
        dotGO.transform.localScale    = Vector3.one * dotRadius * 2f;
        _srDot = dotGO.AddComponent<SpriteRenderer>();
        _srDot.sprite       = MakeCircle(128, soft: false);
        _srDot.color        = dotColor;
        _srDot.sortingOrder = 11;
        _srDot.material     = AdditiveMat();
    }

    Material AdditiveMat()
    {
        Shader sh = Shader.Find("Sprites/Additive")
                 ?? Shader.Find("Legacy Shaders/Particles/Additive")
                 ?? Shader.Find("Sprites/Default");
        return new Material(sh);
    }

    Sprite MakeCircle(int res, bool soft)
    {
        Texture2D tex = new Texture2D(res, res, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        float half = res * 0.5f, r = half - 1f;
        for (int y = 0; y < res; y++)
        for (int x = 0; x < res; x++)
        {
            float d = Mathf.Sqrt((x - half + .5f) * (x - half + .5f) +
                                 (y - half + .5f) * (y - half + .5f));
            float a = soft ? Mathf.Clamp01(1f - d / r) : (d < r ? 1f : 0f);
            tex.SetPixel(x, y, new Color(1, 1, 1, a));
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, res, res), Vector2.one * 0.5f, res);
    }

    void OnDestroy()
    {
        if (_dotRoot != null) Destroy(_dotRoot.gameObject);
    }
}
