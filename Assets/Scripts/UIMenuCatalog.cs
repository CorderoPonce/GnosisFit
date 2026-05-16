using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Linq;

/// <summary>
/// Builds the entire Scene_Menu UI programmatically.
/// Attach to any GameObject in Scene_Menu (e.g. CatalogManager).
/// No prefab or manual UI setup required.
/// </summary>
public class UIMenuCatalog : MonoBehaviour
{
    // ── state ──
    private List<ExerciseData> allExercises;
    private string currentMuscle = "Todos";
    private string currentDiff = "Todas";

    // ── runtime refs ──
    private Transform contentParent;
    private List<Button> muscleButtons = new List<Button>();
    private List<Button> diffButtons = new List<Button>();

    // ── design tokens ──
    static readonly Color BG        = new Color(0.96f, 0.96f, 0.96f);
    static readonly Color CARD_BG   = Color.white;
    static readonly Color IMG_PH    = new Color(0.85f, 0.85f, 0.85f);
    static readonly Color BTN_AR    = new Color(0.15f, 0.75f, 0.65f);
    static readonly Color BTN_POSTURE = new Color(0.85f, 0.45f, 0.15f);
    static readonly Color ACTIVE_BG = new Color(0.15f, 0.15f, 0.15f);
    static readonly Color INACTIVE_BG = new Color(0.91f, 0.91f, 0.91f);
    static readonly Color TXT_DARK  = new Color(0.15f, 0.15f, 0.15f);
    static readonly Color TXT_GRAY  = new Color(0.5f, 0.5f, 0.5f);

    void Start()
    {
        allExercises = ExerciseData.ObtenerCatalogo().ToList();
        BuildUI();
    }

