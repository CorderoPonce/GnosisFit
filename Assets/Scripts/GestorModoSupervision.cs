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

    [Header("Visión por Computadora")]
    public GameObject motorMediaPipe;

    [Header("Supervisión de Postura")]
    public AnalisisPostura analisisPostura;

    // ── ESTADO INTERNO Y PIP ────────────────────────────────────────
    private bool _supervisionActivada = false;
    private GameObject _clonPIPActual;
    private string _nombreEjercicioActual = "Ejercicio";
    private SupervisionLayerManager _layerManager;
    
    [HideInInspector] public RenderTexture renderTexturePIP;
    private Camera camaraPIP;

    private void Awake()
    {
        _layerManager = GetComponent<SupervisionLayerManager>() ?? gameObject.AddComponent<SupervisionLayerManager>();
        _layerManager.motorMediaPipe = motorMediaPipe;

        var btn = GameObject.Find("BotonCamara")?.GetComponent<UnityEngine.UI.Button>();
        if (btn != null) { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(AlternarModoSupervision); }
    }

    private void Start()
    {
        // NUEVO: Como ahora cargamos SupervisionMode directamente desde el menú,
        // arrancamos el flujo de supervisión inmediatamente al iniciar la escena.
        _supervisionActivada = true;
        StartCoroutine(ActivarSupervision());
    }

    private void Update()
    {
        if (_supervisionActivada && _clonPIPActual != null)
        {
            _clonPIPActual.transform.Rotate(Vector3.up, 25f * Time.deltaTime, Space.World);
        }
    }

    public void AlternarModoSupervision()
    {
        if (GenosisFitDataManager.Instance != null)
        {
            GenosisFitDataManager.Instance.VieneDeAR = true;
            if (scriptPlaceExample != null)
            {
                GenosisFitDataManager.Instance.IndicePersonaje = scriptPlaceExample.indicePersonajeActual;
                GenosisFitDataManager.Instance.IndiceEjercicio = scriptPlaceExample.indiceEjercicioActual;
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
        UnityEngine.SceneManagement.SceneManager.LoadScene("SupervisionMode");
    }

    private IEnumerator ActivarSupervision()
    {
        if (scriptPlaceExample != null && scriptPlaceExample.modeloInstanciado != null) scriptPlaceExample.modeloInstanciado.SetActive(false);
        if (scriptPlaceExample != null) scriptPlaceExample.enabled = false;

        yield return ApagarAR();

        if (panelSupervisionUI != null) panelSupervisionUI.SetActive(true);

        // CORRECCIÓN: Primero generamos el clon y la textura para que existan de antemano
        GenerarClonPIP();
        ConfigurarAnalisisPostura();

        yield return _layerManager.ActivarAsync();

        if (_layerManager.EstaActivo && !_layerManager.HuboError)
        {
            // CORRECCIÓN: El HUD se crea DESPUÉS de que la textura PIP está lista y asignada
            CrearHUDDentroDeCanvas(_layerManager.ObtenerCanvasMaestro());
        }
        else { DesactivarSupervision(); }
    }

    private void DesactivarSupervision()
    {
        _supervisionActivada = false;
        if (analisisPostura != null) analisisPostura.enabled = false;
        _layerManager.Desactivar();
        if (panelSupervisionUI != null) panelSupervisionUI.SetActive(false);
        if (_clonPIPActual != null) Destroy(_clonPIPActual);
        StartCoroutine(RestaurarAR());
        if (scriptPlaceExample != null) scriptPlaceExample.enabled = true;
        if (scriptPlaceExample != null && scriptPlaceExample.modeloInstanciado != null) scriptPlaceExample.modeloInstanciado.SetActive(true);
    }

    private IEnumerator ApagarAR()
    {
        var arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
        var planeManager = FindFirstObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        var cameraBackground = FindFirstObjectByType<ARCameraBackground>(FindObjectsInactive.Include);

        if (cameraBackground != null) cameraBackground.enabled = false;
        if (planeManager != null) planeManager.enabled = false;
        if (cameraManager != null) cameraManager.enabled = false;

#if !UNITY_EDITOR
        if (arSession != null) arSession.gameObject.SetActive(false); yield return new WaitForSeconds(1.0f);
#else
        if (arSession != null) { arSession.enabled = false; arSession.Reset(); } yield return new WaitForSeconds(0.2f);
#endif
    }

    private IEnumerator RestaurarAR()
    {
        var arSession = FindFirstObjectByType<ARSession>(FindObjectsInactive.Include);
        var planeManager = FindFirstObjectByType<ARPlaneManager>(FindObjectsInactive.Include);
        var cameraBackground = FindFirstObjectByType<ARCameraBackground>(FindObjectsInactive.Include);

#if !UNITY_EDITOR
        if (arSession != null) arSession.gameObject.SetActive(true); yield return new WaitForSeconds(0.5f);
#else
        if (arSession != null) arSession.enabled = true; yield return null;
#endif

        if (cameraBackground != null) cameraBackground.enabled = true;
        if (cameraManager != null) { cameraManager.requestedFacingDirection = CameraFacingDirection.World; cameraManager.enabled = true; }
        if (planeManager != null) planeManager.enabled = true;
    }

    private void GenerarClonPIP()
    {
        // Leer los índices desde el DataManager persistente
        int idPersonaje = 0;
        int idEjercicio = 0;

        if (GenosisFitDataManager.Instance != null)
        {
            idPersonaje = GenosisFitDataManager.Instance.IndicePersonaje;
            idEjercicio = GenosisFitDataManager.Instance.IndiceEjercicio;
            _nombreEjercicioActual = GenosisFitDataManager.Instance.EjercicioSeleccionado;
        }

        if (scriptPlaceExample == null || scriptPlaceExample.avataresPrefabs == null || scriptPlaceExample.avataresPrefabs.Length == 0) return;
        if (_clonPIPActual != null) Destroy(_clonPIPActual);

        if (idPersonaje >= scriptPlaceExample.avataresPrefabs.Length) idPersonaje = 0;

        if (contenedorPIP == null) {
            contenedorPIP = new GameObject("ContenedorPIP_Oculto").transform;
            contenedorPIP.position = new Vector3(0, -5000, 0); 
        }

        _clonPIPActual = Instantiate(scriptPlaceExample.avataresPrefabs[idPersonaje], contenedorPIP.position, contenedorPIP.rotation);

        var animator = _clonPIPActual.GetComponent<Animator>();
        if (animator != null && scriptPlaceExample.ejerciciosControllers != null && idEjercicio < scriptPlaceExample.ejerciciosControllers.Length) {
            animator.runtimeAnimatorController = scriptPlaceExample.ejerciciosControllers[idEjercicio];
        }

        CambiarCapaRecursivo(_clonPIPActual.transform, 4);

        if (renderTexturePIP == null) {
            renderTexturePIP = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            renderTexturePIP.Create();
        }

        if (camaraPIP == null) {
            GameObject camObj = new GameObject("CamaraPIP");
            camObj.transform.position = contenedorPIP.position + new Vector3(0, 1.2f, 2.8f);
            camObj.transform.LookAt(contenedorPIP.position + new Vector3(0, 1f, 0));
            
            camaraPIP = camObj.AddComponent<Camera>();
            camaraPIP.targetTexture = renderTexturePIP;
            camaraPIP.clearFlags = CameraClearFlags.SolidColor;
            camaraPIP.backgroundColor = new Color(0, 0, 0, 0); // Fondo transparente premium
            camaraPIP.cullingMask = 1 << 4; // Capa Water únicamente
        }
    }

    private void CambiarCapaRecursivo(Transform obj, int layer)
    {
        obj.gameObject.layer = layer;
        foreach (Transform child in obj) CambiarCapaRecursivo(child, layer);
    }

    private void ConfigurarAnalisisPostura()
    {
        if (analisisPostura == null) return;
        TipoSupervision tipo = TipoSupervision.Generic;

        if (GenosisFitDataManager.Instance != null)
        {
            tipo = GenosisFitDataManager.Instance.TipoEjercicio;
        }

        analisisPostura.ConfigurarEjercicio(tipo);
        analisisPostura.enabled = true;
    }

    private void CrearHUDDentroDeCanvas(Canvas canvasMaestro)
    {
        if (canvasMaestro == null) return;
        var hud = canvasMaestro.gameObject.AddComponent<SupervisionHUD>();
        hud.Inicializar(canvasMaestro, analisisPostura, this, _nombreEjercicioActual);
    }
}