using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

/// <summary>
/// Orquesta la activación del Modo Supervisión.
/// Delega la gestión de capas visuales a SupervisionLayerManager
/// y la lógica de análisis de postura a AnalisisPostura.
/// </summary>
public class GestorModoSupervision : MonoBehaviour
{
    [Header("Referencias AR")]
    public ARCameraManager cameraManager;
    public PlaceExample scriptPlaceExample;

    [Header("UI y PIP")]
    public GameObject panelSupervisionUI;
    public Transform contenedorPIP;

    [Header("Visión por Computadora")]
    public GameObject motorMediaPipe;

    [Header("Supervisión de Postura")]
    public AnalisisPostura analisisPostura;

    // ─────────────────────────────────────────────────────────────────
    // ESTADO INTERNO
    // ─────────────────────────────────────────────────────────────────
    private bool _supervisionActivada = false;
    private GameObject _clonPIPActual;
    private string _nombreEjercicioActual = "Ejercicio";
    private SupervisionLayerManager _layerManager;

    // ─────────────────────────────────────────────────────────────────
    // INICIALIZACIÓN
    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        Debug.Log("[GestorSupervision] Awake iniciado.");
        // Crear o encontrar el LayerManager en este mismo GO
        _layerManager = GetComponent<SupervisionLayerManager>();
        if (_layerManager == null)
            _layerManager = gameObject.AddComponent<SupervisionLayerManager>();

        _layerManager.motorMediaPipe = motorMediaPipe;

