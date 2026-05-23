using UnityEngine;
using MFlight;

/// <summary>
/// Permet de basculer entre vue cockpit et vue extérieure avec la touche V.
/// </summary>
public class CameraViewSwitcher : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Transform de l'avion")]
    public Transform aircraft;
    
    [Tooltip("Caméra à déplacer")]
    public Camera viewCamera;
    
    [Tooltip("Référence au MouseFlightController pour désactiver en vue cockpit")]
    public MouseFlightController mouseFlightController;

    [Header("VR Support")]
    [Tooltip("Racine XR Origin (XR Rig) — à assigner si la scène utilise la VR. En VR, c'est cet objet qui est repositionné plutôt que la caméra.")]
    public Transform xrOrigin;
    
    [Header("Vue Extérieure (actuelle)")]
    [Tooltip("Position de la caméra en vue extérieure (relative à l'avion)")]
    public Vector3 externalViewOffset = new Vector3(0f, 2f, -8f);
    
    [Tooltip("Distance de la caméra en vue extérieure")]
    public float externalViewDistance = 8f;
    
    [Header("Vue Cockpit")]
    [Tooltip("Utiliser directement l'offset par rapport à l'avion (ignore la recherche de siège)")]
    public bool useOffsetOnly = true;
    
    [Tooltip("Transform du siège pilote (utilisé seulement si useOffsetOnly = false)")]
    public Transform pilotSeatTransform;
    
    [Tooltip("Position de la caméra en vue cockpit (relative à l'avion)")]
    public Vector3 cockpitViewOffset = new Vector3(0.03f, 0.41f, 0.8f);
    
    [Tooltip("Rotation de la caméra en vue cockpit (Euler angles)")]
    public Vector3 cockpitViewRotation = new Vector3(3f, -1.13f, 0f);
    
    [Tooltip("Field of View (FOV) en vue cockpit")]
    [Range(30f, 120f)]
    public float cockpitFOV = 30f;
    
    [Header("Contrôle Vue Cockpit")]
    [Tooltip("Permettre de regarder autour avec la souris en vue cockpit")]
    public bool enableCockpitFreeLook = true;
    
    [Tooltip("Sensibilité de rotation de la caméra cockpit")]
    public float cockpitLookSensitivity = 2f;
    
    [Tooltip("Angle maximum de rotation horizontale (gauche/droite)")]
    [Range(0f, 180f)]
    public float maxYawAngle = 90f;
    
    [Tooltip("Angle maximum de rotation verticale (haut/bas)")]
    [Range(0f, 90f)]
    public float maxPitchAngle = 60f;
    
    [Tooltip("Touche pour réinitialiser la vue (regarder devant)")]
    public KeyCode recenterViewKey = KeyCode.C;
    
    [Header("Transition")]
    [Tooltip("Vitesse de transition entre les vues (0 = instantané)")]
    [Range(0f, 20f)]
    public float transitionSpeed = 10f;
    
    [Header("Contrôles")]
    [Tooltip("Touche pour changer de vue")]
    public KeyCode switchViewKey = KeyCode.V;
    
    [Header("État")]
    [Tooltip("Vue actuelle (false = extérieure, true = cockpit)")]
    public bool isCockpitView = false;
    
    [Tooltip("Vue par défaut au démarrage (false = extérieure, true = cockpit)")]
    public bool startInCockpitView = false;
    
    [Header("Fuselage (vue cockpit)")]
    [Tooltip("Renderers du fuselage à rendre double-face en vue cockpit (faces internes visibles)")]
    public Renderer[] fuselageRenderers;

    [Header("Audio")]
    [Tooltip("Son du beep lors du changement de vue")]
    public AudioClip switchViewBeep;
    
    [Tooltip("Volume du beep (0-1)")]
    [Range(0f, 1f)]
    public float beepVolume = 0.5f;
    
    [Tooltip("Pitch du beep pour vue cockpit")]
    [Range(0.5f, 2f)]
    public float cockpitBeepPitch = 1.2f;
    
    [Tooltip("Pitch du beep pour vue extérieure")]
    [Range(0.5f, 2f)]
    public float externalBeepPitch = 0.8f;
    
    // Variables internes
    private AudioSource audioSource;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private float externalViewFOV; // Sauvegarde du FOV de la vue externe
    private float cockpitYaw = 0f; // Rotation horizontale de la vue cockpit
    private float cockpitPitch = 0f; // Rotation verticale de la vue cockpit
    private GameObject[] innerFuselageObjects; // Meshes miroir (normales inversées) pour la vue cockpit
    
    void Start()
    {
        Debug.Log("CameraViewSwitcher: Initialisation...");

        CreateInnerFuselageObjects();

        if (viewCamera == null)
        {
            viewCamera = Camera.main;
            Debug.Log("CameraViewSwitcher: Caméra trouvée automatiquement: " + (viewCamera != null ? viewCamera.name : "null"));
        }

        // Chercher XR Origin automatiquement si non assigné
        if (xrOrigin == null)
        {
            var xrOriginObj = GameObject.Find("XR Origin (XR Rig)") ?? GameObject.Find("XR Origin") ?? GameObject.Find("XROrigin");
            if (xrOriginObj != null)
            {
                xrOrigin = xrOriginObj.transform;
                Debug.Log("CameraViewSwitcher: XR Origin trouvé automatiquement: " + xrOrigin.name);
            }
        }
        
        if (aircraft == null)
        {
            Debug.LogError("CameraViewSwitcher: Aucune référence aircraft assignée!");
            enabled = false;
            return;
        }
        
        // Chercher le siège pilote dans l'avion si non assigné ET si useOffsetOnly est false
        if (!useOffsetOnly && pilotSeatTransform == null)
        {
            // Chercher un Transform nommé "pilot", "seat", "cockpit" (mais pas "camera" pour éviter confusion)
            Transform[] children = aircraft.GetComponentsInChildren<Transform>();
            foreach (Transform child in children)
            {
                string nameLower = child.name.ToLower();
                if (nameLower.Contains("pilot") || nameLower.Contains("seat") || nameLower.Contains("cockpit"))
                {
                    pilotSeatTransform = child;
                    Debug.Log("CameraViewSwitcher: Siège pilote trouvé automatiquement: " + child.name);
                    break;
                }
            }
            
            if (pilotSeatTransform == null)
            {
                Debug.LogWarning("CameraViewSwitcher: Aucun siège pilote trouvé. Utilisation de cockpitViewOffset par rapport à l'avion.");
            }
        }
        else if (useOffsetOnly)
        {
            Debug.Log("CameraViewSwitcher: Mode Offset Only activé - utilisation de cockpitViewOffset par rapport à l'avion.");
        }
        else if (pilotSeatTransform != null)
        {
            Debug.Log("CameraViewSwitcher: Siège pilote assigné: " + pilotSeatTransform.name);
        }
        
        if (mouseFlightController == null)
        {
            mouseFlightController = FindObjectOfType<MouseFlightController>();
            if (mouseFlightController != null)
            {
                Debug.Log("CameraViewSwitcher: MouseFlightController trouvé: " + mouseFlightController.name);
            }
        }
        
        // Créer l'AudioSource pour le beep
        audioSource = gameObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // Son 2D
            audioSource.volume = beepVolume;
        }
        
        // Sauvegarder le FOV initial de la vue externe
        if (viewCamera != null)
        {
            externalViewFOV = viewCamera.fieldOfView;
            Debug.Log("CameraViewSwitcher: FOV vue externe sauvegardé: " + externalViewFOV);
        }
        
        // Appliquer la vue par défaut au démarrage
        isCockpitView = startInCockpitView;
        
        // Initialiser avec la vue actuelle
        if (isCockpitView)
        {
            SetCockpitView(true);
        }
        else
        {
            SetExternalView(true);
        }
        
        Debug.Log("CameraViewSwitcher: Initialisation terminée. Vue par défaut: " + (isCockpitView ? "Cockpit" : "Extérieure") + ". Appuyez sur " + switchViewKey + " pour changer de vue.");
    }
    
    void Update()
    {
        // Détecter l'appui sur la touche V
        if (Input.GetKeyDown(switchViewKey))
        {
            Debug.Log("CameraViewSwitcher: Touche " + switchViewKey + " appuyée!");
            ToggleView();
        }
        
        // Gestion du free look en vue cockpit
        if (isCockpitView && enableCockpitFreeLook)
        {
            HandleCockpitFreeLook();
        }
    }
    
    void LateUpdate()
    {
        // Mise à jour de la position/rotation de la caméra APRÈS le physics update
        UpdateCameraTransform();
    }
    
    /// <summary>
    /// Gère le free look avec la souris en vue cockpit
    /// </summary>
    void HandleCockpitFreeLook()
    {
        // Récupérer le mouvement de la souris
        float mouseX = Input.GetAxis("Mouse X") * cockpitLookSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * cockpitLookSensitivity;
        
        // Appliquer la rotation
        cockpitYaw += mouseX;
        cockpitPitch -= mouseY; // Inverser pour que haut = regarder haut
        
        // Limiter les angles
        cockpitYaw = Mathf.Clamp(cockpitYaw, -maxYawAngle, maxYawAngle);
        cockpitPitch = Mathf.Clamp(cockpitPitch, -maxPitchAngle, maxPitchAngle);
        
        // Réinitialiser la vue avec la touche C
        if (Input.GetKeyDown(recenterViewKey))
        {
            cockpitYaw = 0f;
            cockpitPitch = 0f;
        }
    }
    
    /// <summary>
    /// Active directement la vue cockpit (sans toggle).
    /// Appelable depuis d'autres scripts (ex: RunwayMenuController).
    /// </summary>
    public void ActivateCockpitView()
    {
        // Toujours forcer le repositionnement, même si isCockpitView est déjà true
        isCockpitView = true;
        SetCockpitView(true);
        PlaySwitchBeep(true);
    }

    /// <summary>
    /// Bascule entre vue cockpit et vue extérieure
    /// </summary>
    public void ToggleView()
    {
        Debug.Log($">>> ToggleView() appelé - isCockpitView AVANT: {isCockpitView}");
        isCockpitView = !isCockpitView;
        Debug.Log($">>> ToggleView() - isCockpitView APRÈS toggle: {isCockpitView}");
        
        if (isCockpitView)
        {
            Debug.Log(">>> Appel SetCockpitView(true)");
            SetCockpitView(true); // Changement instantané
            PlaySwitchBeep(true);
            Debug.Log(">>> Vue Cockpit activée (instant)");
        }
        else
        {
            Debug.Log(">>> Appel SetExternalView(true)");
            SetExternalView(true); // Changement instantané
            PlaySwitchBeep(false);
            Debug.Log(">>> Vue Extérieure activée (instant)");
        }
        
        // Forcer une mise à jour immédiate
        if (viewCamera != null)
        {
            Debug.Log($">>> Caméra position: {viewCamera.transform.position}, rotation: {viewCamera.transform.rotation.eulerAngles}");
        }
    }
    
    /// <summary>
    /// Joue le son de changement de vue
    /// </summary>
    void PlaySwitchBeep(bool toCockpit)
    {
        Debug.Log($"*** PlaySwitchBeep appelé! toCockpit={toCockpit}, Time={Time.time}, StackTrace={System.Environment.StackTrace}");
        
        if (audioSource == null) return;
        
        if (switchViewBeep != null)
        {
            // Pitch différent selon la vue
            audioSource.pitch = toCockpit ? cockpitBeepPitch : externalBeepPitch;
            audioSource.PlayOneShot(switchViewBeep, beepVolume);
        }
        else
        {
            // Beep synthétique si aucun AudioClip n'est assigné
            audioSource.pitch = toCockpit ? cockpitBeepPitch : externalBeepPitch;
            audioSource.PlayOneShot(GenerateBeep(), beepVolume);
        }
    }
    
    /// <summary>
    /// Génère un beep synthétique simple
    /// </summary>
    AudioClip GenerateBeep()
    {
        int sampleRate = 44100;
        int samples = sampleRate / 10; // 0.1 seconde
        AudioClip beep = AudioClip.Create("Beep", samples, 1, sampleRate, false);
        
        float[] data = new float[samples];
        float frequency = 800f; // Hz
        
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            // Onde sinusoïdale avec enveloppe
            float envelope = 1f - (float)i / samples; // Décroissance
            data[i] = Mathf.Sin(2f * Mathf.PI * frequency * t) * envelope * 0.5f;
        }
        
        beep.SetData(data, 0);
        return beep;
    }

    /// <summary>
    /// Passe les matériaux du fuselage en double-face (Cull Off) pour la vue cockpit.
    /// </summary>
    /// <summary>
    /// Crée, pour chaque renderer du fuselage, un GameObject enfant avec le même mesh
    /// mais les normales/triangles inversés, afin de rendre les faces intérieures.
    /// </summary>
    void CreateInnerFuselageObjects()
    {
        if (fuselageRenderers == null || fuselageRenderers.Length == 0) return;

        innerFuselageObjects = new GameObject[fuselageRenderers.Length];

        for (int i = 0; i < fuselageRenderers.Length; i++)
        {
            if (fuselageRenderers[i] == null) continue;

            MeshFilter mf = fuselageRenderers[i].GetComponent<MeshFilter>();
            if (mf == null || mf.sharedMesh == null) continue;

            GameObject inner = new GameObject(fuselageRenderers[i].name + "_inner");
            inner.transform.SetParent(fuselageRenderers[i].transform, false);
            inner.transform.localPosition = Vector3.zero;
            inner.transform.localRotation = Quaternion.identity;
            // Scale négatif sur X : inverse le winding order → faces intérieures visibles
            // sans nécessiter Read/Write sur le mesh.
            inner.transform.localScale = new Vector3(-1f, 1f, 1f);

            MeshFilter innerMF = inner.AddComponent<MeshFilter>();
            innerMF.sharedMesh = mf.sharedMesh; // même mesh, pas de copie

            MeshRenderer innerMR = inner.AddComponent<MeshRenderer>();
            innerMR.sharedMaterials = fuselageRenderers[i].sharedMaterials;

            inner.SetActive(false);
            innerFuselageObjects[i] = inner;
        }
    }

    /// <summary>
    /// Retourne une copie du mesh avec les normales et l'ordre des triangles inversés.
    /// </summary>
    Mesh FlipNormals(Mesh original)
    {
        Mesh flipped = UnityEngine.Object.Instantiate(original);
        flipped.name = original.name + "_flipped";

        Vector3[] normals = flipped.normals;
        for (int k = 0; k < normals.Length; k++)
            normals[k] = -normals[k];
        flipped.normals = normals;

        for (int s = 0; s < flipped.subMeshCount; s++)
        {
            int[] tris = flipped.GetTriangles(s);
            for (int k = 0; k < tris.Length; k += 3)
            {
                int tmp = tris[k];
                tris[k] = tris[k + 2];
                tris[k + 2] = tmp;
            }
            flipped.SetTriangles(tris, s);
        }

        return flipped;
    }

    void SetFuselageDoubleSided(bool doubleSided)
    {
        if (innerFuselageObjects == null) return;
        for (int i = 0; i < innerFuselageObjects.Length; i++)
        {
            if (innerFuselageObjects[i] != null)
                innerFuselageObjects[i].SetActive(doubleSided);
        }
    }

    /// <summary>
    /// Configure la vue cockpit
    /// </summary>
    void SetCockpitView(bool instant)
    {
        // Réinitialiser le free look
        cockpitYaw = 0f;
        cockpitPitch = 0f;
        
        // Utiliser l'offset relatif à l'avion (suit toujours le Rigidbody)
        if (useOffsetOnly || pilotSeatTransform == null)
        {
            targetPosition = aircraft.TransformPoint(cockpitViewOffset);
            targetRotation = aircraft.rotation * Quaternion.Euler(cockpitViewRotation);
            Debug.Log("CameraViewSwitcher: Position cockpit depuis offset (suit l'avion): " + cockpitViewOffset);
        }
        else
        {
            // Utiliser le siège pilote + offset
            targetPosition = pilotSeatTransform.position + pilotSeatTransform.TransformDirection(cockpitViewOffset);
            targetRotation = pilotSeatTransform.rotation * Quaternion.Euler(cockpitViewRotation);
            Debug.Log("CameraViewSwitcher: Position cockpit depuis siège pilote + offset: " + targetPosition);
        }
        
        // Désactiver MouseFlightController en vue cockpit
        if (mouseFlightController != null)
        {
            mouseFlightController.enabled = false;
            Debug.Log("CameraViewSwitcher: MouseFlightController désactivé");
        }
        
        // Appliquer le FOV cockpit (désactivé en VR, le headset contrôle le FOV)
        if (viewCamera != null && !UnityEngine.XR.XRSettings.enabled)
        {
            viewCamera.fieldOfView = cockpitFOV;
        }
        
        if (instant)
            ApplyCameraTransform(targetPosition, targetRotation);
    }
    
    /// <summary>
    /// Configure la vue extérieure
    /// </summary>
    void SetExternalView(bool instant)
    {
        // Réactiver MouseFlightController en vue extérieure
        if (mouseFlightController != null)
        {
            mouseFlightController.enabled = true;
        }

        // Restaurer le FOV de la vue externe (désactivé en VR, le headset contrôle le FOV)
        if (viewCamera != null && !UnityEngine.XR.XRSettings.enabled)
        {
            viewCamera.fieldOfView = externalViewFOV;
        }
        
        // Si MouseFlightController gère la caméra, ne rien faire
        // Sinon, positionner manuellement
        if (mouseFlightController == null)
        {
            targetPosition = aircraft.position + aircraft.TransformDirection(externalViewOffset);
            targetRotation = Quaternion.LookRotation(aircraft.position - targetPosition);
            
            if (instant)
                ApplyCameraTransform(targetPosition, targetRotation);
        }
    }
    
    /// <summary>
    /// Met à jour la position et rotation de la caméra avec interpolation
    /// </summary>
    void UpdateCameraTransform()
    {
        if (viewCamera == null || aircraft == null) return;
        
        if (isCockpitView)
        {
            // Vue cockpit: la caméra suit rigidement l'avion (pas d'interpolation pour éviter décalage)
            if (useOffsetOnly || pilotSeatTransform == null)
            {
                targetPosition = aircraft.TransformPoint(cockpitViewOffset);
                // Rotation de base (avion + réglage cockpit)
                Quaternion baseRotation = aircraft.rotation * Quaternion.Euler(cockpitViewRotation);
                // Free look en espace monde : yaw autour de l'axe Y monde, pitch autour de l'axe X monde
                Quaternion worldYaw   = Quaternion.AngleAxis(cockpitYaw,   Vector3.up);
                Quaternion worldPitch = Quaternion.AngleAxis(cockpitPitch, Vector3.right);
                targetRotation = worldYaw * worldPitch * baseRotation;
            }
            else
            {
                // Utiliser le siège pilote + offset
                targetPosition = pilotSeatTransform.position + pilotSeatTransform.TransformDirection(cockpitViewOffset);
                Quaternion baseRotation = pilotSeatTransform.rotation * Quaternion.Euler(cockpitViewRotation);
                Quaternion worldYaw   = Quaternion.AngleAxis(cockpitYaw,   Vector3.up);
                Quaternion worldPitch = Quaternion.AngleAxis(cockpitPitch, Vector3.right);
                targetRotation = worldYaw * worldPitch * baseRotation;
            }
            
            // Application directe sans Lerp pour éviter l'effet d'inertie
            ApplyCameraTransform(targetPosition, targetRotation);
            
            // Maintenir le FOV cockpit (désactivé en VR, le headset contrôle le FOV)
            if (!UnityEngine.XR.XRSettings.enabled)
                viewCamera.fieldOfView = cockpitFOV;
        }
        else
        {
            // Vue extérieure: MouseFlightController gère la caméra
            // Ne rien faire ici
        }
    }
    
    /// <summary>
    /// Applique la position/rotation soit au XR Origin (VR), soit à la caméra (non-VR).
    /// </summary>
    void ApplyCameraTransform(Vector3 pos, Quaternion rot)
    {
        // Toujours utiliser le XR Origin s'il est assigné.
        // XRSettings.enabled est false avec le simulateur XR mais le Tracked Pose Driver
        // écrase viewCamera.transform.position chaque frame → la caméra ne bougera jamais
        // si on essaie de la déplacer directement.
        if (xrOrigin != null)
        {
            // Compensate pour l'offset de tracking de la tête dans le rig (3D complet)
            // afin que les YEUX atterrissent exactement sur 'pos' et non pos + headOffset.
            // On garde les 3 composantes (y compris y) pour que la rotation ne déplace pas
            // la caméra — sinon rot * (0, head_y, 0) varie avec la rotation et crée un effet
            // d'orbite autour de cockpitViewOffset au lieu d'une rotation sur place.
            if (viewCamera != null)
            {
                Vector3 headLocal = xrOrigin.InverseTransformPoint(viewCamera.transform.position);
                xrOrigin.position = pos - rot * headLocal;
            }
            else
            {
                xrOrigin.position = pos;
            }
            xrOrigin.rotation = rot;
        }
        else if (viewCamera != null)
        {
            viewCamera.transform.position = pos;
            viewCamera.transform.rotation = rot;
        }
    }

    /// <summary>
    /// Debug: dessiner les positions de caméra dans l'éditeur
    /// </summary>
    void OnDrawGizmosSelected()
    {
        if (aircraft == null) return;
        
        // Vue cockpit (vert)
        Gizmos.color = Color.green;
        Vector3 cockpitPos = aircraft.TransformPoint(cockpitViewOffset);
        Gizmos.DrawWireSphere(cockpitPos, 0.2f);
        Gizmos.DrawLine(aircraft.position, cockpitPos);
        
        // Vue extérieure (bleu)
        Gizmos.color = Color.blue;
        Vector3 externalPos = aircraft.position + aircraft.TransformDirection(externalViewOffset);
        Gizmos.DrawWireSphere(externalPos, 0.3f);
        Gizmos.DrawLine(aircraft.position, externalPos);
    }
}
