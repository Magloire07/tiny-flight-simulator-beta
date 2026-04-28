using UnityEngine;
using UnityEngine.EventSystems;
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
/// - Fonctionne en World Space ET Screen Space Overlay
/// </summary>
public class XRUISetup : MonoBehaviour
{
    void Awake()
    {
        SetupEventSystem();
        SetupAllCanvases();
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
}
