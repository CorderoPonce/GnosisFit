using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GestorControlesAR : MonoBehaviour
{
    private GameObject panelHUD;
    private PlaceExample scriptAR;
    private ControladorHUD scriptHUD;

    // Velocidades
    private int[] indicesVelocidad = { 1, 2, 3 };
    private string[] nombresVelocidad = { "1.0x", "0.75x", "0.5x" };
    private TextMeshProUGUI textoVelocidadMain;
    private GameObject panelVelocidades;
    private bool velocidadesAbiertas = false;

    // Pausa
    private bool estaPausado = false;
    private TextMeshProUGUI textoPausa;

    // Estado Modelos
    private TextMeshProUGUI textoModeloMain;
    private GameObject panelModelos;
    private bool modelosAbiertos = false;
    private string[] nombresModelos = { "Modelo" };

    void Start()
    {
        scriptAR = FindFirstObjectByType<PlaceExample>();
        scriptHUD = FindFirstObjectByType<ControladorHUD>();

        if (scriptAR != null && scriptAR.avataresPrefabs != null && scriptAR.avataresPrefabs.Length > 0)
        {
            nombresModelos = new string[scriptAR.avataresPrefabs.Length];
            for(int i = 0; i < scriptAR.avataresPrefabs.Length; i++)
            {
                nombresModelos[i] = scriptAR.avataresPrefabs[i].name;
            }
        }

        CrearHUDFlotante();
    }

    void CrearHUDFlotante()
    {
        var canvasGO = new GameObject("ControlesARCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; 
        
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 0.5f;

        panelHUD = UIHelper.CrearPanel("ControlesFlotantes", canvasGO.transform, new Color(0,0,0,0));
        var rtPanel = panelHUD.GetComponent<RectTransform>();
        rtPanel.anchorMin = new Vector2(1, 1);
        rtPanel.anchorMax = new Vector2(1, 1);
        rtPanel.pivot = new Vector2(1, 1);
        rtPanel.sizeDelta = new Vector2(300, 300);
        rtPanel.anchoredPosition = new Vector2(-40, -140); 

        // 1. Botón Modelo (Abre sub-menú)
        var btnMod = UIHelper.CrearBoton("BtnModelo", panelHUD.transform, "Modelo Actual", new Color(0,0,0,0.6f), UIHelper.BLANCO, 26, true);
        var rtMod = btnMod.GetComponent<RectTransform>();
        rtMod.anchorMin = new Vector2(1, 1); rtMod.anchorMax = new Vector2(1, 1);
        rtMod.pivot = new Vector2(1, 1);
        rtMod.sizeDelta = new Vector2(220, 60);
        rtMod.anchoredPosition = new Vector2(0, 0);
        textoModeloMain = btnMod.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        btnMod.onClick.AddListener(ToggleMenuModelos);

        // Panel de sub-modelos
        panelModelos = UIHelper.CrearPanel("SubModelos", panelHUD.transform, new Color(0,0,0,0));
        var rtSubMod = panelModelos.GetComponent<RectTransform>();
        rtSubMod.anchorMin = new Vector2(1, 1); rtSubMod.anchorMax = new Vector2(1, 1);
        rtSubMod.pivot = new Vector2(1, 1);
        rtSubMod.sizeDelta = new Vector2(180, 220);
        rtSubMod.anchoredPosition = new Vector2(-240, 0); // A la izquierda del botón de Modelo

        for(int i = 0; i < nombresModelos.Length; i++) {
             int captureIndex = i;
             var subBtn = UIHelper.CrearBoton("SubMod" + i, panelModelos.transform, nombresModelos[i], UIHelper.GRIS_OSCURO, UIHelper.BLANCO, 24, true);
             var rtSubBtn = subBtn.GetComponent<RectTransform>();
             rtSubBtn.anchorMin = new Vector2(0, 1); rtSubBtn.anchorMax = new Vector2(1, 1);
             rtSubBtn.pivot = new Vector2(0.5f, 1);
             rtSubBtn.sizeDelta = new Vector2(0, 60);
             rtSubBtn.anchoredPosition = new Vector2(0, -(i * 70));
             subBtn.onClick.AddListener(() => SeleccionarModelo(captureIndex));
        }
        panelModelos.SetActive(false);

        // 2. Botón Velocidad (Abre sub-menú)
        var btnVel = UIHelper.CrearBoton("BtnVelocidad", panelHUD.transform, "Velocidad: 1.0x", new Color(0,0,0,0.6f), UIHelper.BLANCO, 26, true);
        var rtVel = btnVel.GetComponent<RectTransform>();
        rtVel.anchorMin = new Vector2(1, 1); rtVel.anchorMax = new Vector2(1, 1);
        rtVel.pivot = new Vector2(1, 1);
        rtVel.sizeDelta = new Vector2(220, 60);
        rtVel.anchoredPosition = new Vector2(0, -80);
        textoVelocidadMain = btnVel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        btnVel.onClick.AddListener(ToggleMenuVelocidades);

        // Panel de sub-velocidades
        panelVelocidades = UIHelper.CrearPanel("SubVelocidades", panelHUD.transform, new Color(0,0,0,0));
        var rtSub = panelVelocidades.GetComponent<RectTransform>();
        rtSub.anchorMin = new Vector2(1, 1); rtSub.anchorMax = new Vector2(1, 1);
        rtSub.pivot = new Vector2(1, 1);
        rtSub.sizeDelta = new Vector2(90, 220);
        rtSub.anchoredPosition = new Vector2(-240, -80); // A la izquierda del botón de velocidad

        for(int i = 0; i < nombresVelocidad.Length; i++) {
             int captureIndex = i;
             var subBtn = UIHelper.CrearBoton("Sub" + i, panelVelocidades.transform, nombresVelocidad[i], UIHelper.GRIS_OSCURO, UIHelper.BLANCO, 24, true);
             var rtSubBtn = subBtn.GetComponent<RectTransform>();
             rtSubBtn.anchorMin = new Vector2(0, 1); rtSubBtn.anchorMax = new Vector2(1, 1);
             rtSubBtn.pivot = new Vector2(0.5f, 1);
             rtSubBtn.sizeDelta = new Vector2(0, 60);
             rtSubBtn.anchoredPosition = new Vector2(0, -(i * 70));
             subBtn.onClick.AddListener(() => SeleccionarVelocidad(captureIndex));
        }
        panelVelocidades.SetActive(false);

        // 3. Botón Pausa
        var btnPausa = UIHelper.CrearBoton("BtnPausa", panelHUD.transform, "PAUSAR", new Color(0,0,0,0.6f), UIHelper.BLANCO, 26, true);
        var rtPausa = btnPausa.GetComponent<RectTransform>();
        rtPausa.anchorMin = new Vector2(1, 1); rtPausa.anchorMax = new Vector2(1, 1);
        rtPausa.pivot = new Vector2(1, 1);
        rtPausa.sizeDelta = new Vector2(220, 60);
        rtPausa.anchoredPosition = new Vector2(0, -160);
        textoPausa = btnPausa.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        btnPausa.onClick.AddListener(AlternarPausa);
    }

    void ToggleMenuModelos()
    {
        modelosAbiertos = !modelosAbiertos;
        panelModelos.SetActive(modelosAbiertos);
    }

    void SeleccionarModelo(int index)
    {
        textoModeloMain.text = nombresModelos[index];
        modelosAbiertos = false;
        panelModelos.SetActive(false);

        if (scriptAR != null) scriptAR.CambiarPersonaje(index + 1);
    }

    void ToggleMenuVelocidades()
    {
        velocidadesAbiertas = !velocidadesAbiertas;
        panelVelocidades.SetActive(velocidadesAbiertas);
    }

    void SeleccionarVelocidad(int index)
    {
        textoVelocidadMain.text = "Velocidad: " + nombresVelocidad[index];
        velocidadesAbiertas = false;
        panelVelocidades.SetActive(false);

        if (scriptAR != null) scriptAR.CambiarVelocidadAnimacion(indicesVelocidad[index]);
        
        // Al cambiar velocidad por menú, se asume que se reanuda
        if (estaPausado) AlternarPausa();
    }

    void AlternarPausa()
    {
        estaPausado = !estaPausado;
        if (estaPausado)
        {
            textoPausa.text = "REANUDAR";
            if (scriptAR != null) scriptAR.CambiarVelocidadAnimacion(4); // 4 = Pausa en PlaceExample
        }
        else
        {
            textoPausa.text = "PAUSAR";
            if (scriptAR != null) scriptAR.AlternarPausaAnimacion(); // Restaura la velocidad en base a la variable interna de PlaceExample
        }
    }

    void Update()
    {
        if (scriptHUD != null && panelHUD != null && scriptAR != null)
        {
            bool menuAbierto = (scriptHUD.panelConfiguracion != null && scriptHUD.panelConfiguracion.activeSelf);
            bool chatAbierto = (scriptHUD.panelChat != null && scriptHUD.panelChat.activeSelf);
            bool hayModeloInstanciado = (scriptAR.modeloInstanciado != null);
            
            // Solo se muestra el HUD si la AR está a la vista Y si ya se hizo clic en el suelo para instanciar a alguien
            panelHUD.SetActive(!menuAbierto && !chatAbierto && hayModeloInstanciado);
        }
    }
}
