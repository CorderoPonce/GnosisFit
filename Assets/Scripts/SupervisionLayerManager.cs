using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using Mediapipe.Tasks.Vision.PoseLandmarker;

/// <summary>
/// Gestiona las capas visuales y repara las referencias del motor de MediaPipe en tiempo de ejecución.
/// Esta versión V2.1 incluye Autorreparación de referencias y normalización de escalas.
/// </summary>
public class SupervisionLayerManager : MonoBehaviour
{
    [Header("Referencia al motor de MediaPipe")]
    public GameObject motorMediaPipe;

    // Canvas independiente para el HUD (Capa superior)
    private Canvas _canvasHUD;
    
    // Referencias para restaurar el estado original
    private Canvas _canvasMediaPipeOriginal;
    private int _sortingOrderOriginal = 0;

    // Estado
    private bool _activo = false;
    public bool EstaActivo => _activo;
    public bool HuboError { get; private set; } = false;

    // ─────────────────────────────────────────────────────────────────
    // ACTIVACIÓN
    // ─────────────────────────────────────────────────────────────────

    public IEnumerator ActivarAsync()
    {
        if (_activo) yield break;
        HuboError = false;

        Debug.Log("[LayerManager] Iniciando pipeline de activación V3.0...");

        // 1. Configurar capas de UI
        ConfigurarSortingCapas();

        // 2. Seleccionar cámara frontal ANTES de activar el Runner
        //    (El Runner la usará cuando llame a imageSource.Play() internamente)
        ForzarInicioCamaraFrontal();

        // 3. Activar el motor MediaPipe (Solution GameObject)
        //    Su PoseLandmarkerRunner.Start() se ejecutará automáticamente y:
        //    - Buscará/Creará Bootstrap (lo instancia desde prefab si no existe)
        //    - Esperará a que Bootstrap termine (inicializa GPU, AssetLoader, ImageSource)
        //    - Llamará a Play() → Run() que inicia la cámara y el pipeline de detección
        if (motorMediaPipe != null)
        {
            motorMediaPipe.SetActive(true);
            Debug.Log("[LayerManager] Motor MediaPipe (Solution) activado. Esperando inicialización interna...");
        }
        else
        {
            Debug.LogError("[LayerManager] Error: motorMediaPipe no asignado en el Inspector.");
            HuboError = true;
            yield break;
        }

        // 4. Esperar a que el Runner complete su inicialización
        //    El Runner internamente: Bootstrap → isFinished → Play() → Run() → imageSource.Play()
        var runner = motorMediaPipe.GetComponent<PoseLandmarkerRunner>();
        float elapsed = 0f;
        float timeout = 20f; // Bootstrap + carga de modelo + cámara puede tardar

        // Esperar a que ImageSource esté preparada (señal de que Run() ya arrancó la cámara)
        while (ImageSourceProvider.ImageSource == null || !ImageSourceProvider.ImageSource.isPrepared)
        {
            elapsed += Time.deltaTime;
            if (elapsed > timeout)
            {
                Debug.LogError("[LayerManager] TIMEOUT: El pipeline MediaPipe no completó la inicialización en " + timeout + "s.");
                HuboError = true;
                yield break;
            }
            yield return null;
        }
        Debug.Log("[LayerManager] ImageSource preparada (" + ImageSourceProvider.ImageSource.textureWidth + "x" + ImageSourceProvider.ImageSource.textureHeight + "). Elapsed: " + elapsed.ToString("F1") + "s");

        // 5. Reparar Referencias Críticas (por si la escena las perdió)
        RepararReferenciasMotor();

        // 6. Crear el Canvas del HUD
        CrearHUDCanvas();

        _activo = true;
        Debug.Log("[LayerManager] Modo Supervisión activo. V3.0 Completo.");
    }

