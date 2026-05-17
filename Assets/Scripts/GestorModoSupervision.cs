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

    private bool _supervisionActivada = false;
    private GameObject _clonPIPActual;
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

    private void Update()
    {
        if (_supervisionActivada && _clonPIPActual != null)
            _clonPIPActual.transform.Rotate(Vector3.up, 25f * Time.deltaTime, Space.World);
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

    public void InicializarTexturaYClon()
    {
        // 1. Asegurar la existencia de la Textura y la Cámara SÍ O SÍ
        if (renderTexturePIP == null) {
            renderTexturePIP = new RenderTexture(512, 512, 16, RenderTextureFormat.ARGB32);
            renderTexturePIP.Create();
        }

        if (contenedorPIP == null) {
            contenedorPIP = new GameObject("ContenedorPIP_Oculto").transform;
            contenedorPIP.position = new Vector3(0, -5000, 0); 
        }

        if (camaraPIP == null) {
            GameObject camObj = new GameObject("CamaraPIP");
            camObj.transform.position = contenedorPIP.position + new Vector3(0, 1.2f, 2.8f);
            camObj.transform.LookAt(contenedorPIP.position + new Vector3(0, 1f, 0));
            
            camaraPIP = camObj.AddComponent<Camera>();
            camaraPIP.targetTexture = renderTexturePIP;
            camaraPIP.clearFlags = CameraClearFlags.SolidColor;
            camaraPIP.backgroundColor = new Color(0, 0, 0, 0); 
            camaraPIP.cullingMask = 1 << 4; 
        }

        // 2. Intentar cargar el avatar
        GenerarClonPIP();
        _supervisionActivada = true;
    }

    private void GenerarClonPIP()
    {
        int idPersonaje = 0;
        int idEjercicio = 0;

        if (GenosisFitDataManager.Instance != null)
        {
            idPersonaje = GenosisFitDataManager.Instance.IndicePersonaje;
            idEjercicio = GenosisFitDataManager.Instance.IndiceEjercicio;
        }

        if (scriptPlaceExample == null || scriptPlaceExample.avataresPrefabs == null || scriptPlaceExample.avataresPrefabs.Length == 0) return;
        if (_clonPIPActual != null) Destroy(_clonPIPActual);
        if (idPersonaje >= scriptPlaceExample.avataresPrefabs.Length) idPersonaje = 0;

        _clonPIPActual = Instantiate(scriptPlaceExample.avataresPrefabs[idPersonaje], contenedorPIP.position, contenedorPIP.rotation);

        var animator = _clonPIPActual.GetComponent<Animator>();
        if (animator != null && scriptPlaceExample.ejerciciosControllers != null && idEjercicio < scriptPlaceExample.ejerciciosControllers.Length) {
            animator.runtimeAnimatorController = scriptPlaceExample.ejerciciosControllers[idEjercicio];
        }

        CambiarCapaRecursivo(_clonPIPActual.transform, 4);
    }

    private void CambiarCapaRecursivo(Transform obj, int layer)
    {
        obj.gameObject.layer = layer;
        foreach (Transform child in obj) CambiarCapaRecursivo(child, layer);
    }
}