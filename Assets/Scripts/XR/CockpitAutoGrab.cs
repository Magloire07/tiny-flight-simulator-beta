using UnityEngine;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Visuals;

/// <summary>
/// Colle visuellement les mains VR aux poignÃ©es du volant en vue cockpit.
///
/// COMMENT Ã‡A MARCHE :
///   1. Le TrackedPoseDriver est dÃ©sactivÃ© â†’ arrÃªte le suivi physique.
///   2. [DefaultExecutionOrder(10000)] : notre LateUpdate s'exÃ©cute EN DERNIER,
///      aprÃ¨s CameraViewSwitcher (XR Origin suit l'avion) et aprÃ¨s ActionBasedController
///      (XRI place/masque le modÃ¨le de main).
///   3. On force la position MONDE du contrÃ´leur sur l'AttachPoint chaque frame.
///   4. On force SetActive(true) sur le modÃ¨le de main pour contrer le masquage XRI.
///
/// SETUP Inspector :
///   viewSwitcher             â†’ CameraViewSwitcher de la scÃ¨ne
///   leftControllerTransform  â†’ "Left Controller"  (XR Origin > Camera Offset > Left Controller)
///   rightControllerTransform â†’ "Right Controller"
///   leftHandModel            â†’ "Left Hand Model"  (enfant de Left Controller)
///   rightHandModel           â†’ "Right Hand Model" (enfant de Right Controller)
///   leftGripAttach           â†’ Transform AttachPoint poignÃ©e gauche du volant
///   rightGripAttach          â†’ Transform AttachPoint poignÃ©e droite du volant
/// </summary>
[DefaultExecutionOrder(10000)]
public class CockpitAutoGrab : MonoBehaviour
{
    [Header("RÃ©fÃ©rences")]
    public CameraViewSwitcher viewSwitcher;

    [Header("ContrÃ´leurs XR (sous XR Origin > Camera Offset)")]
    public Transform leftControllerTransform;
    public Transform rightControllerTransform;

    [Header("ModÃ¨les de mains (enfants des contrÃ´leurs)")]
    [Tooltip("'Left Hand Model' â€” enfant de Left Controller")]
    public GameObject leftHandModel;
    [Tooltip("'Right Hand Model' â€” enfant de Right Controller")]
    public GameObject rightHandModel;

    [Header("Ancre cockpit (optionnel)")]
    [Tooltip("Placez un Transform vide sur le volant. Les offsets ci-dessous sont relatifs a ce point. Si vide, utilise l'espace local de l'avion.")]
    public Transform cockpitAnchor;

    [Header("Offset des mains depuis l'ancre (ou depuis l'avion si ancre vide)")]
    public Vector3 leftHandCockpitPos  = new Vector3(-0.001f, -0.2f,  0.21f);
    public Vector3 rightHandCockpitPos = new Vector3(-0.09f,  -0.19f,  0.22f);

    [Header("Lasers XR (optionnel — auto-trouves si vide)")]
    [Tooltip("XRRayInteractor gauche (composant, pas le GO — auto-trouve si vide)")]
    public XRRayInteractor leftRayInteractor;
    [Tooltip("XRRayInteractor droit (composant, pas le GO — auto-trouve si vide)")]
    public XRRayInteractor rightRayInteractor;

    [Header("Options")]
    [Range(0f, 2f)]
    public float attachDelay = 0.15f;

    // â”€â”€ Ã©tat interne â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
    private bool wasCockpitView = false;
    private float attachTimer   = -1f;
    private bool pendingAttach  = false;
    private bool isAttached     = false;

    private TrackedPoseDriver leftTPD;
    private TrackedPoseDriver rightTPD;

    // â”€â”€ MonoBehaviour â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void Start()
    {
        if (viewSwitcher == null)
            viewSwitcher = FindObjectOfType<CameraViewSwitcher>();

        if (viewSwitcher == null)
        {
            Debug.LogWarning("[CockpitAutoGrab] CameraViewSwitcher introuvable.");
            enabled = false;
            return;
        }

        if (leftControllerTransform == null || rightControllerTransform == null)
            FindControllerTransforms();

        if (leftControllerTransform != null)
        {
            leftTPD = leftControllerTransform.GetComponent<TrackedPoseDriver>();
            if (leftHandModel == null)
                leftHandModel = FindHandModel(leftControllerTransform);
        }
        if (rightControllerTransform != null)
        {
            rightTPD = rightControllerTransform.GetComponent<TrackedPoseDriver>();
            if (rightHandModel == null)
                rightHandModel = FindHandModel(rightControllerTransform);
        }

        // Lasers — chercher automatiquement si non assignés
        FindRayInteractors();

        wasCockpitView = viewSwitcher.isCockpitView;
        if (wasCockpitView)
            ScheduleAttach();
    }