    private void RepararReferenciasMotor()
    {
        if (motorMediaPipe == null) return;
        
        var runner = motorMediaPipe.GetComponent<PoseLandmarkerRunner>();
        if (runner == null)
        {
            Debug.LogError("[LayerManager] Error: No se encontró PoseLandmarkerRunner en " + motorMediaPipe.name);
            return;
        }

        // 1. Buscar el Controller y el Screen en la escena (incluso si están inactivos)
        var controller = Object.FindAnyObjectByType<PoseLandmarkerResultAnnotationController>(FindObjectsInactive.Include);
        var screenComp = Object.FindAnyObjectByType<Mediapipe.Unity.Screen>(FindObjectsInactive.Include);

        // Si no se encuentran por componente, intentar por nombre como último recurso
        if (controller == null) {
             var obj = GameObject.Find("Annotation Layer"); // Nota: Esto solo encuentra activos
             if (obj != null) controller = obj.GetComponent<PoseLandmarkerResultAnnotationController>();
        }
        
        if (screenComp == null) {
             var obj = GameObject.Find("Annotatable Screen");
             if (obj != null) screenComp = obj.GetComponent<Mediapipe.Unity.Screen>();
        }

        if (controller == null || screenComp == null)
        {
            Debug.LogWarning("[LayerManager] No se pudo encontrar Controller o Screen para reparar.");
            return;
        }

        Debug.Log("[LayerManager] Ejecutando Reparación Profunda de Referencias...");

        try {
            // A. Inyectar Controller en el Runner (_poseLandmarkerResultAnnotationController)
            var runnerType = typeof(PoseLandmarkerRunner);
            var fieldController = runnerType.GetField("_poseLandmarkerResultAnnotationController", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldController != null) {
                fieldController.SetValue(runner, controller);
                Debug.Log("[LayerManager] -> Controller inyectado en Runner.");
            }

            // B. Inyectar Screen en el Runner (campo 'screen' en la clase base VisionTaskApiRunner)
            var baseType = typeof(VisionTaskApiRunner<PoseLandmarker>);
            var fieldScreen = baseType.GetField("screen", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldScreen != null) {
                fieldScreen.SetValue(runner, screenComp);
                Debug.Log("[LayerManager] -> Screen inyectado en Runner.");
            }

            // C. Inyectar Annotation en el Controller
            var controllerBaseType = typeof(AnnotationController<MultiPoseLandmarkListWithMaskAnnotation>);
            var fieldAnnotation = controllerBaseType.GetField("annotation", BindingFlags.NonPublic | BindingFlags.Instance);
            
            var annot = controller.GetComponentInChildren<MultiPoseLandmarkListWithMaskAnnotation>(true);
            if (annot != null && fieldAnnotation != null)
            {
                fieldAnnotation.SetValue(controller, annot);
                Debug.Log("[LayerManager] -> Annotation inyectada en Controller.");

                // D. Inyectar Screen (RawImage) en la Annotation (_screen)
                var annotType = typeof(MultiPoseLandmarkListWithMaskAnnotation);
                var fieldRawImage = annotType.GetField("_screen", BindingFlags.NonPublic | BindingFlags.Instance);
                var rawImg = screenComp.GetComponent<RawImage>();
                
                if (fieldRawImage != null && rawImg != null) {
                    fieldRawImage.SetValue(annot, rawImg);
                    Debug.Log("[LayerManager] -> RawImage inyectado en Annotation.");
                    
                    // Asegurar visibilidad
                    rawImg.color = Color.white;
                    rawImg.enabled = true;

                    // E. Forzar Inicialización del Screen (Sincroniza Textura)
                    screenComp.Initialize(ImageSourceProvider.ImageSource);
                    Debug.Log("[LayerManager] -> Screen inicializado con la textura actual.");
                }

                // F. Forzar tamaño de imagen para el rendering (Fix Skeleton invisibility)
                if (ImageSourceProvider.ImageSource != null) {
                    controller.imageSize = new Vector2Int(ImageSourceProvider.ImageSource.textureWidth, ImageSourceProvider.ImageSource.textureHeight);
                    controller.InitScreen(ImageSourceProvider.ImageSource.textureWidth, ImageSourceProvider.ImageSource.textureHeight);
                    Debug.Log("[LayerManager] -> ImageSize forzada: " + controller.imageSize);

                    // G. Fix Aspect Ratio using AspectRatioFitter
                    var fitter = screenComp.GetComponent<UnityEngine.UI.AspectRatioFitter>();
                    if (fitter == null) fitter = screenComp.gameObject.AddComponent<UnityEngine.UI.AspectRatioFitter>();
                    fitter.aspectMode = UnityEngine.UI.AspectRatioFitter.AspectMode.EnvelopeParent;
                    fitter.aspectRatio = (float)ImageSourceProvider.ImageSource.textureWidth / ImageSourceProvider.ImageSource.textureHeight;
                    Debug.Log($"[LayerManager] -> AspectRatioFitter applied. Ratio: {fitter.aspectRatio}");
                }
            }

            // E. Forzar escala correcta para evitar que el esqueleto sea invisible por tamaño
            controller.transform.localScale = Vector3.one;
            controller.transform.localPosition = Vector3.zero;

            Debug.Log("[LayerManager] ¡Reparación de motor completada con éxito!");
        }
        catch (System.Exception e) {
            Debug.LogError("[LayerManager] Error crítico en reparación por reflexión: " + e.Message);
        }
    }

