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

    // ── database & tracking ──
    private CharacterDatabase characterDb;
    private List<GameObject> spawnedContainers = new List<GameObject>();
    private List<RenderTexture> spawnedRenderTextures = new List<RenderTexture>();
    private List<Transform> thumbnailClones = new List<Transform>();

    // ── procedurally generated sprites ──
    private Sprite whiteRoundedSprite;
    private Sprite cardRoundedSprite;

    // ── design tokens (Propuesta 1: Tema Claro Refinado) ──
    static readonly Color BG            = new Color(0.97f, 0.97f, 0.98f); // Fondo gris muy claro
    static readonly Color CARD_BG       = Color.white;
    static readonly Color IMG_PH        = new Color(0.92f, 0.93f, 0.95f, 1f); // Fondo azul metálico claro para la previsualización 3D
    static readonly Color BTN_AR        = new Color(0.10f, 0.75f, 0.65f); // Verde azulado Teal para RA
    static readonly Color BTN_POSTURE   = new Color(0.85f, 0.45f, 0.15f); // Naranja premium para Corrección de Postura
    static readonly Color ACTIVE_BG     = new Color(0.10f, 0.75f, 0.65f); // Teal para chip activo
    static readonly Color INACTIVE_BG   = new Color(0.93f, 0.93f, 0.94f); // Gris muy claro para chip inactivo
    static readonly Color TXT_DARK      = new Color(0.15f, 0.15f, 0.15f); // Texto principal oscuro
    static readonly Color TXT_GRAY      = new Color(0.55f, 0.55f, 0.55f); // Texto secundario gris

    void Awake()
    {
        // Generar Sprites redondeados suavizados (anti-aliased) en tiempo de ejecución
        // 1. Sprite redondeado blanco para botones, placeholders y chips
        whiteRoundedSprite = CreateRoundedSprite(128, 128, 32, Color.white, Color.clear, 0f);
        // 2. Sprite de tarjeta con fondo blanco y contorno gris suave de 2.5px
        cardRoundedSprite = CreateRoundedSprite(128, 128, 32, Color.white, new Color(0.88f, 0.89f, 0.90f, 1f), 2.5f);
    }

    void Start()
    {
        allExercises = ExerciseData.ObtenerCatalogo().ToList();
        characterDb = Resources.Load<CharacterDatabase>("CharacterDatabase");
        BuildUI();
    }

    void OnDestroy()
    {
        foreach (var container in spawnedContainers)
        {
            if (container != null) Destroy(container);
        }
        foreach (var rt in spawnedRenderTextures)
        {
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }
        }

        // Liberar texturas procedimentales para evitar fugas de memoria
        if (whiteRoundedSprite != null)
        {
            if (whiteRoundedSprite.texture != null) Destroy(whiteRoundedSprite.texture);
            Destroy(whiteRoundedSprite);
        }
        if (cardRoundedSprite != null)
        {
            if (cardRoundedSprite.texture != null) Destroy(cardRoundedSprite.texture);
            Destroy(cardRoundedSprite);
        }
    }

    // ════════════════════════════════════════
    //  BUILD THE ENTIRE UI (Programmatic)
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

        // ── Background Panel ──
        var bg = CreatePanel(canvasGO.transform, "Background", BG);
        Stretch(bg);

        var rootVL = bg.AddComponent<VerticalLayoutGroup>();
        rootVL.childControlWidth = true;
        rootVL.childControlHeight = true;
        rootVL.childForceExpandWidth = true;
        rootVL.childForceExpandHeight = false;
        rootVL.spacing = 0;
        rootVL.padding = new RectOffset(0, 0, 0, 0);

        // ── Title Bar ──
        var titleBar = CreatePanel(bg.transform, "TitleBar", BG);
        AddLE(titleBar, minH: 120);

        var titleTxt = CreateTMP(titleBar.transform, "Gnosis Fit", 54, TXT_DARK, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(titleTxt.gameObject);

        // ── Filter Section (Restored original stacked configuration) ──
        var filterSection = CreatePanel(bg.transform, "Filters", BG);
        AddLE(filterSection, minH: 260);
        
        var filterVL = filterSection.AddComponent<VerticalLayoutGroup>();
        filterVL.childControlWidth = true;
        filterVL.childControlHeight = true;
        filterVL.childForceExpandWidth = true;
        filterVL.childForceExpandHeight = false;
        filterVL.spacing = 10;
        filterVL.padding = new RectOffset(40, 40, 15, 15);

        // Muscle group label + row
        var muscleLabel = CreateTMP(filterSection.transform, "Grupo Muscular", 22, TXT_GRAY, FontStyles.Bold, TextAlignmentOptions.Left);
        AddLE(muscleLabel.gameObject, minH: 30);

        var muscleRow = CreateRow(filterSection.transform, "MuscleRow");
        string[] muscles = { "Todos", "Pecho", "Piernas", "Espalda", "Core", "Brazos" };
        foreach (var m in muscles)
        {
            var btn = CreateFilterBtn(muscleRow.transform, m);
            string cap = m;
            btn.onClick.AddListener(() => SetMuscle(cap));
            muscleButtons.Add(btn);
        }

        // Difficulty label + row
        var diffLabel = CreateTMP(filterSection.transform, "Dificultad", 22, TXT_GRAY, FontStyles.Bold, TextAlignmentOptions.Left);
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

        // ── ScrollView (Workouts list) ──
        var scrollGO = CreateScrollView(bg.transform);
        contentParent = scrollGO.transform;

        // ── Floating Chatbot FAB (esquina inferior derecha) ──
        var chatBtnGO = CreateRoundedPanel(canvasGO.transform, "BtnChatbot", BTN_AR, whiteRoundedSprite);
        var chatBtnRT = chatBtnGO.GetComponent<RectTransform>();
        chatBtnRT.anchorMin = new Vector2(1, 0);
        chatBtnRT.anchorMax = new Vector2(1, 0);
        chatBtnRT.pivot = new Vector2(1, 0);
        chatBtnRT.sizeDelta = new Vector2(312, 98);
        chatBtnRT.anchoredPosition = new Vector2(-50, 65);

        var chatShadow = chatBtnGO.AddComponent<Shadow>();
        chatShadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
        chatShadow.effectDistance = new Vector2(0f, -5f);

        // Plain text "Chatbot AI" (no emoji to prevent broken square box)
        var chatTxt = CreateTMP(chatBtnGO.transform, "Chatbot AI", 34, Color.white,
            FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(chatTxt.gameObject);

        var chatBtn = chatBtnGO.AddComponent<Button>();
        chatBtn.targetGraphic = chatBtnGO.GetComponent<Image>();
        var chatColors = chatBtn.colors;
        chatColors.highlightedColor = new Color(0.08f, 0.65f, 0.55f);
        chatColors.pressedColor = new Color(0.06f, 0.55f, 0.45f);
        chatBtn.colors = chatColors;
        chatBtn.onClick.AddListener(() => SceneManager.LoadScene("ChatbotMode"));

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
            b.GetComponentInChildren<TMP_Text>().color = active ? Color.white : TXT_DARK;
        }
        foreach (var b in diffButtons)
        {
            bool active = b.name == "Btn_" + currentDiff;
            b.GetComponent<Image>().color = active ? ACTIVE_BG : INACTIVE_BG;
            b.GetComponentInChildren<TMP_Text>().color = active ? Color.white : TXT_DARK;
        }
    }

    void RefreshCards()
    {
        // Clean up previous 3D thumbnail resources
        foreach (var container in spawnedContainers)
        {
            if (container != null) Destroy(container);
        }
        spawnedContainers.Clear();

        foreach (var rt in spawnedRenderTextures)
        {
            if (rt != null)
            {
                rt.Release();
                Destroy(rt);
            }
        }
        spawnedRenderTextures.Clear();
        thumbnailClones.Clear();

        // Clear children
        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);

        // Filter exercise list based on muscle group and difficulty
        var filtered = allExercises.Where(e =>
            (currentMuscle == "Todos" || e.grupoMuscular == currentMuscle) &&
            (currentDiff == "Todas" || e.dificultad == currentDiff)
        );

        foreach (var ex in filtered)
            CreateCard(contentParent, ex);
    }

    // ════════════════════════════════════════
    //  CARD BUILDER
    // ════════════════════════════════════════
    void CreateCard(Transform parent, ExerciseData data)
    {
        // Card rounded root panel (using procedural cardRoundedSprite)
        var card = CreateRoundedPanel(parent, "Card_" + data.nombre, CARD_BG, cardRoundedSprite);
        AddLE(card, minH: 240);
        
        var hl = card.AddComponent<HorizontalLayoutGroup>();
        hl.childControlWidth = true;
        hl.childControlHeight = true;
        hl.childForceExpandWidth = false;
        hl.childForceExpandHeight = true;
        hl.spacing = 25;
        hl.padding = new RectOffset(25, 25, 25, 25);
        hl.childAlignment = TextAnchor.MiddleLeft;

        // Card soft shadow
        var shadow = card.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.05f);
        shadow.effectDistance = new Vector2(0f, -4f);

        // ── 3D Thumbnail Panel (Rounded steel-blue background using procedural whiteRoundedSprite) ──
        var imgGO = CreateRoundedPanel(card.transform, "ImgPlaceholder", IMG_PH, whiteRoundedSprite);
        AddLE(imgGO, minW: 190, minH: 190);
        imgGO.AddComponent<RectMask2D>(); // Clean clipping of 3D avatar

        // 3D RawImage
        var rawImgGO = new GameObject("Thumbnail3D", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        rawImgGO.transform.SetParent(imgGO.transform, false);
        Stretch(rawImgGO);
        var rawImg = rawImgGO.GetComponent<RawImage>();

        // Run character 3D preview
        ConfigurarPreview3D(rawImg, data.idControlador);

        // ── Information Column ──
        var infoGO = CreatePanel(card.transform, "InfoCol", new Color(1f, 1f, 1f, 0f));
        AddLE(infoGO, flexW: 1);
        
        var vl = infoGO.AddComponent<VerticalLayoutGroup>();
        vl.childControlWidth = true;
        vl.childControlHeight = true;
        vl.childForceExpandWidth = true;
        vl.childForceExpandHeight = false;
        vl.spacing = 8;
        vl.padding = new RectOffset(0, 0, 5, 5);
        vl.childAlignment = TextAnchor.MiddleLeft;

        // Title
        var title = CreateTMP(infoGO.transform, data.nombre, 34, TXT_DARK, FontStyles.Bold, TextAlignmentOptions.Left);
        AddLE(title.gameObject, minH: 45);

        // Subtitle (Grupo Muscular & Dificultad)
        var sub = CreateTMP(infoGO.transform, data.grupoMuscular + " - " + data.dificultad, 24, TXT_GRAY, FontStyles.Normal, TextAlignmentOptions.Left);
        AddLE(sub.gameObject, minH: 35);

        // Spacer
        var spacer = new GameObject("Spacer", typeof(RectTransform));
        spacer.transform.SetParent(infoGO.transform, false);
        AddLE(spacer, flexH: 1);

        // Side-by-side buttons row
        var buttonsRow = new GameObject("ButtonsRow", typeof(RectTransform));
        buttonsRow.transform.SetParent(infoGO.transform, false);
        AddLE(buttonsRow, minH: 60);
        
        var btnHL = buttonsRow.AddComponent<HorizontalLayoutGroup>();
        btnHL.childControlWidth = true;
        btnHL.childControlHeight = true;
        btnHL.childForceExpandWidth = true;
        btnHL.childForceExpandHeight = true;
        btnHL.spacing = 15;

        // AR Button
        var btnAR_GO = CreateRoundedPanel(buttonsRow.transform, "BtnAR", BTN_AR, whiteRoundedSprite);
        var btnTxtAR = CreateTMP(btnAR_GO.transform, "Ver en AR", 21, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(btnTxtAR.gameObject);
        
        var btnAR = btnAR_GO.AddComponent<Button>();
        btnAR.targetGraphic = btnAR_GO.GetComponent<Image>();
        var colorsAR = btnAR.colors;
        colorsAR.highlightedColor = new Color(0.08f, 0.65f, 0.55f);
        colorsAR.pressedColor = new Color(0.06f, 0.55f, 0.45f);
        btnAR.colors = colorsAR;
        btnAR.onClick.AddListener(() => GoToAR(data));

        // Posture Correction Button
        var btnPosGO = CreateRoundedPanel(buttonsRow.transform, "BtnPosture", BTN_POSTURE, whiteRoundedSprite);
        var btnTxtPos = CreateTMP(btnPosGO.transform, "Corregir Postura", 21, Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        Stretch(btnTxtPos.gameObject);
        
        var btnPos = btnPosGO.AddComponent<Button>();
        btnPos.targetGraphic = btnPosGO.GetComponent<Image>();
        var colorsPos = btnPos.colors;
        colorsPos.highlightedColor = new Color(0.75f, 0.35f, 0.10f);
        colorsPos.pressedColor = new Color(0.65f, 0.30f, 0.08f);
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
    //  UI HELPERS & PROCEDURAL GENERATORS
    // ════════════════════════════════════════
    GameObject CreatePanel(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        go.GetComponent<Image>().color = color;
        return go;
    }

    GameObject CreateRoundedPanel(Transform parent, string name, Color color, Sprite customSprite)
    {
        var go = CreatePanel(parent, name, Color.white); // Image color must be White to allow tint multiplication
        var img = go.GetComponent<Image>();
        img.sprite = customSprite;
        img.type = Image.Type.Sliced;
        img.color = color; // Apply tint color programmatically
        return go;
    }

    TMP_Text CreateTMP(Transform parent, string text, float size, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject("Txt", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = align;
        tmp.textWrappingMode = TextWrappingModes.Normal;
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

    void AddLE(GameObject go, float minW = -1, float minH = -1, float flexW = -1, float flexH = -1)
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
        var go = CreateRoundedPanel(parent, "Btn_" + label, INACTIVE_BG, whiteRoundedSprite);
        AddLE(go, minH: 48, flexW: 1); // 48 height, expandable width
        
        var txt = CreateTMP(go.transform, label, 20, TXT_DARK, FontStyles.Normal, TextAlignmentOptions.Center);
        Stretch(txt.gameObject);
        
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = go.GetComponent<Image>();
        return btn;
    }

    GameObject CreateRow(Transform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        AddLE(go, minH: 48);
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
        // Scroll Root
        var scrollGO = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
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

        // Viewport
        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(scrollGO.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.sizeDelta = Vector2.zero;
        sr.viewport = vpRT;

        // Content
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
        vl.padding = new RectOffset(40, 40, 20, 40);

        var csf = content.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        sr.content = cRT;

        return content.transform;
    }

    private void ConfigurarPreview3D(RawImage rawImg, int idEjercicio)
    {
        if (characterDb == null || characterDb.avataresPrefabs == null || characterDb.avataresPrefabs.Length == 0)
        {
            var fallbackGO = new GameObject("FallbackEmoji", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            fallbackGO.transform.SetParent(rawImg.transform, false);
            Stretch(fallbackGO);
            var tmp = fallbackGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "🏋";
            tmp.fontSize = 50;
            tmp.color = TXT_GRAY;
            tmp.alignment = TextAlignmentOptions.Center;
            return;
        }

        // Create low-resolution RenderTexture
        RenderTexture rt = new RenderTexture(128, 128, 16, RenderTextureFormat.ARGB32);
        rt.Create();
        rawImg.texture = rt;
        spawnedRenderTextures.Add(rt);

        // Position avatar
        float offsetUnit = idEjercicio * 15f;
        Vector3 posClon = new Vector3(offsetUnit, -2000f, 0f);

        GameObject contenedor = new GameObject("ContenedorPreview_" + idEjercicio);
        contenedor.transform.position = posClon;
        spawnedContainers.Add(contenedor);

        // Instantiate clone
        GameObject clon = Instantiate(characterDb.avataresPrefabs[0], posClon, Quaternion.identity, contenedor.transform);
        clon.name = "Clon_" + idEjercicio;
        clon.transform.rotation = Quaternion.Euler(0f, 140f, 0f);
        thumbnailClones.Add(clon.transform);

        // Setup animator
        var animator = clon.GetComponent<Animator>();
        int controllerIndex = idEjercicio;
        if (controllerIndex < 0 || characterDb.ejerciciosControllers == null || controllerIndex >= characterDb.ejerciciosControllers.Length)
        {
            controllerIndex = 0;
        }

        if (animator != null && characterDb.ejerciciosControllers != null && characterDb.ejerciciosControllers.Length > 0)
        {
            animator.runtimeAnimatorController = characterDb.ejerciciosControllers[controllerIndex];
        }

        // Camera setup centered on avatar's waist/torso
        GameObject camObj = new GameObject("CamaraPreview_" + idEjercicio, typeof(Camera));
        camObj.transform.SetParent(contenedor.transform, false);
        camObj.transform.localPosition = new Vector3(0f, 0.55f, 2.2f);
        camObj.transform.LookAt(posClon + new Vector3(0f, 0.35f, 0f));

        Camera cam = camObj.GetComponent<Camera>();
        cam.fieldOfView = 30f; // Zoomed in 3D model
        cam.targetTexture = rt;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = IMG_PH; // Matching Theme 1 steel blue background
    }

    void Update()
    {
        // Rotate 3D avatars slowly
        float speed = 20f * Time.deltaTime;
        foreach (var clone in thumbnailClones)
        {
            if (clone != null)
            {
                clone.Rotate(Vector3.up, speed, Space.World);
            }
        }
    }

    // ── Procedural rounded rectangle texture generator with anti-aliasing ──
    private Sprite CreateRoundedSprite(int width, int height, int radius, Color fillColor, Color strokeColor, float strokeWidth)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Find distance from nearest corner center
                float cx = x;
                float cy = y;
                if (x < radius) cx = radius;
                else if (x >= width - radius) cx = width - radius - 1;
                
                if (y < radius) cy = radius;
                else if (y >= height - radius) cy = height - radius - 1;
                
                Color finalColor = Color.clear;
                
                if (cx != x || cy != y)
                {
                    // Inside corner quadrant
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    
                    float outerEdge = radius;
                    float innerEdge = radius - strokeWidth;
                    
                    if (dist > outerEdge + 0.5f)
                    {
                        finalColor = Color.clear;
                    }
                    else if (dist > outerEdge - 0.5f)
                    {
                        // Outer edge anti-aliasing
                        float t = (outerEdge + 0.5f - dist);
                        if (strokeWidth > 0)
                            finalColor = Color.Lerp(Color.clear, strokeColor, t);
                        else
                            finalColor = Color.Lerp(Color.clear, fillColor, t);
                    }
                    else if (strokeWidth > 0 && dist > innerEdge + 0.5f)
                    {
                        // Outline area
                        finalColor = strokeColor;
                    }
                    else if (strokeWidth > 0 && dist > innerEdge - 0.5f)
                    {
                        // Inner border anti-aliasing
                        float t = (innerEdge + 0.5f - dist);
                        finalColor = Color.Lerp(strokeColor, fillColor, t);
                    }
                    else
                    {
                        // Fill area
                        finalColor = fillColor;
                    }
                }
                else
                {
                    // Central/non-corner area
                    if (strokeWidth > 0)
                    {
                        float minEdgeDist = Mathf.Min(x, Mathf.Min(width - 1 - x, Mathf.Min(y, height - 1 - y)));
                        if (minEdgeDist < strokeWidth - 0.5f)
                        {
                            finalColor = strokeColor;
                        }
                        else if (minEdgeDist < strokeWidth + 0.5f)
                        {
                            float t = (minEdgeDist - (strokeWidth - 0.5f));
                            finalColor = Color.Lerp(strokeColor, fillColor, t);
                        }
                        else
                        {
                            finalColor = fillColor;
                        }
                    }
                    else
                    {
                        finalColor = fillColor;
                    }
                }
                
                cols[y * width + x] = finalColor;
            }
        }
        
        tex.SetPixels(cols);
        tex.Apply();
        
        // 9-slice borders configuration
        Vector4 border = new Vector4(radius, radius, radius, radius);
        Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, border);
        return sprite;
    }
}
