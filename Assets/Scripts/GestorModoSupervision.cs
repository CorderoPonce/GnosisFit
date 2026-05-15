using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class GestorModoSupervision : MonoBehaviour
{
    [Header("Referencias AR")]
    public ARCameraManager cameraManager;
    public PlaceExample scriptPlaceExample; 

    [Header("UI y PIP")]
    public GameObject panelSupervisionUI; 
    public Transform contenedorPIP; 

    private GameObject clonPIPActual;
    private bool supervisionActivada = false;

    [Header("Visión por Computadora")]
    public GameObject motorMediaPipe; 

    public void AlternarModoSupervision()
    {
        supervisionActivada = !supervisionActivada;

        if (supervisionActivada)
        {
            // 1. Ocultar el modelo original de forma segura
            if (scriptPlaceExample != null && scriptPlaceExample.modeloInstanciado != null)
            {
                scriptPlaceExample.modeloInstanciado.SetActive(false);
            }

            if (scriptPlaceExample != null) scriptPlaceExample.enabled = false;
            
            // 2. Encender la UI
            if (panelSupervisionUI != null) panelSupervisionUI.SetActive(true);

            // 3. Crear el clon (Protegido contra errores)
            GenerarClonPIP();

            // 4. Iniciar el semáforo con la dirección correcta
            StartCoroutine(ReiniciarCamaraAR(CameraFacingDirection.User));
        }
        else
        {
            StartCoroutine(ReiniciarCamaraAR(CameraFacingDirection.World));
            
            if (scriptPlaceExample != null) scriptPlaceExample.enabled = true;
            if (panelSupervisionUI != null) panelSupervisionUI.SetActive(false);

            if (scriptPlaceExample != null && scriptPlaceExample.modeloInstanciado != null)
            {
                scriptPlaceExample.modeloInstanciado.SetActive(true);
            }
            
            if (clonPIPActual != null) Destroy(clonPIPActual);
        }
    }

    private IEnumerator ReiniciarCamaraAR(CameraFacingDirection nuevaDireccion)
    {
        ARSession arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
        ARPlaneManager planeManager = FindFirstObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        ARCameraBackground cameraBackground = FindFirstObjectByType<ARCameraBackground>(FindObjectsInactive.Include);

        Debug.Log($"[GestorSupervision] Iniciando transición → {nuevaDireccion}. Estado ARSession inicial: {ARSession.state}");

        // ── PASO 1: APAGAR AR ─────────────────────────────────────────────
        if (cameraBackground != null) cameraBackground.enabled = false;
        if (planeManager != null) planeManager.enabled = false;
        if (cameraManager != null) cameraManager.enabled = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        // ═══════════════════════════════════════════════════════════════════
        // ANDROID: Flujo completo
        // ═══════════════════════════════════════════════════════════════════

        // 1. Detener MediaPipe si estaba corriendo
        if (motorMediaPipe != null && motorMediaPipe.activeSelf)
        {
            motorMediaPipe.SendMessage("Stop", SendMessageOptions.DontRequireReceiver);
            Debug.Log("[GestorSupervision] [Android] MediaPipe Stop() enviado.");
        }

        // 2. Desactivar ARSession GO para liberar la cámara trasera
        if (arSession != null)
        {
            arSession.gameObject.SetActive(false);
            Debug.Log("[GestorSupervision] [Android] ARSession GO desactivado.");
        }

        // 3. Esperar a que Android libere el hardware
        yield return new WaitForSeconds(1.5f);
        Debug.Log("[GestorSupervision] [Android] Espera de liberación completada.");

        if (nuevaDireccion == CameraFacingDirection.User)
        {
            // 4. Activar MediaPipe GO si estaba desactivado (esto dispara Bootstrap.Init())
            if (motorMediaPipe != null && !motorMediaPipe.activeSelf)
            {
                motorMediaPipe.SetActive(true);
                Debug.Log("[GestorSupervision] [Android] MediaPipe GO activado. Esperando Bootstrap...");
            }

            // 5. Esperar a que Bootstrap termine de inicializar ImageSourceProvider
            float timeout = 10f;
            float waited = 0f;
            while (Mediapipe.Unity.Sample.ImageSourceProvider.ImageSource == null && waited < timeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            Debug.Log($"[GestorSupervision] [Android] Bootstrap completado en {waited:F2}s. ImageSource null={Mediapipe.Unity.Sample.ImageSourceProvider.ImageSource == null}");

            // 6. Configurar cámara frontal
            var src = Mediapipe.Unity.Sample.ImageSourceProvider.ImageSource;
            if (src != null)
            {
                var devices = WebCamTexture.devices;
                Debug.Log($"[GestorSupervision] [Android] Cámaras disponibles: {devices.Length}");
                for (int i = 0; i < devices.Length; i++)
                {
                    Debug.Log($"[GestorSupervision] [Android]   [{i}] isFrontFacing: {devices[i].isFrontFacing}");
                    if (devices[i].isFrontFacing)
                    {
                        src.SelectSource(i);
                        Debug.Log($"[GestorSupervision] [Android] SelectSource({i}) OK.");
                        break;
                    }
                }
            }
            else
            {
                Debug.LogError("[GestorSupervision] [Android] FALLO: ImageSource sigue null tras esperar Bootstrap.");
            }

            // 7. Iniciar captura de MediaPipe
            if (motorMediaPipe != null)
            {
                motorMediaPipe.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
                Debug.Log("[GestorSupervision] [Android] MediaPipe Play() enviado.");

                // 8. Configurar Canvas y garantizar que la UI esté por encima
                yield return null; // Esperar un frame para que Screen.Initialize() se ejecute

                // Desactivar raycast en el RawImage de MediaPipe
                var mpScreen = FindFirstObjectByType<Mediapipe.Unity.Screen>(FindObjectsInactive.Include);
                if (mpScreen != null)
                {
                    var rawImg = mpScreen.GetComponentInChildren<UnityEngine.UI.RawImage>(true);
                    if (rawImg != null) rawImg.raycastTarget = false;
                }

                // PLAN C: Crear un Canvas separado para la UI, encima de todo
                if (panelSupervisionUI != null)
                {
                    // Crear Canvas independiente solo si no existe ya
                    var existingOverlay = GameObject.Find("CanvasSupervisionOverlay");
                    if (existingOverlay == null)
                    {
                        var overlayGO = new GameObject("CanvasSupervisionOverlay");
                        var overlayCanvas = overlayGO.AddComponent<Canvas>();
                        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                        overlayCanvas.sortingOrder = 100; // Siempre encima
                        overlayGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                        overlayGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                        // Mover el panel de supervisión al nuevo Canvas
                        panelSupervisionUI.transform.SetParent(overlayGO.transform, false);

                        // Estirar el panel para que llene el nuevo Canvas
                        var panelRT = panelSupervisionUI.GetComponent<RectTransform>();
                        if (panelRT != null)
                        {
                            panelRT.anchorMin = Vector2.zero;
                            panelRT.anchorMax = Vector2.one;
                            panelRT.offsetMin = Vector2.zero;
                            panelRT.offsetMax = Vector2.zero;
                        }

                        Debug.Log("[GestorSupervision] [Android] Canvas separado creado para UI (sortingOrder=100).");
                    }
                }
            }
        }
        else
        {
            // ── Volver a AR ──
            // Detener MediaPipe para liberar cámara frontal
            if (motorMediaPipe != null && motorMediaPipe.activeSelf)
            {
                motorMediaPipe.SendMessage("Stop", SendMessageOptions.DontRequireReceiver);
                Debug.Log("[GestorSupervision] [Android] MediaPipe Stop() para liberar cámara frontal.");
                yield return new WaitForSeconds(0.5f);
            }

            // Reactivar ARSession
            if (arSession != null)
            {
                arSession.gameObject.SetActive(true);
                Debug.Log("[GestorSupervision] [Android] ARSession GO reactivado.");
            }
            yield return new WaitForSeconds(0.5f);

            if (cameraBackground != null) cameraBackground.enabled = true;
            if (cameraManager != null) cameraManager.requestedFacingDirection = CameraFacingDirection.World;
            if (cameraManager != null) cameraManager.enabled = true;
            if (planeManager != null) planeManager.enabled = true;
            Debug.Log("[GestorSupervision] [Android] AR restaurado.");
        }

#else
        // ═══════════════════════════════════════════════════════════════════
        // PC / EDITOR: Flujo original que ya funciona
        // ═══════════════════════════════════════════════════════════════════
        if (motorMediaPipe != null)
        {
            motorMediaPipe.SendMessage("Stop", SendMessageOptions.DontRequireReceiver);
            motorMediaPipe.SetActive(false);
        }
        if (arSession != null) { arSession.enabled = false; arSession.Reset(); }
        yield return new WaitForSeconds(0.5f);

        Debug.Log("[GestorSupervision] Espera completada. Iniciando modo destino...");

        if (nuevaDireccion == CameraFacingDirection.User)
        {
            if (motorMediaPipe != null)
            {
                motorMediaPipe.SetActive(true);
                yield return null;
            }
            var devices = WebCamTexture.devices;
            Debug.Log($"[GestorSupervision] Cámaras disponibles: {devices.Length}");
            for (int i = 0; i < devices.Length; i++)
            {
                Debug.Log($"[GestorSupervision]   [{i}] Camera {i} — isFrontFacing: {devices[i].isFrontFacing}");
                if (devices[i].isFrontFacing)
                {
                    var src = Mediapipe.Unity.Sample.ImageSourceProvider.ImageSource;
                    Debug.Log($"[GestorSupervision] ImageSource es null: {src == null}");
                    if (src != null)
                    {
                        src.SelectSource(i);
                        Debug.Log($"[GestorSupervision] SelectSource({i}) llamado.");
                    }
                    break;
                }
            }
            if (motorMediaPipe != null)
            {
                motorMediaPipe.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
                Debug.Log("[GestorSupervision] MediaPipe.Play() enviado.");
            }
        }
        else
        {
            if (arSession != null) arSession.enabled = true;
            if (cameraBackground != null) cameraBackground.enabled = true;
            if (cameraManager != null) cameraManager.requestedFacingDirection = CameraFacingDirection.World;
            if (cameraManager != null) cameraManager.enabled = true;
            if (planeManager != null) planeManager.enabled = true;
            Debug.Log("[GestorSupervision] AR restaurado.");
        }
#endif
    }

    private void GenerarClonPIP()
    {
        // Seguro: Si no hay referencias, no intentamos clonar nada para no crashear la app
        if (scriptPlaceExample == null || scriptPlaceExample.avataresPrefabs == null || scriptPlaceExample.avataresPrefabs.Length == 0) return;

        if (clonPIPActual != null) Destroy(clonPIPActual);

        int idPersonaje = scriptPlaceExample.indicePersonajeActual;
        int idEjercicio = scriptPlaceExample.indiceEjercicioActual;

        // Seguro de índices
        if (idPersonaje >= scriptPlaceExample.avataresPrefabs.Length) idPersonaje = 0;

        clonPIPActual = Instantiate(scriptPlaceExample.avataresPrefabs[idPersonaje], contenedorPIP.position, contenedorPIP.rotation);
        
        Animator animator = clonPIPActual.GetComponent<Animator>();
        if (animator != null && scriptPlaceExample.ejerciciosControllers != null && idEjercicio < scriptPlaceExample.ejerciciosControllers.Length)
        {
            animator.runtimeAnimatorController = scriptPlaceExample.ejerciciosControllers[idEjercicio];
        }
    }
}