    // ════════════════════════════════════════
    //  BUILD THE ENTIRE UI
    // ════════════════════════════════════════
    void BuildUI()
    {
        // ── Canvas ──
        var canvasGO = new GameObject("MenuCanvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.matchWidthOrHeight = 1f;

        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Background panel ──
        var bg = CreatePanel(canvasGO.transform, "Background", BG);
        Stretch(bg);

        // ── Root vertical layout ──
        var rootVL = bg.AddComponent<VerticalLayoutGroup>();
        rootVL.childControlWidth = true;
        rootVL.childControlHeight = true;
        rootVL.childForceExpandWidth = true;
        rootVL.childForceExpandHeight = false;
        rootVL.spacing = 0;
        rootVL.padding = new RectOffset(0, 0, 0, 0);

        // ── Title ──
        var titleBar = CreatePanel(bg.transform, "TitleBar", BG);
        AddLE(titleBar, minH: 100);
        var titleTxt = CreateTMP(titleBar.transform,
            "Gnosis Fit", 54, TXT_DARK, FontStyles.Bold,
            TextAlignmentOptions.Center);
        Stretch(titleTxt.gameObject);

        // ── Filter Section ──
        var filterSection = CreatePanel(bg.transform, "Filters", BG);
        AddLE(filterSection, minH: 140);
        var filterVL = filterSection.AddComponent<VerticalLayoutGroup>();
        filterVL.childControlWidth = true;
        filterVL.childControlHeight = true;
        filterVL.childForceExpandWidth = true;
        filterVL.childForceExpandHeight = false;
        filterVL.spacing = 10;
        filterVL.padding = new RectOffset(20, 20, 10, 10);

        // Muscle group label + row
        var muscleLabel = CreateTMP(filterSection.transform,
            "Grupo Muscular", 24, TXT_GRAY, FontStyles.Normal,
            TextAlignmentOptions.Left);
        AddLE(muscleLabel.gameObject, minH: 30);

        var muscleRow = CreateRow(filterSection.transform, "MuscleRow");
        string[] muscles = { "Todos", "Pecho", "Piernas",
                             "Espalda", "Core", "Brazos" };
        foreach (var m in muscles)
        {
            var btn = CreateFilterBtn(muscleRow.transform, m);
            string cap = m;
            btn.onClick.AddListener(() => SetMuscle(cap));
            muscleButtons.Add(btn);
        }

        // Difficulty label + row
        var diffLabel = CreateTMP(filterSection.transform,
            "Dificultad", 24, TXT_GRAY, FontStyles.Normal,
            TextAlignmentOptions.Left);
        AddLE(diffLabel.gameObject, minH: 30);

        var diffRow = CreateRow(filterSection.transform, "DiffRow");
        string[] diffs = { "Todas", "Baja", "Media", "Alta" };
        foreach (var d in diffs)
        {
            var btn = CreateFilterBtn(diffRow.transform, d);
            string cap = d;
            btn.onClick.AddListener(() => SetDiff(cap));
            diffButtons.Add(btn);
        }

        // ── ScrollView ──
        var scrollGO = CreateScrollView(bg.transform);
        contentParent = scrollGO.transform;
        
        // highlight defaults
        RefreshFilterVisuals();
        RefreshCards();
    }

    // ════════════════════════════════════════
    //  FILTER LOGIC
    // ════════════════════════════════════════
    void SetMuscle(string m)
    {
        currentMuscle = m;
        RefreshFilterVisuals();
        RefreshCards();
    }

    void SetDiff(string d)
    {
        currentDiff = d;
        RefreshFilterVisuals();
        RefreshCards();
    }

    void RefreshFilterVisuals()
    {
        foreach (var b in muscleButtons)
        {
            bool active = b.name == "Btn_" + currentMuscle;
            b.GetComponent<Image>().color = active ? ACTIVE_BG : INACTIVE_BG;
            b.GetComponentInChildren<TMP_Text>().color =
                active ? Color.white : TXT_DARK;
        }
        foreach (var b in diffButtons)
        {
            bool active = b.name == "Btn_" + currentDiff;
            b.GetComponent<Image>().color = active ? ACTIVE_BG : INACTIVE_BG;
            b.GetComponentInChildren<TMP_Text>().color =
                active ? Color.white : TXT_DARK;
        }
    }

    void RefreshCards()
    {
        // clear
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        var filtered = allExercises.Where(e =>
            (currentMuscle == "Todos" ||
             e.grupoMuscular == currentMuscle) &&
            (currentDiff == "Todas" ||
             e.dificultad == currentDiff));

        foreach (var ex in filtered)
            CreateCard(contentParent, ex);
    }

    // ════════════════════════════════════════
    //  CARD BUILDER
    // ════════════════════════════════════════
    void CreateCard(Transform parent, ExerciseData data)
    {
        // Card root
        var card = CreatePanel(parent, "Card_" + data.nombre, CARD_BG);
        AddLE(card, minH: 200);
        var hl = card.AddComponent<HorizontalLayoutGroup>();
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;
        hl.spacing = 20;
        hl.padding = new RectOffset(20, 20, 20, 20);
        hl.childAlignment = TextAnchor.MiddleLeft;

        // shadow
        var shadow = card.AddComponent<Shadow>();
        shadow.effectColor = new Color(0, 0, 0, 0.15f);
        shadow.effectDistance = new Vector2(0, -3);

        // ── Image placeholder ──
        var imgGO = CreatePanel(card.transform, "ImgPlaceholder", IMG_PH);
        AddLE(imgGO, minW: 160, minH: 160);
        // Icon text
        var icon = CreateTMP(imgGO.transform, "🏋", 60,
            TXT_GRAY, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(icon.gameObject);

        // ── Info column ──
        var infoGO = CreatePanel(card.transform, "InfoCol",
            new Color(1, 1, 1, 0));
        AddLE(infoGO, flexW: 1);
        var vl = infoGO.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        vl.spacing = 8;
        vl.padding = new RectOffset(0, 0, 5, 5);
        vl.childAlignment = TextAnchor.MiddleLeft;

        // title
        var title = CreateTMP(infoGO.transform, data.nombre,
            36, TXT_DARK, FontStyles.Bold, TextAlignmentOptions.Left);
        AddLE(title.gameObject, minH: 45);

        // subtitle
        var sub = CreateTMP(infoGO.transform,
            data.grupoMuscular + " · " + data.dificultad,
            26, TXT_GRAY, FontStyles.Normal,
            TextAlignmentOptions.Left);
        AddLE(sub.gameObject, minH: 35);

        // spacer
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(infoGO.transform, false);
        AddLE(spacer, flexH: 1);

        // Buttons Row
        var buttonsRow = new GameObject("ButtonsRow", typeof(RectTransform));
        buttonsRow.transform.SetParent(infoGO.transform, false);
        AddLE(buttonsRow, minH: 50);
        var btnHL = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        btnHL.childControlWidth = true;
        btnHL.childControlHeight = true;
        btnHL.childForceExpandWidth = true;
        btnHL.childForceExpandHeight = true;
        btnHL.spacing = 15;

        // AR button
        var btnAR_GO = CreatePanel(buttonsRow.transform, "BtnAR", BTN_AR);
        var btnTxtAR = CreateTMP(btnAR_GO.transform, "Ver en RA",
            20, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
        Stretch(btnTxtAR.gameObject);
        var btnAR = btnAR_GO.AddComponent<Button>();
        btnAR.targetGraphic = btnAR_GO.GetComponent<Image>();
        var colorsAR = btnAR.colors;
        colorsAR.highlightedColor = new Color(0.1f, 0.6f, 0.5f);
        colorsAR.pressedColor = new Color(0.08f, 0.5f, 0.4f);
        btnAR.colors = colorsAR;
        btnAR.onClick.AddListener(() => GoToAR(data));

        // Posture Correction button
        var btnPosGO = CreatePanel(buttonsRow.transform, "BtnPosture", BTN_POSTURE);
        var btnTxtPos = CreateTMP(btnPosGO.transform, "Corregir Postura",
            20, Color.white, FontStyles.Bold,
            TextAlignmentOptions.Center);
        Stretch(btnTxtPos.gameObject);
        var btnPos = btnPosGO.AddComponent<Button>();
        btnPos.targetGraphic = btnPosGO.GetComponent<Image>();
        var colorsPos = btnPos.colors;
        colorsPos.highlightedColor = new Color(0.7f, 0.35f, 0.1f);
        colorsPos.pressedColor = new Color(0.6f, 0.3f, 0.1f);
        btnPos.colors = colorsPos;
        btnPos.onClick.AddListener(() => GoToSupervision(data));
    }

    void GoToAR(ExerciseData data)
    {
        if (GenosisFitDataManager.Instance != null)
        {
            GenosisFitDataManager.Instance.EjercicioSeleccionado = data.nombre;
            GenosisFitDataManager.Instance.TipoEjercicio = data.tipoSupervision;
            GenosisFitDataManager.Instance.IndiceEjercicio = data.idControlador;
            GenosisFitDataManager.Instance.VieneDeAR = false;
        }
        SceneManager.LoadScene("ARMode");
    }

    void GoToSupervision(ExerciseData data)
    {
        if (GenosisFitDataManager.Instance != null)
        {
            GenosisFitDataManager.Instance.EjercicioSeleccionado = data.nombre;
            GenosisFitDataManager.Instance.TipoEjercicio = data.tipoSupervision;
            GenosisFitDataManager.Instance.IndiceEjercicio = data.idControlador;
            GenosisFitDataManager.Instance.VieneDeAR = false;
        }
        SceneManager.LoadScene("SupervisionMode");
    }

    // ════════════════════════════════════════
    //  UI HELPERS
    // ════════════════════════════════════════
    GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform),
            typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    TMP_Text CreateTMP(Transform parent, string text, float size,
        Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject("Txt", typeof(RectTransform),
            typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        return tmp;
    }

    void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    void AddLE(GameObject go, float minW = -1, float minH = -1,
        float flexW = -1, float flexH = -1)
    {
        var le = go.GetComponent<LayoutElement>();
        if (le == null) le = go.AddComponent<LayoutElement>();
        if (minW >= 0) le.minWidth = minW;
        if (minH >= 0) le.minHeight = minH;
        if (flexW >= 0) le.flexibleWidth = flexW;
        if (flexH >= 0) le.flexibleHeight = flexH;
    }

    Button CreateFilterBtn(Transform parent, string label)
    {
        var go = CreatePanel(parent, "Btn_" + label, INACTIVE_BG);
        AddLE(go, minH: 40, flexW: 1);
        var txt = CreateTMP(go.transform, label, 20, TXT_DARK,
            FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(txt.gameObject);
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        return btn;
    }

    GameObject CreateRow(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        AddLE(go, minH: 45);
        var hl = go.AddComponent<HorizontalLayoutGroup>();
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = false;
        hl.spacing = 10;
        return go;
    }

    Transform CreateScrollView(Transform parent)
    {
        // scroll root
        var scrollGO = new GameObject("ScrollView",
            typeof(RectTransform), typeof(ScrollRect));
        scrollGO.transform.SetParent(parent, false);
        AddLE(scrollGO, flexH: 1);
        var scrollRT = scrollGO.GetComponent<RectTransform>();
        scrollRT.anchorMin = Vector2.zero;
        scrollRT.anchorMax = Vector2.one;
        scrollRT.sizeDelta = Vector2.zero;

        var sr = scrollGO.GetComponent<ScrollRect>();
        sr.horizontal = false;
        sr.vertical = true;
        sr.movementType = ScrollRect.MovementType.Elastic;

        // viewport
        var viewport = new GameObject("Viewport",
            typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGO.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = Vector2.zero;
        sr.viewport = vpRT;

        // content
        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var cRT = content.GetComponent<RectTransform>();
        cRT.anchorMin = new Vector2(0, 1);
        cRT.anchorMax = new Vector2(1, 1);
        cRT.pivot = new Vector2(0.5f, 1);
        cRT.sizeDelta = new Vector2(0, 0);

        var vl = content.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        vl.spacing = 24;
        vl.padding = new RectOffset(30, 30, 20, 40);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content = cRT;

        return content.transform;
    }
}