    private void ForzarInicioCamaraFrontal()
    {
        var source = ImageSourceProvider.ImageSource as WebCamSource;
        if (source == null) return;

        Debug.Log("[LayerManager] Configurando cámara frontal...");
        
        WebCamDevice[] devices = WebCamTexture.devices;
        for (int i = 0; i < devices.Length; i++)
        {
            if (devices[i].isFrontFacing)
            {
                source.SelectSource(i);
                break;
            }
        }
    }

    public void Desactivar()
    {
        if (!_activo) return;

        if (_canvasMediaPipeOriginal != null)
        {
            _canvasMediaPipeOriginal.sortingOrder = _sortingOrderOriginal;
        }

        if (_canvasHUD != null)
        {
            Destroy(_canvasHUD.gameObject);
            _canvasHUD = null;
        }

        if (motorMediaPipe != null)
        {
            motorMediaPipe.SetActive(false);
        }

        _activo = false;
        HuboError = false;
        Debug.Log("[LayerManager] Modo Supervisión desactivado.");
    }

    private void ConfigurarSortingCapas()
    {
        var screen = FindFirstObjectByType<Mediapipe.Unity.Screen>(FindObjectsInactive.Include);
        if (screen != null)
        {
            // Activar toda la rama del Screen y sus HIJOS (Capa de anotaciones)
            ActivarRecursivo(screen.transform);
            
            Transform current = screen.transform;
            while (current != null)
            {
                current.gameObject.SetActive(true);
                
                // Normalizar escala para evitar que el esqueleto se encoja
                current.localScale = Vector3.one;

                var canvas = current.GetComponent<Canvas>();
                if (canvas != null)
                {
                    _canvasMediaPipeOriginal = canvas;
                    _sortingOrderOriginal = canvas.sortingOrder;
                    canvas.sortingOrder = 10; // Debajo del HUD (50)
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                current = current.parent;
            }
            
            // Forzar estiramiento del Screen pero sin offsets, el AspectRatioFitter hará el resto
            var rt = screen.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }
    }

    private void ActivarRecursivo(Transform t)
    {
        t.gameObject.SetActive(true);
        foreach (Transform child in t)
        {
            ActivarRecursivo(child);
        }
    }

    private void CrearHUDCanvas()
    {
        var go = new GameObject("CanvasSupervisionHUD");
        _canvasHUD = go.AddComponent<Canvas>();
        _canvasHUD.renderMode = RenderMode.ScreenSpaceOverlay;
        _canvasHUD.sortingOrder = 50; 

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();
    }

    public Canvas ObtenerCanvasMaestro() => _canvasHUD;

    /// <summary>
    /// Devuelve el Canvas del motor MediaPipe (donde se renderiza la cámara y las anotaciones).
    /// Útil para dibujar el skeleton overlay directamente sobre la imagen de cámara.
    /// </summary>
    public Canvas ObtenerCanvasMediaPipe() => _canvasMediaPipeOriginal;

    /// <summary>
    /// Devuelve el RectTransform de la pantalla de MediaPipe, para alinear el SkeletonOverlay exactamente con la imagen.
    /// </summary>
    public RectTransform ObtenerPantallaMediaPipe()
    {
        var screenComp = Object.FindAnyObjectByType<Mediapipe.Unity.Screen>(FindObjectsInactive.Include);
        return screenComp != null ? screenComp.GetComponent<RectTransform>() : null;
    }
}
