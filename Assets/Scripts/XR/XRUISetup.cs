using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// Configure automatiquement tous les Canvas de la scène pour être
/// interactables avec les manettes XR (Meta Quest).
///
/// SETUP :
/// 1. Ajouter ce script sur n'importe quel GameObject de la scène du menu
///    (ex: le même "XRMenuInput" ou "MenuEnvironment")
/// 2. Lancer — tout est configuré automatiquement
///
/// Ce script :
/// - Remplace GraphicRaycaster par TrackedDeviceGraphicRaycaster sur chaque Canvas
/// - Remplace StandaloneInputModule par XRUIInputModule sur l'EventSystem
/// - Convertit les Canvas ScreenSpaceOverlay en WorldSpace quand la VR est active
/// </summary>
public class XRUISetup : MonoBehaviour
{
    [Tooltip("Distance devant la caméra pour les canvas HUD convertis en WorldSpace (mètres)")]
    public float hudWorldDistance = 2f;

    [Tooltip("Echelle appliquée aux canvas HUD convertis (pixels → mètres)")]
    public float hudWorldScale = 0.001f;

    void Awake()
    {
        SetupEventSystem();
        SetupAllCanvases();
        if (XRSettings.enabled)
            ConvertOverlayCanvasesToWorldSpace();
    }

    void SetupEventSystem()
    {
        EventSystem es = FindObjectOfType<EventSystem>();
        if (es == null)
        {
            Debug.LogWarning("[XRUISetup] Aucun EventSystem trouvé dans la scène !");
            return;
        }

        // Remplacer StandaloneInputModule par XRUIInputModule
        StandaloneInputModule standalone = es.GetComponent<StandaloneInputModule>();
        if (standalone != null)
            Destroy(standalone);

        if (es.GetComponent<XRUIInputModule>() == null)
        {
            es.gameObject.AddComponent<XRUIInputModule>();
            Debug.Log("[XRUISetup] XRUIInputModule ajouté à l'EventSystem.");
        }
    }

    void SetupAllCanvases()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        int count = 0;

        foreach (Canvas canvas in canvases)
        {
            // Remplacer GraphicRaycaster standard par TrackedDeviceGraphicRaycaster
            var old = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (old != null)
                Destroy(old);

            if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
                count++;
            }
        }

        Debug.Log($"[XRUISetup] TrackedDeviceGraphicRaycaster ajouté sur {count} Canvas.");
    }

    void ConvertOverlayCanvasesToWorldSpace()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogWarning("[XRUISetup] Aucune Main Camera trouvée — impossible de convertir les Canvas overlay en WorldSpace.");
            return;
        }

        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        int converted = 0;

        foreach (Canvas canvas in canvases)
        {
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                continue;

            canvas.renderMode = RenderMode.WorldSpace;

            // Attacher au GameObject de la caméra pour suivre le regard
            canvas.transform.SetParent(mainCam.transform, false);

            // Positionner devant la caméra
            canvas.transform.localPosition = new Vector3(0f, 0f, hudWorldDistance);
            canvas.transform.localRotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one * hudWorldScale;

            Debug.Log($"[XRUISetup] Canvas '{canvas.name}' converti en WorldSpace et attaché à la caméra.");
            converted++;
        }

        Debug.Log($"[XRUISetup] {converted} Canvas ScreenSpaceOverlay convertis en WorldSpace pour la VR.");
    }
}