        // Auto-vincular el botón si existe (por si se perdió la referencia en el Inspector)
        var btn = GameObject.Find("BotonCamara")?.GetComponent<UnityEngine.UI.Button>();
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(AlternarModoSupervision);
            Debug.Log("[GestorSupervision] BotonCamara vinculado automáticamente.");
        }
        else
        {
            Debug.LogWarning("[GestorSupervision] No se encontró el objeto 'BotonCamara' para auto-vincular.");
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // PUNTO DE ENTRADA (BOTÓN)
    // ─────────────────────────────────────────────────────────────────

    public void AlternarModoSupervision()
    {
        Debug.Log("[GestorSupervision] Iniciando transición a escena de Supervisión...");

        // 1. Guardar estado en el DataManager persistente
        if (GenosisFitDataManager.Instance != null)
        {
            GenosisFitDataManager.Instance.VieneDeAR = true;
            
            if (scriptPlaceExample != null)
            {
                GenosisFitDataManager.Instance.IndicePersonaje = scriptPlaceExample.indicePersonajeActual;
                GenosisFitDataManager.Instance.IndiceEjercicio = scriptPlaceExample.indiceEjercicioActual;
                
                // Buscar el nombre y tipo del ejercicio actual
                foreach (var ej in ExerciseData.ObtenerCatalogo())
                {
                    if (ej.idControlador == scriptPlaceExample.indiceEjercicioActual)
                    {
                        GenosisFitDataManager.Instance.EjercicioSeleccionado = ej.nombre;
                        GenosisFitDataManager.Instance.TipoEjercicio = ej.tipoSupervision;
                        break;
                    }
                }
            }
        }

        // 2. Cargar la escena dedicada
        // Esto automáticamente liberará ARCore y la cámara trasera
        UnityEngine.SceneManagement.SceneManager.LoadScene("SupervisionMode");
    }

    // ─────────────────────────────────────────────────────────────────
    // ACTIVACIÓN
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator ActivarSupervision()
    {
        // 1. Ocultar el modelo AR y deshabilitar PlaceExample
        if (scriptPlaceExample != null && scriptPlaceExample.modeloInstanciado != null)
            scriptPlaceExample.modeloInstanciado.SetActive(false);
        if (scriptPlaceExample != null)
            scriptPlaceExample.enabled = false;

        // 2. Apagar subsistemas AR para liberar la cámara trasera
        yield return ApagarAR();

        // 3. Activar el panel de supervisión (contiene Annotatable Screen)
        if (panelSupervisionUI != null)
            panelSupervisionUI.SetActive(true);

        // 4. Crear clon PIP del ejercicio en curso
        GenerarClonPIP();

        // 5. Configurar análisis de postura ANTES de activar cámara
        ConfigurarAnalisisPostura();

        // 6. Activar cámara frontal + Bootstrap + reorganizar capas
        yield return _layerManager.ActivarAsync();

        // 7. Si el LayerManager logró inicializarse, construir el HUD dentro de su Canvas
        if (_layerManager.EstaActivo && !_layerManager.HuboError)
        {
            var canvasMaestro = _layerManager.ObtenerCanvasMaestro();
            CrearHUDDentroDeCanvas(canvasMaestro);
            Debug.Log("[GestorSupervision] Modo Supervisión activado correctamente.");
        }
        else
        {
            Debug.LogError("[GestorSupervision] Fallo crítico en inicialización. Revirtiendo a modo AR...");
            _supervisionActivada = false;
            DesactivarSupervision();
            // Aquí se podría disparar una notificación UI de "Error de Cámara"
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // DESACTIVACIÓN
    // ─────────────────────────────────────────────────────────────────

    private void DesactivarSupervision()
    {
        // Desactivar análisis de postura
        if (analisisPostura != null) analisisPostura.enabled = false;

        // LayerManager restaura la jerarquía y destruye el HUD
        _layerManager.Desactivar();

        // Restaurar UI
        if (panelSupervisionUI != null) panelSupervisionUI.SetActive(false);
        if (_clonPIPActual != null) Destroy(_clonPIPActual);

        // Restaurar AR
        StartCoroutine(RestaurarAR());

        // Restaurar modelo
        if (scriptPlaceExample != null) scriptPlaceExample.enabled = true;
        if (scriptPlaceExample != null && scriptPlaceExample.modeloInstanciado != null)
            scriptPlaceExample.modeloInstanciado.SetActive(true);

        Debug.Log("[GestorSupervision] Modo Supervisión desactivado.");
    }

    // ─────────────────────────────────────────────────────────────────
    // GESTIÓN DE AR
    // ─────────────────────────────────────────────────────────────────

    private IEnumerator ApagarAR()
    {
        var arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
        var planeManager = FindFirstObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        var cameraBackground = FindFirstObjectByType<ARCameraBackground>(FindObjectsInactive.Include);

        if (cameraBackground != null) cameraBackground.enabled = false;
        if (planeManager != null) planeManager.enabled = false;
        if (cameraManager != null) cameraManager.enabled = false;

#if !UNITY_EDITOR
        if (arSession != null) arSession.gameObject.SetActive(false);
        yield return new WaitForSeconds(1.0f);
#else
        if (arSession != null) { arSession.enabled = false; arSession.Reset(); }
        yield return new WaitForSeconds(0.2f);
#endif
        Debug.Log("[GestorSupervision] AR apagado.");
    }

    private IEnumerator RestaurarAR()
    {
        var arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
        var planeManager = FindFirstObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        var cameraBackground = FindFirstObjectByType<ARCameraBackground>(FindObjectsInactive.Include);

#if !UNITY_EDITOR
        if (arSession != null) arSession.gameObject.SetActive(true);
        yield return new WaitForSeconds(0.5f);
#else
        if (arSession != null) arSession.enabled = true;
        yield return null;
#endif

        if (cameraBackground != null) cameraBackground.enabled = true;
        if (cameraManager != null)
        {
            cameraManager.requestedFacingDirection = CameraFacingDirection.World;
            cameraManager.enabled = true;
        }
        if (planeManager != null) planeManager.enabled = true;

        Debug.Log("[GestorSupervision] AR restaurado.");
    }

    // ─────────────────────────────────────────────────────────────────
    // CLON PIP
    // ─────────────────────────────────────────────────────────────────

    private void GenerarClonPIP()
    {
        if (scriptPlaceExample == null || scriptPlaceExample.avataresPrefabs == null
            || scriptPlaceExample.avataresPrefabs.Length == 0) return;

        if (_clonPIPActual != null) Destroy(_clonPIPActual);

        int idPersonaje = scriptPlaceExample.indicePersonajeActual;
        int idEjercicio = scriptPlaceExample.indiceEjercicioActual;

        if (idPersonaje >= scriptPlaceExample.avataresPrefabs.Length) idPersonaje = 0;

        _clonPIPActual = Instantiate(
            scriptPlaceExample.avataresPrefabs[idPersonaje],
            contenedorPIP.position,
            contenedorPIP.rotation);

        var animator = _clonPIPActual.GetComponent<Animator>();
        if (animator != null && scriptPlaceExample.ejerciciosControllers != null
            && idEjercicio < scriptPlaceExample.ejerciciosControllers.Length)
        {
            animator.runtimeAnimatorController = scriptPlaceExample.ejerciciosControllers[idEjercicio];
        }
    }

    // ─────────────────────────────────────────────────────────────────
    // ANÁLISIS DE POSTURA
    // ─────────────────────────────────────────────────────────────────

    private void ConfigurarAnalisisPostura()
    {
        if (analisisPostura == null) return;

        TipoSupervision tipo = TipoSupervision.Generic;
        _nombreEjercicioActual = "Ejercicio";

        if (scriptPlaceExample != null)
        {
            int idEjercicio = scriptPlaceExample.indiceEjercicioActual;
            foreach (var ej in ExerciseData.ObtenerCatalogo())
            {
                if (ej.idControlador == idEjercicio)
                {
                    tipo = ej.tipoSupervision;
                    _nombreEjercicioActual = ej.nombre;
                    break;
                }
            }
        }

        analisisPostura.ConfigurarEjercicio(tipo);
        analisisPostura.enabled = true;
        Debug.Log($"[GestorSupervision] AnalisisPostura configurado: {tipo} ({_nombreEjercicioActual})");
    }

    // ─────────────────────────────────────────────────────────────────
    // HUD
    // ─────────────────────────────────────────────────────────────────

    private void CrearHUDDentroDeCanvas(Canvas canvasMaestro)
    {
        if (canvasMaestro == null) return;

        var hud = canvasMaestro.gameObject.AddComponent<SupervisionHUD>();
        hud.Inicializar(canvasMaestro, analisisPostura, this, _nombreEjercicioActual);

        Debug.Log($"[GestorSupervision] HUD creado para: {_nombreEjercicioActual}");
    }
}