using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GestorCatalogoVisual : MonoBehaviour
{
    private ExerciseData[] catalogo;
    private string filtroGrupo = "Todos";
    private string filtroDificultad = "Todas";
    
    private GameObject panelFiltrosGrupo;
    private GameObject panelFiltrosDificultad;
    private ScrollRect scrollEjercicios;
    private Transform contenedorTarjetas;

    private List<GameObject> tarjetasActivadas = new List<GameObject>();
    private List<Button> botonesGrupo = new List<Button>();
    private List<Button> botonesDificultad = new List<Button>();

    private PlaceExample scriptAR;

    void Start()
    {
        // Encontrar dependencias
        scriptAR = FindObjectOfType<PlaceExample>();
        var hud = FindObjectOfType<ControladorHUD>();

        if (hud != null && hud.panelConfiguracion != null)
        {
            catalogo = ExerciseData.ObtenerCatalogo();
            ConstruirInterfazMockup(hud.panelConfiguracion.transform);
        }
    }

    void ConstruirInterfazMockup(Transform padre)
    {
        // Limpiar el panel actual (elimina los botones viejos de "Ejercicios", "Velocidad", "Modelos")
        foreach (Transform child in padre)
        {
            Destroy(child.gameObject);
        }

        // Destruir LayoutGroups viejos para que no arruinen nuestra posición absoluta
        var layout = padre.GetComponent<UnityEngine.UI.LayoutGroup>();
        if (layout != null) Destroy(layout);
        var csf = padre.GetComponent<UnityEngine.UI.ContentSizeFitter>();
        if (csf != null) Destroy(csf);

        // Configurar el panel padre (MenuConfiguracion) para que ocupe todo menos la barra inferior
        var rtPadre = padre.GetComponent<RectTransform>();
        if (rtPadre != null)
        {
            // Oculta el fondo semi-transparente y le pone fondo blanco/gris claro del mockup
            var imgPadre = padre.GetComponent<Image>();
            if (imgPadre != null) imgPadre.color = UIHelper.BLANCO;
        }

        float yOffset = 0;
        yOffset = CrearHeader(padre, yOffset);
        yOffset = CrearFiltrosGrupo(padre, yOffset);
        yOffset = CrearFiltrosDificultad(padre, yOffset);
        CrearScrollEjercicios(padre, yOffset);

        FiltrarEjercicios();
    }

    float CrearHeader(Transform padre, float yOff)
    {
        float h = 140; // Un poco más alto para verse bien arriba
        var panel = UIHelper.CrearPanel("Header", padre, UIHelper.BLANCO);
        var rt = panel.GetComponent<RectTransform>();
        UIHelper.SetAnchorsStretch(rt);
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.sizeDelta = new Vector2(0, h);
        rt.anchoredPosition = new Vector2(0, -h/2);

        // Menú icon
        var menu = UIHelper.CrearTexto("Menu", panel.transform, "☰", 40, UIHelper.GRIS_OSCURO, FontStyles.Bold);
        var mrt = menu.GetComponent<RectTransform>();
        mrt.anchorMin = new Vector2(0, 0.5f); mrt.anchorMax = new Vector2(0, 0.5f);
        mrt.pivot = new Vector2(0, 0.5f);
        mrt.anchoredPosition = new Vector2(40, -10); mrt.sizeDelta = new Vector2(60, 60);

        // Título
        var titulo = UIHelper.CrearTexto("Titulo", panel.transform, "Gnosis Fit", 45, UIHelper.NEGRO, FontStyles.Bold);
        var trt = titulo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0.5f, 0.5f); trt.anchorMax = new Vector2(0.5f, 0.5f);
        trt.sizeDelta = new Vector2(400, 60); trt.anchoredPosition = new Vector2(0, -10);

        // Lupa icon
        var search = UIHelper.CrearTexto("Search", panel.transform, "🔍", 36, UIHelper.GRIS_OSCURO);
        var srt = search.GetComponent<RectTransform>();
        srt.anchorMin = new Vector2(1, 0.5f); srt.anchorMax = new Vector2(1, 0.5f);
        srt.pivot = new Vector2(1, 0.5f);
        srt.anchoredPosition = new Vector2(-40, -10); srt.sizeDelta = new Vector2(60, 60);

        return h;
    }

    float CrearFiltrosGrupo(Transform padre, float yOff)
    {
        float h = 120;
        var panel = UIHelper.CrearPanel("FiltrosGrupo", padre, UIHelper.BLANCO);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(1, 1);
        prt.sizeDelta = new Vector2(0, h); prt.anchoredPosition = new Vector2(0, -yOff - h/2);

        var titulo = UIHelper.CrearTexto("Label", panel.transform, "Grupo Muscular", 32, UIHelper.GRIS_OSCURO, FontStyles.Bold);
        var trt = titulo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(0, 1);
        trt.pivot = new Vector2(0, 1);
        trt.anchoredPosition = new Vector2(40, -10); trt.sizeDelta = new Vector2(300, 40);
        titulo.alignment = TextAlignmentOptions.Left;

        // Container gris redondeado para los botones
        var bgContainer = UIHelper.CrearPanel("BG", panel.transform, UIHelper.GRIS_MEDIO, true);
        var bgRt = bgContainer.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0); bgRt.anchorMax = new Vector2(1, 0);
        bgRt.pivot = new Vector2(0.5f, 0);
        bgRt.sizeDelta = new Vector2(-80, 60); // 40px margin left right
        bgRt.anchoredPosition = new Vector2(0, 10);

        string[] opciones = { "Todos", "Pecho", "Piernas", "Espalda" };
        float btnWidth = 1f / opciones.Length;

        for (int i = 0; i < opciones.Length; i++)
        {
            string op = opciones[i];
            var btn = UIHelper.CrearBoton(op, bgContainer.transform, op, new Color(0,0,0,0), UIHelper.BLANCO, 28, true);
            var brt = btn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(i * btnWidth, 0);
            brt.anchorMax = new Vector2((i + 1) * btnWidth, 1);
            brt.sizeDelta = Vector2.zero;
            brt.anchoredPosition = Vector2.zero;

            btn.onClick.AddListener(() => { SeleccionarGrupo(op, btn); });
            botonesGrupo.Add(btn);

            if (i == 0) SeleccionarGrupo(op, btn); // Default
        }

        return yOff + h;
    }

    float CrearFiltrosDificultad(Transform padre, float yOff)
    {
        float h = 120;
        var panel = UIHelper.CrearPanel("FiltrosDificultad", padre, UIHelper.BLANCO);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = new Vector2(0, 1); prt.anchorMax = new Vector2(1, 1);
        prt.sizeDelta = new Vector2(0, h); prt.anchoredPosition = new Vector2(0, -yOff - h/2);

        var titulo = UIHelper.CrearTexto("Label", panel.transform, "Dificultad", 32, UIHelper.GRIS_OSCURO, FontStyles.Bold);
        var trt = titulo.GetComponent<RectTransform>();
        trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(0, 1);
        trt.pivot = new Vector2(0, 1);
        trt.anchoredPosition = new Vector2(40, -10); trt.sizeDelta = new Vector2(300, 40);
        titulo.alignment = TextAlignmentOptions.Left;

        var bgContainer = UIHelper.CrearPanel("BG", panel.transform, UIHelper.GRIS_MEDIO, true);
        var bgRt = bgContainer.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0, 0); bgRt.anchorMax = new Vector2(1, 0);
        bgRt.pivot = new Vector2(0.5f, 0);
        bgRt.sizeDelta = new Vector2(-80, 60);
        bgRt.anchoredPosition = new Vector2(0, 10);

        string[] opciones = { "Baja", "Media", "Alta" };
        float btnWidth = 1f / opciones.Length;

        for (int i = 0; i < opciones.Length; i++)
        {
            string op = opciones[i];
            var btn = UIHelper.CrearBoton(op, bgContainer.transform, op, new Color(0,0,0,0), UIHelper.BLANCO, 28, true);
            var brt = btn.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(i * btnWidth, 0);
            brt.anchorMax = new Vector2((i + 1) * btnWidth, 1);
            brt.sizeDelta = Vector2.zero;
            brt.anchoredPosition = Vector2.zero;

            btn.onClick.AddListener(() => { SeleccionarDificultad(op, btn); });
            botonesDificultad.Add(btn);

            if (i == 1) SeleccionarDificultad(op, btn); // Default "Media" según mockup
        }

        return yOff + h;
    }

    void CrearScrollEjercicios(Transform padre, float yOff)
    {
        var go = new GameObject("ScrollEjercicios", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 1); // Llena el resto
        rt.offsetMax = new Vector2(0, -yOff);
        rt.offsetMin = new Vector2(0, 0); 
        
        var img = go.GetComponent<Image>();
        img.color = UIHelper.BLANCO;

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Mask), typeof(Image));
        viewport.transform.SetParent(go.transform, false);
        UIHelper.SetAnchorsStretch(viewport.GetComponent<RectTransform>());
        viewport.GetComponent<Image>().color = new Color(1,1,1,0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        content.transform.SetParent(viewport.transform, false);
        var crt = content.GetComponent<RectTransform>();
        crt.anchorMin = new Vector2(0, 1); crt.anchorMax = new Vector2(1, 1);
        crt.pivot = new Vector2(0.5f, 1);
        crt.sizeDelta = new Vector2(0, 0);
        crt.anchoredPosition = Vector2.zero;

        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(40, 40, 20, 200); // 200 bottom padding para la barra
        vlg.spacing = 30;
        vlg.childControlHeight = false;
        vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;

        var csf = content.GetComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollEjercicios = go.GetComponent<ScrollRect>();
        scrollEjercicios.content = crt;
        scrollEjercicios.viewport = viewport.GetComponent<RectTransform>();
        scrollEjercicios.horizontal = false;
        scrollEjercicios.vertical = true;
        scrollEjercicios.scrollSensitivity = 50f;

        contenedorTarjetas = content.transform;
    }

    void SeleccionarGrupo(string grupo, Button btnActivo)
    {
        filtroGrupo = grupo;
        foreach (var b in botonesGrupo)
        {
            b.GetComponent<Image>().color = new Color(0,0,0,0);
        }
        btnActivo.GetComponent<Image>().color = UIHelper.GRIS_OSCURO;
        FiltrarEjercicios();
    }

    void SeleccionarDificultad(string dif, Button btnActivo)
    {
        // En el mockup la dificultad es "Todas" si no hay, pero usemos un toggle.
        // Si clickeamos la que ya estaba, la deseleccionamos ("Todas")
        if (filtroDificultad == dif)
        {
            filtroDificultad = "Todas";
            foreach (var b in botonesDificultad) b.GetComponent<Image>().color = new Color(0,0,0,0);
        }
        else
        {
            filtroDificultad = dif;
            foreach (var b in botonesDificultad) b.GetComponent<Image>().color = new Color(0,0,0,0);
            btnActivo.GetComponent<Image>().color = UIHelper.GRIS_OSCURO;
        }
        
        FiltrarEjercicios();
    }

    void FiltrarEjercicios()
    {
        foreach (var t in tarjetasActivadas) Destroy(t);
        tarjetasActivadas.Clear();

        foreach (var ej in catalogo)
        {
            bool matchG = (filtroGrupo == "Todos" || ej.grupoMuscular == filtroGrupo);
            bool matchD = (filtroDificultad == "Todas" || ej.dificultad == filtroDificultad);

            if (matchG && matchD)
            {
                CrearTarjetaEjercicio(ej);
            }
        }
        
        // Forzar layout update
        Canvas.ForceUpdateCanvases();
    }

    void CrearTarjetaEjercicio(ExerciseData ej)
    {
        var tarjeta = UIHelper.CrearPanel("Tarjeta_" + ej.nombre, contenedorTarjetas, UIHelper.GRIS_TARJETA, true);
        var trt = tarjeta.GetComponent<RectTransform>();
        trt.sizeDelta = new Vector2(0, 220); // Height of card

        // Placeholder Imagen (Cuadro oscuro)
        var imgHolder = UIHelper.CrearPanel("Img", tarjeta.transform, UIHelper.GRIS_OSCURO, true);
        var irt = imgHolder.GetComponent<RectTransform>();
        irt.anchorMin = new Vector2(0, 0.5f); irt.anchorMax = new Vector2(0, 0.5f);
        irt.pivot = new Vector2(0, 0.5f);
        irt.anchoredPosition = new Vector2(30, 0); irt.sizeDelta = new Vector2(180, 180);

        // Nombre Ejercicio
        var nom = UIHelper.CrearTexto("Nombre", tarjeta.transform, ej.nombre, 34, UIHelper.NEGRO, FontStyles.Bold);
        var nrt = nom.GetComponent<RectTransform>();
        nrt.anchorMin = new Vector2(0, 1); nrt.anchorMax = new Vector2(1, 1);
        nrt.pivot = new Vector2(0, 1);
        nrt.anchoredPosition = new Vector2(240, -30); nrt.sizeDelta = new Vector2(-260, 45);
        nom.alignment = TextAlignmentOptions.Left;

        // Subtitulo (Grupo Muscular)
        var grupo = UIHelper.CrearTexto("Grupo", tarjeta.transform, ej.grupoMuscular, 26, UIHelper.GRIS_MEDIO);
        var grt = grupo.GetComponent<RectTransform>();
        grt.anchorMin = new Vector2(0, 1); grt.anchorMax = new Vector2(1, 1);
        grt.pivot = new Vector2(0, 1);
        grt.anchoredPosition = new Vector2(240, -75); grt.sizeDelta = new Vector2(-260, 35);
        grupo.alignment = TextAlignmentOptions.Left;

        // Dificultad
        var dif = UIHelper.CrearTexto("Dificultad", tarjeta.transform, ej.dificultad, 26, UIHelper.GRIS_MEDIO);
        var drt = dif.GetComponent<RectTransform>();
        drt.anchorMin = new Vector2(0, 1); drt.anchorMax = new Vector2(1, 1);
        drt.pivot = new Vector2(0, 1);
        drt.anchoredPosition = new Vector2(240, -110); drt.sizeDelta = new Vector2(-260, 35);
        dif.alignment = TextAlignmentOptions.Left;

        // Botón "Ver en RA"
        var btn = UIHelper.CrearBoton("BtnRA", tarjeta.transform, "Ver en RA", UIHelper.AZUL_BOTON, UIHelper.BLANCO, 28, true);
        var brt = btn.GetComponent<RectTransform>();
        brt.anchorMin = new Vector2(0, 0); brt.anchorMax = new Vector2(1, 0);
        brt.pivot = new Vector2(0, 0);
        brt.anchoredPosition = new Vector2(240, 20); brt.sizeDelta = new Vector2(-260, 60);
        
        btn.onClick.AddListener(() => {
            if (scriptAR != null) {
                scriptAR.CambiarEjercicio(ej.idControlador);
                
                // Cerrar el menú al seleccionar
                var hud = FindObjectOfType<ControladorHUD>();
                if (hud != null) hud.CerrarTodo();
            }
        });

        tarjetasActivadas.Add(tarjeta);
    }
}
