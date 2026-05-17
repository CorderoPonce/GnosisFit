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

        var icono = UIHelper.CrearTexto("Icono", canvasGO.transform, "🏋️", 100, Color.white);
        var rtIcono = icono.GetComponent<RectTransform>();
        rtIcono.anchorMin = new Vector2(0, 0.7f); rtIcono.anchorMax = new Vector2(1, 0.9f);
        rtIcono.sizeDelta = Vector2.zero; rtIcono.anchoredPosition = Vector2.zero;

        var title = UIHelper.CrearTexto("Titulo", canvasGO.transform, "¿LISTO PARA ENTRENAR?", 48, Color.white, FontStyles.Bold);
        var rtTitle = title.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0, 0.55f); rtTitle.anchorMax = new Vector2(1, 0.7f);
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

    private void BotonIniciarPresionado()
    {
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
                skeletonGO.AddComponent<SkeletonOverlay>().Inicializar(canvas);
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