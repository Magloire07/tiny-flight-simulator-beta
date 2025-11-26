using UnityEngine;

/// <summary>
/// Force la caméra à afficher le skybox en s'assurant que les Clear Flags sont correctement configurés.
/// Utile quand CloudMaster est désactivé ou pour garantir que le skybox est toujours visible.
/// </summary>
[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class ForceSkyboxCamera : MonoBehaviour
{
    private Camera cam;

    void Awake()
    {
        cam = GetComponent<Camera>();
        ApplySkyboxSettings();
    }

    void OnEnable()
    {
        ApplySkyboxSettings();
    }

    void ApplySkyboxSettings()
    {
        if (cam == null)
            cam = GetComponent<Camera>();

        if (cam != null)
        {
            // Force la caméra à afficher le skybox
            cam.clearFlags = CameraClearFlags.Skybox;
            
            // S'assurer que le skybox est assigné dans RenderSettings
            if (RenderSettings.skybox == null)
            {
                Debug.LogWarning("ForceSkyboxCamera: Aucun skybox assigné dans les Lighting Settings!");
            }
        }
    }

#if UNITY_EDITOR
    void Update()
    {
        // En mode éditeur, vérifier à chaque frame pour faciliter le debug
        if (!Application.isPlaying)
        {
            ApplySkyboxSettings();
        }
    }
#endif
}
