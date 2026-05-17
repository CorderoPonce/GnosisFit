using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class InitSupervisionScene : MonoBehaviour
{
    [Header("Referencias")]
    public SupervisionLayerManager layerManager;
    public AnalisisPostura analisisPostura;

    // Variables para guardar el estado antes de iniciar
    private GameObject panelPreInicio;
    private string nombreEjercicioPendiente;
    private TipoSupervision tipoEjercicioPendiente;

    private void Start()
    {
        Debug.Log("[SupervisionScene] Inicializando modo dedicado...");

        // Valores por defecto
        nombreEjercicioPendiente = "Corrección de Postura";
        tipoEjercicioPendiente = TipoSupervision.BicepCurl; 

        // Leer datos de tu Data Manager
        if (GenosisFitDataManager.Instance != null)
        {
            tipoEjercicioPendiente = GenosisFitDataManager.Instance.TipoEjercicio;
            nombreEjercicioPendiente = GenosisFitDataManager.Instance.EjercicioSeleccionado;
            Debug.Log("[SupervisionScene] Tipo de ejercicio detectado: " + tipoEjercicioPendiente);
        }
        else
        {
            Debug.LogWarning("[SupervisionScene] Iniciando sin DataManager, asumiendo Bicep Curl.");
        }

        // Ocultar la UI nativa de MediaPipe que no usamos
        OcultarUIMediaPipe();

        if (layerManager != null)
        {
            // NUEVO: En lugar de iniciar todo, mostramos la pantalla de Pre-Inicio
            CrearUIPreInicio();
        }
        else
        {
            Debug.LogError("[SupervisionScene] No se encontró LayerManager en la escena.");
        }
    }

    private void CrearUIPreInicio()
    {
        // Creamos un Canvas temporal que estará por encima de todo
        var canvasGO = new GameObject("CanvasPreInicio");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // Capa superior

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        panelPreInicio = canvasGO;

        // Fondo oscuro premium
        var bg = UIHelper.CrearPanel("Bg", canvasGO.transform, new Color(0.08f, 0.08f, 0.12f, 1f));
        UIHelper.SetAnchorsStretch(bg.GetComponent<RectTransform>());

        // Icono decorativo
        var icono = UIHelper.CrearTexto("Icono", canvasGO.transform, "🏋️", 100, Color.white, FontStyles.Normal);
        var rtIcono = icono.GetComponent<RectTransform>();
        rtIcono.anchorMin = new Vector2(0, 0.7f); rtIcono.anchorMax = new Vector2(1, 0.9f);
        rtIcono.sizeDelta = Vector2.zero; rtIcono.anchoredPosition = Vector2.zero;

        // Título principal
        var title = UIHelper.CrearTexto("Titulo", canvasGO.transform, "¿LISTO PARA ENTRENAR?", 48, Color.white, FontStyles.Bold);
        var rtTitle = title.GetComponent<RectTransform>();
        rtTitle.anchorMin = new Vector2(0, 0.55f); rtTitle.anchorMax = new Vector2(1, 0.7f);
        rtTitle.sizeDelta = Vector2.zero; rtTitle.anchoredPosition = Vector2.zero;

        // Subtítulo (Nombre del ejercicio)
        var sub = UIHelper.CrearTexto("Sub", canvasGO.transform, nombreEjercicioPendiente.ToUpper(), 36, new Color(0.2f, 0.75f, 1f), FontStyles.Bold);
        var rtSub = sub.GetComponent<RectTransform>();
        rtSub.anchorMin = new Vector2(0, 0.45f); rtSub.anchorMax = new Vector2(1, 0.55f);
        rtSub.sizeDelta = Vector2.zero; rtSub.anchoredPosition = Vector2.zero;

        // Botón: INICIAR ENTRENAMIENTO
        var btnIniciar = UIHelper.CrearBoton("BtnIniciar", canvasGO.transform, "INICIAR ENTRENAMIENTO", new Color(0.00f, 0.75f, 0.65f, 1f), Color.white, 28, true);
        var rtBtnIni = btnIniciar.GetComponent<RectTransform>();
        rtBtnIni.anchorMin = new Vector2(0.5f, 0.3f); rtBtnIni.anchorMax = new Vector2(0.5f, 0.3f);
        rtBtnIni.pivot = new Vector2(0.5f, 0.5f);
        rtBtnIni.sizeDelta = new Vector2(450, 90);
        rtBtnIni.anchoredPosition = Vector2.zero;
        btnIniciar.onClick.AddListener(BotonIniciarPresionado);

        // Botón: VOLVER AL MENÚ
        var btnVolver = UIHelper.CrearBoton("BtnVolver", canvasGO.transform, "VOLVER AL MENÚ", new Color(0.8f, 0.2f, 0.2f, 0.9f), Color.white, 24, true);
        var rtBtnVol = btnVolver.GetComponent<RectTransform>();
        rtBtnVol.anchorMin = new Vector2(0.5f, 0.18f); rtBtnVol.anchorMax = new Vector2(0.5f, 0.18f);
        rtBtnVol.pivot = new Vector2(0.5f, 0.5f);
        rtBtnVol.sizeDelta = new Vector2(350, 70);
        rtBtnVol.anchoredPosition = Vector2.zero;
        btnVolver.onClick.AddListener(RegresarAlMenu);
    }

    private void BotonIniciarPresionado()
    {
        // 1. Destruimos la pantalla de pre-inicio
        if (panelPreInicio != null) Destroy(panelPreInicio);

        // 2. Configuramos el análisis JUSTO AHORA (esto asegura que el cronómetro de la rutina empiece en 0s)
        if (analisisPostura != null)
        {
            analisisPostura.ConfigurarEjercicio(tipoEjercicioPendiente);
            analisisPostura.enabled = true;
        }

        // 3. Arrancamos el motor de MediaPipe, la cámara y el HUD de supervisión real
        StartCoroutine(IniciarConHUD(nombreEjercicioPendiente));
    }

    private System.Collections.IEnumerator IniciarConHUD(string nombreEjercicio)
    {
        // 1. Activar el pipeline de MediaPipe
        yield return StartCoroutine(layerManager.ActivarAsync());

        // 2. Crear el HUD de supervisión sobre el canvas
        Canvas canvas = layerManager.ObtenerCanvasMaestro();
        if (canvas != null && analisisPostura != null)
        {
            var hud = canvas.gameObject.AddComponent<SupervisionHUD>();
            hud.Inicializar(canvas, analisisPostura, null, nombreEjercicio);
            Debug.Log("[SupervisionScene] HUD de supervisión creado.");

            // 3. Crear el overlay de esqueleto sobre la pantalla de MediaPipe
            var pantallaMediaPipe = layerManager.ObtenerPantallaMediaPipe();
            if (pantallaMediaPipe != null)
            {
                var skeletonGO = new GameObject("SkeletonOverlay", typeof(RectTransform));
                skeletonGO.transform.SetParent(pantallaMediaPipe, false);
                
                var rt = skeletonGO.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var skeletonOverlay = skeletonGO.AddComponent<SkeletonOverlay>();
                // Inyección de dependencia para los colores dinámicos
                skeletonOverlay.Inicializar(canvas, analisisPostura);
                Debug.Log("[SupervisionScene] SkeletonOverlay creado sobre " + pantallaMediaPipe.name);
            }
            else
            {
                Debug.LogWarning("[SupervisionScene] No se encontró la pantalla de MediaPipe para el SkeletonOverlay.");
            }
        }
        else
        {
            Debug.LogWarning("[SupervisionScene] No se pudo crear HUD. Canvas: " + (canvas != null) + " Analisis: " + (analisisPostura != null));
        }
    }

    private void OcultarUIMediaPipe()
    {
        // Ocultar el Footer (botones Graph Config y ImageSource Config)
        var footer = GameObject.Find("Footer");
        if (footer != null)
        {
            footer.SetActive(false);
            Debug.Log("[SupervisionScene] Footer de MediaPipe ocultado.");
        }

        // Ocultar el Header (MenuButton hamburguesa)
        var header = GameObject.Find("Header");
        if (header != null)
        {
            header.SetActive(false);
            Debug.Log("[SupervisionScene] Header de MediaPipe ocultado.");
        }

        // Ocultar Modal Panel si existe
        var modal = GameObject.Find("Modal Panel");
        if (modal != null)
        {
            modal.SetActive(false);
        }
    }

    public void RegresarAlMenu()
    {
        Debug.Log("[SupervisionScene] Regresando al Menú Principal...");
        SceneManager.LoadScene("Scene_Menu");
    }
}