using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class InitSupervisionScene : MonoBehaviour
{
    [Header("Referencias")]
    public SupervisionLayerManager layerManager;
    public AnalisisPostura analisisPostura;
    public GestorModoSupervision gestorSupervision; 

    private GameObject panelPreInicio;
    private string nombreEjercicioPendiente;
    private TipoSupervision tipoEjercicioPendiente;

    // Preview 3D
    private GameObject _previewContainer;
    private RenderTexture _previewRT;
    private Transform _previewClone;

    private void Awake()
    {
        // AUTO-ENLACE: Busca los gestores automáticamente para evitar referencias nulas
        if (gestorSupervision == null) gestorSupervision = FindFirstObjectByType<GestorModoSupervision>();
        if (layerManager == null) layerManager = FindFirstObjectByType<SupervisionLayerManager>();
        if (analisisPostura == null) analisisPostura = FindFirstObjectByType<AnalisisPostura>();
    }

    private void Start()
    {
        nombreEjercicioPendiente = "Corrección de Postura";
        tipoEjercicioPendiente = TipoSupervision.BicepCurl; 

        if (GenosisFitDataManager.Instance != null)
        {
            tipoEjercicioPendiente = GenosisFitDataManager.Instance.TipoEjercicio;
            nombreEjercicioPendiente = GenosisFitDataManager.Instance.EjercicioSeleccionado;
        }

        OcultarUIMediaPipe();

        if (layerManager != null) CrearUIPreInicio();
    }

    private void Update()
    {
        // Rotar el modelo 3D lentamente
        if (_previewClone != null)
            _previewClone.Rotate(Vector3.up, 20f * Time.deltaTime, Space.World);
    }

    private void OnDestroy()
    {
        LimpiarPreview3D();
    }

    private void CrearUIPreInicio()
    {
        var canvasGO = new GameObject("CanvasPreInicio");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        panelPreInicio = canvasGO;

        var bg = UIHelper.CrearPanel("Bg", canvasGO.transform, new Color(0.08f, 0.08f, 0.12f, 1f));
        UIHelper.SetAnchorsStretch(bg.GetComponent<RectTransform>());

        
        int idEjercicio = 0;
        if (GenosisFitDataManager.Instance != null)
            idEjercicio = GenosisFitDataManager.Instance.IndiceEjercicio;

        var previewPanel = UIHelper.CrearPanel("PreviewPanel", canvasGO.transform, new Color(0.12f, 0.12f, 0.18f, 1f), true);
        var rtPanel = previewPanel.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(0.25f, 0.68f);
        rtPanel.anchorMax = new Vector2(0.75f, 0.92f);
        rtPanel.sizeDelta = Vector2.zero;
        rtPanel.anchoredPosition = Vector2.zero;

        // RawImage para mostrar la RenderTexture del modelo 3D
        var rawImgGO = new GameObject("Preview3D", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        rawImgGO.transform.SetParent(previewPanel.transform, false);
        UIHelper.SetAnchorsStretch(rawImgGO.GetComponent<RectTransform>());
        var rawImg = rawImgGO.GetComponent<RawImage>();

        ConfigurarPreview3D(rawImg, idEjercicio);

        var title = UIHelper.CrearTexto("Titulo", canvasGO.transform, "¿LISTO PARA ENTRENAR?", 48, Color.white, FontStyles.Bold);
        var rtTitle = title.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0, 0.55f); rtTitle.anchorMax = new Vector2(1, 0.68f);
        rtTitle.sizeDelta = Vector2.zero; rtTitle.anchoredPosition = Vector2.zero;

        var sub = UIHelper.CrearTexto("Sub", canvasGO.transform, nombreEjercicioPendiente.ToUpper(), 36, new Color(0.2f, 0.75f, 1f), FontStyles.Bold);
        var rtSub = sub.GetComponent<RectTransform>();
        rtSub.anchorMin = new Vector2(0, 0.45f); rtSub.anchorMax = new Vector2(1, 0.55f);
        rtSub.sizeDelta = Vector2.zero; rtSub.anchoredPosition = Vector2.zero;

        var btnIniciar = UIHelper.CrearBoton("BtnIniciar", canvasGO.transform, "INICIAR ENTRENAMIENTO", new Color(0.00f, 0.75f, 0.65f, 1f), Color.white, 28, true);
        var rtBtnIni = btnIniciar.GetComponent<RectTransform>();
        rtBtnIni.anchorMin = new Vector2(0.5f, 0.3f); rtBtnIni.anchorMax = new Vector2(0.5f, 0.3f);
        rtBtnIni.pivot = new Vector2(0.5f, 0.5f); rtBtnIni.sizeDelta = new Vector2(450, 90);
        btnIniciar.onClick.AddListener(BotonIniciarPresionado);

        var btnVolver = UIHelper.CrearBoton("BtnVolver", canvasGO.transform, "VOLVER AL MENÚ", new Color(0.8f, 0.2f, 0.2f, 0.9f), Color.white, 24, true);
        var rtBtnVol = btnVolver.GetComponent<RectTransform>();
        rtBtnVol.anchorMin = new Vector2(0.5f, 0.18f); rtBtnVol.anchorMax = new Vector2(0.5f, 0.18f);
        rtBtnVol.pivot = new Vector2(0.5f, 0.5f); rtBtnVol.sizeDelta = new Vector2(350, 70);
        btnVolver.onClick.AddListener(RegresarAlMenu);
    }

    private void ConfigurarPreview3D(RawImage rawImg, int idEjercicio)
    {
        CharacterDatabase characterDb = Resources.Load<CharacterDatabase>("CharacterDatabase");

        if (characterDb == null || characterDb.avataresPrefabs == null || characterDb.avataresPrefabs.Length == 0)
        {
            // Fallback: mostrar emoji si no hay base de datos
            var fallbackGO = new GameObject("FallbackEmoji", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            fallbackGO.transform.SetParent(rawImg.transform, false);
            UIHelper.SetAnchorsStretch(fallbackGO.GetComponent<RectTransform>());
            var tmp = fallbackGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "🏋️";
            tmp.fontSize = 80;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            return;
        }

        // Crear RenderTexture
        _previewRT = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
        _previewRT.Create();
        rawImg.texture = _previewRT;

        // Posicionar el contenedor fuera de vista
        Vector3 posClon = new Vector3(0f, -3000f, 0f);
        _previewContainer = new GameObject("ContenedorPreviewSupervision");
        _previewContainer.transform.position = posClon;

        // Instanciar el avatar
        int idPersonaje = 0;
        if (GenosisFitDataManager.Instance != null)
            idPersonaje = GenosisFitDataManager.Instance.IndicePersonaje;
        if (idPersonaje >= characterDb.avataresPrefabs.Length) idPersonaje = 0;

        GameObject clon = Instantiate(characterDb.avataresPrefabs[idPersonaje], posClon, Quaternion.Euler(0f, 140f, 0f), _previewContainer.transform);
        clon.name = "ClonPreviewSupervision";
        _previewClone = clon.transform;

        // Configurar animación del ejercicio
        var animator = clon.GetComponent<Animator>();
        if (animator != null && characterDb.ejerciciosControllers != null)
        {
            int controllerIndex = idEjercicio;
            if (controllerIndex < 0 || controllerIndex >= characterDb.ejerciciosControllers.Length)
                controllerIndex = 0;
            if (characterDb.ejerciciosControllers.Length > 0)
                animator.runtimeAnimatorController = characterDb.ejerciciosControllers[controllerIndex];
        }

        // Configurar cámara dedicada
        GameObject camObj = new GameObject("CamaraPreviewSupervision", typeof(Camera));
        camObj.transform.SetParent(_previewContainer.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 0.55f, 2.2f);
        camObj.transform.LookAt(posClon + new Vector3(0f, 0.35f, 0f));

        Camera cam = camObj.GetComponent<Camera>();
        cam.fieldOfView = 30f;
        cam.targetTexture = _previewRT;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.12f, 0.18f, 1f); // Fondo igual al panel
    }

    private void LimpiarPreview3D()
    {
        if (_previewContainer != null) Destroy(_previewContainer);
        if (_previewRT != null)
        {
            _previewRT.Release();
            Destroy(_previewRT);
        }
    }

    private void BotonIniciarPresionado()
    {
        LimpiarPreview3D();
        if (panelPreInicio != null) Destroy(panelPreInicio);

        if (gestorSupervision != null) gestorSupervision.InicializarTexturaYClon();
        if (analisisPostura != null)
        {
            analisisPostura.ConfigurarEjercicio(tipoEjercicioPendiente);
            analisisPostura.enabled = true;
        }

        StartCoroutine(IniciarConHUD(nombreEjercicioPendiente));
    }

    private System.Collections.IEnumerator IniciarConHUD(string nombreEjercicio)
    {
        yield return StartCoroutine(layerManager.ActivarAsync());

        Canvas canvas = layerManager.ObtenerCanvasMaestro();
        if (canvas != null && analisisPostura != null)
        {
            var hud = canvas.gameObject.AddComponent<SupervisionHUD>();
            hud.Inicializar(canvas, analisisPostura, gestorSupervision, nombreEjercicio);

            var pantallaMediaPipe = layerManager.ObtenerPantallaMediaPipe();
            if (pantallaMediaPipe != null)
            {
                var skeletonGO = new GameObject("SkeletonOverlay", typeof(RectTransform));
                skeletonGO.transform.SetParent(pantallaMediaPipe, false);
                var rt = skeletonGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                skeletonGO.AddComponent<SkeletonOverlay>().Inicializar(canvas, analisisPostura);
            }
        }
    }

    private void OcultarUIMediaPipe()
    {
        var f = GameObject.Find("Footer"); if (f != null) f.SetActive(false);
        var h = GameObject.Find("Header"); if (h != null) h.SetActive(false);
        var m = GameObject.Find("Modal Panel"); if (m != null) m.SetActive(false);
    }

    public void RegresarAlMenu() { SceneManager.LoadScene("Scene_Menu"); }
}