    void Update()
    {
        if (viewSwitcher == null) return;

        bool isCockpit = viewSwitcher.isCockpitView;

        if      (isCockpit && !wasCockpitView) ScheduleAttach();
        else if (!isCockpit && wasCockpitView) DetachHands();

        wasCockpitView = isCockpit;

        if (pendingAttach)
        {
            attachTimer -= Time.deltaTime;
            if (attachTimer <= 0f)
            {
                pendingAttach = false;
                AttachHands();
            }
        }
    }

    // ExÃ©cutÃ© EN DERNIER (ordre 10000) â€” aprÃ¨s CameraViewSwitcher, ActionBasedController,
    // TrackedPoseDriver, etc.
    void LateUpdate()
    {
        if (!isAttached) return;

        // Forcer la position MONDE des modèles de mains
        Transform anchor = cockpitAnchor;
        if (anchor == null && viewSwitcher != null) anchor = viewSwitcher.aircraft;

        if (leftHandModel != null)
        {
            if (anchor != null)
                leftHandModel.transform.position = anchor.TransformPoint(leftHandCockpitPos);
            if (!leftHandModel.activeSelf) leftHandModel.SetActive(true);
        }
        if (rightHandModel != null)
        {
            if (anchor != null)
                rightHandModel.transform.position = anchor.TransformPoint(rightHandCockpitPos);
            if (!rightHandModel.activeSelf) rightHandModel.SetActive(true);
        }
    }

    // â”€â”€ API publique â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public void AttachHands()
    {
        if (isAttached) return;

        // DÃ©sactiver le TPD pour stopper la mise Ã  jour de position (Ã©vite le conflit)
        if (leftTPD  != null) leftTPD.enabled  = false;
        if (rightTPD != null) rightTPD.enabled = false;
        // Masquer les lasers en cockpit (composants seulement, pas le GO)
        SetRayEnabled(leftRayInteractor,  false);
        SetRayEnabled(rightRayInteractor, false);
        isAttached = true;
        Debug.Log("[CockpitAutoGrab] Mains accrochÃ©es au volant.");
    }

    public void DetachHands()
    {
        if (!isAttached) return;

        isAttached = false;

        // RÃ©activer le tracking normal
        if (leftTPD  != null) leftTPD.enabled  = true;
        if (rightTPD != null) rightTPD.enabled = true;
        // Réafficher les lasers hors cockpit
        SetRayEnabled(leftRayInteractor,  true);
        SetRayEnabled(rightRayInteractor, true);
        Debug.Log("[CockpitAutoGrab] Mains libÃ©rÃ©es du volant.");
    }

    // â”€â”€ helpers privÃ©s â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    void ScheduleAttach()
    {
        attachTimer   = attachDelay;
        pendingAttach = true;
    }

    void FindControllerTransforms()
    {
        var origin = GameObject.Find("XR Origin (XR Rig)")
                  ?? GameObject.Find("XR Origin")
                  ?? GameObject.Find("XROrigin");

        if (origin == null)
        {
            Debug.LogWarning("[CockpitAutoGrab] XR Origin introuvable. Assignez les contrÃ´leurs manuellement.");
            return;
        }

        foreach (Transform t in origin.GetComponentsInChildren<Transform>())
        {
            string n = t.name.ToLower();
            if (leftControllerTransform  == null && n.Contains("left")  && n.Contains("controller"))
                leftControllerTransform  = t;
            else if (rightControllerTransform == null && n.Contains("right") && n.Contains("controller"))
                rightControllerTransform = t;

            if (leftControllerTransform != null && rightControllerTransform != null) break;
        }

        if (leftControllerTransform  == null) Debug.LogWarning("[CockpitAutoGrab] ContrÃ´leur gauche introuvable.");
        if (rightControllerTransform == null) Debug.LogWarning("[CockpitAutoGrab] ContrÃ´leur droit introuvable.");
    }

    void FindRayInteractors()
    {
        if (leftRayInteractor != null && rightRayInteractor != null) return;

        var rays = FindObjectsOfType<XRRayInteractor>(true);
        foreach (var r in rays)
        {
            string n = r.name.ToLower();
            if (leftRayInteractor  == null && n.Contains("left"))  leftRayInteractor  = r;
            else if (rightRayInteractor == null && n.Contains("right")) rightRayInteractor = r;
        }
    }

    static void SetRayEnabled(XRRayInteractor ray, bool value)
    {
        if (ray == null) return;
        ray.enabled = value;
        var line = ray.GetComponent<LineRenderer>();
        if (line != null) line.enabled = value;
        var visual = ray.GetComponent<XRInteractorLineVisual>();
        if (visual != null) visual.enabled = value;
        var dot = ray.GetComponent<XRLaserImpactDot>();
        if (dot != null) dot.enabled = value;
    }

    static GameObject FindHandModel(Transform root)
    {
        foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLower();
            if (n.Contains("hand model") || n.Contains("handmodel"))
                return t.gameObject;
        }
        return null;
    }
}
