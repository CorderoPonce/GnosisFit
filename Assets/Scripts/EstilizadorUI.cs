using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Aplica mejoras visuales en runtime a los elementos UI existentes en la escena.
/// Busca los GameObjects por nombre y les aplica colores, estilos y efectos.
/// No requiere modificar la escena manualmente.
/// </summary>
public class EstilizadorUI : MonoBehaviour
{
    // Paleta de colores
    static readonly Color COLOR_NAVBAR_BG       = new Color(0.08f, 0.08f, 0.10f, 0.92f);
    static readonly Color COLOR_BOTON_ACTIVO    = new Color(0.10f, 0.75f, 0.65f, 1f);    // Teal (Sincronizado con UIMenuCatalog.cs BTN_AR)
    static readonly Color COLOR_BOTON_INACTIVO  = new Color(0.55f, 0.55f, 0.60f, 1f);    // Gris
    static readonly Color COLOR_PANEL_CONFIG    = new Color(0.10f, 0.10f, 0.14f, 0.95f); // Oscuro
    static readonly Color COLOR_BOTON_CONFIG    = new Color(0.18f, 0.18f, 0.24f, 1f);    // Card oscura
    static readonly Color COLOR_BOTON_CONFIG_TX = new Color(0.95f, 0.95f, 0.95f, 1f);    // Texto claro
    static readonly Color COLOR_X_BTN           = new Color(0.9f,  0.25f, 0.25f, 1f);    // Rojo suave
    static readonly Color COLOR_CHAT_HEADER     = new Color(0.06f, 0.06f, 0.10f, 1f);
    static readonly Color COLOR_CHAT_BG         = new Color(0.12f, 0.12f, 0.18f, 1f);
    static readonly Color COLOR_ENVIAR          = new Color(0.10f, 0.75f, 0.65f, 1f);
    static readonly Color COLOR_INPUT_BG        = new Color(0.18f, 0.18f, 0.24f, 1f);
    static readonly Color COLOR_TEXT_WHITE      = new Color(0.95f, 0.95f, 0.95f, 1f);

    private Sprite whiteRoundedSprite;

    void Awake()
    {
        // Generar sprite redondeado nativo para la escena de AR
        whiteRoundedSprite = CreateRoundedSprite(128, 128, 32, Color.white);
    }

    void Start()
    {
        // Pequeña espera para que todos los componentes estén inicializados
        Invoke(nameof(AplicarEstilos), 0.05f);
    }

    void OnDestroy()
    {
        if (whiteRoundedSprite != null)
        {
            if (whiteRoundedSprite.texture != null) Destroy(whiteRoundedSprite.texture);
            Destroy(whiteRoundedSprite);
        }
    }

    void AplicarEstilos()
    {
        EstilizarNavbarInferior();
        EstilizarPanelConfiguracion();
        EstilizarPanelChat();
    }

    // ─────────────────────────────────────────────
    // 1. NAVBAR INFERIOR (Reposicionamiento AR)
    // ─────────────────────────────────────────────
    void EstilizarNavbarInferior()
    {
        // Buscar la barra inferior por nombre
        GameObject navbar = BuscarPorNombre("HUD_Inferior")
                         ?? BuscarPorNombre("BarraInferior") 
                         ?? BuscarPorNombre("Barra Inferior") 
                         ?? BuscarPorNombre("NavBar") 
                         ?? BuscarPorNombre("Bottom Bar")
                         ?? BuscarPorNombre("HUD Bottom")
                         ?? BuscarPorNombre("HUDBottom");
        if (navbar == null) { Debug.Log("[EstilizadorUI] No se encontró la barra inferior."); return; }

        // Ocultar el fondo de la barra original para que floten los botones
        Image bg = navbar.GetComponent<Image>();
        if (bg != null) bg.color = Color.clear;

        // Buscar botones específicos
        GameObject btnVolver = BuscarPorNombre("BotonVolver") ?? BuscarPorNombre("Volver");
        GameObject btnCamara = BuscarPorNombre("BotonCamara") ?? BuscarPorNombre("Camara");
        GameObject btnChat = BuscarPorNombre("BotonChat") ?? BuscarPorNombre("Chat");

        Transform canvasTransform = navbar.transform.parent;

        // 1. Reposicionar Botón de Ir Atrás (BotonVolver) a la esquina superior izquierda
        if (btnVolver != null)
        {
            btnVolver.transform.SetParent(canvasTransform, true);
            var rt = btnVolver.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(117, 117); // 30% más grande
            rt.anchoredPosition = new Vector2(65, -65);

            // Estilo de tarjeta redondeada oscura translúcida (igual a la foto)
            Image img = btnVolver.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = whiteRoundedSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.12f, 0.15f, 0.18f, 0.6f);
            }

            // Flecha cian limpia (←)
            var txt = btnVolver.GetComponentInChildren<TextMeshProUGUI>(true); // Buscar incluyendo inactivos
            if (txt != null)
            {
                txt.gameObject.SetActive(true);
                txt.text = "←";
                txt.color = COLOR_BOTON_ACTIVO; // Cian
                txt.fontSize = 57f; // 30% más grande
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
            }
            else
            {
                // Si es un sprite, pintarlo de cian
                foreach (Image i in btnVolver.GetComponentsInChildren<Image>(true))
                {
                    if (i.gameObject != btnVolver) i.color = COLOR_BOTON_ACTIVO;
                }
            }
        }

        // 2. Reposicionar y configurar Texto de Título al lado del Botón Volver
        GameObject textGnosis = BuscarPorNombre("Gnosis Fit");
        if (textGnosis != null)
        {
            textGnosis.transform.SetParent(canvasTransform, true);
            var rtText = textGnosis.GetComponent<RectTransform>();
            rtText.anchorMin = new Vector2(0, 1);
            rtText.anchorMax = new Vector2(0, 1);
            rtText.pivot = new Vector2(0, 1);
            rtText.sizeDelta = new Vector2(400, 100);
            rtText.anchoredPosition = new Vector2(215, -55); // Ajustado a la derecha del botón Volver más grande

            var tmpText = textGnosis.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                string exerciseName = "Sentadillas";
                if (GenosisFitDataManager.Instance != null && !string.IsNullOrEmpty(GenosisFitDataManager.Instance.EjercicioSeleccionado))
                {
                    exerciseName = GenosisFitDataManager.Instance.EjercicioSeleccionado;
                }
                else
                {
                    PlaceExample placeEx = FindFirstObjectByType<PlaceExample>();
                    if (placeEx != null)
                    {
                        var catalog = ExerciseData.ObtenerCatalogo();
                        int idx = placeEx.indiceEjercicioActual;
                        if (idx >= 0 && idx < catalog.Length)
                        {
                            exerciseName = catalog[idx].nombre;
                        }
                    }
                }
                tmpText.text = exerciseName;
                tmpText.fontSize = 38f; // Proporcional
                tmpText.color = Color.white;
                tmpText.fontStyle = FontStyles.Bold;
                tmpText.alignment = TextAlignmentOptions.Left;
            }
        }

        // 3. Reposicionar Botón del Chatbot en la esquina inferior derecha (Estilo del Menú Principal)
        if (btnChat != null)
        {
            btnChat.transform.SetParent(canvasTransform, true);
            var rt = btnChat.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.pivot = new Vector2(1, 0);
            rt.sizeDelta = new Vector2(312, 98); // 30% más grande
            rt.anchoredPosition = new Vector2(-50, 65);

            // Estilo cápsula Teal redondeada
            Image img = btnChat.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = whiteRoundedSprite;
                img.type = Image.Type.Sliced;
                img.color = COLOR_BOTON_ACTIVO; // Teal
            }

            // Sombra para hacerlo flotante
            var shadow = btnChat.GetComponent<Shadow>();
            if (shadow == null) shadow = btnChat.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -5f);

            // Texto "Chatbot AI"
            var txt = btnChat.GetComponentInChildren<TextMeshProUGUI>(true); // Buscar incluyendo inactivos
            if (txt != null)
            {
                txt.gameObject.SetActive(true);
                txt.text = "Chatbot AI";
                txt.color = Color.white;
                txt.fontSize = 34f; // 30% más grande
                txt.fontStyle = FontStyles.Bold;
                txt.alignment = TextAlignmentOptions.Center;
                Stretch(txt.gameObject);
            }

            // Configurar onClick para ir a la escena del chatbot global
            Button btnComponent = btnChat.GetComponent<Button>();
            if (btnComponent != null)
            {
                btnComponent.onClick = new Button.ButtonClickedEvent(); // LIMPIAR LISTENER PERSISTENTE DEL EDITOR
                btnComponent.onClick.AddListener(() => {
                    if (GenosisFitDataManager.Instance != null)
                    {
                        GenosisFitDataManager.Instance.VieneDeAR = true; // Indicar que venimos de AR
                    }
                    UnityEngine.SceneManagement.SceneManager.LoadScene("ChatbotMode");
                });
            }
        }

        // 4. Reposicionar Botón Cámara como "Corregir Postura" en la esquina inferior izquierda (simétrico al Chatbot AI)
        if (btnCamara != null)
        {
            btnCamara.SetActive(true);
            btnCamara.transform.SetParent(canvasTransform, true);
            var rt = btnCamara.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.sizeDelta = new Vector2(370, 98); // Mismo alto que Chatbot AI
            rt.anchoredPosition = new Vector2(50, 65); // Simétrico al Chatbot AI (derecha: -50)

            // Estilo cápsula naranja redondeada (color BTN_POSTURE del menú principal)
            Image img = btnCamara.GetComponent<Image>();
            if (img != null)
            {
                img.sprite = whiteRoundedSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(0.85f, 0.45f, 0.15f, 1f); // BTN_POSTURE del menú
            }

            // Sombra flotante (igual que Chatbot AI)
            var shadow = btnCamara.GetComponent<Shadow>();
            if (shadow == null) shadow = btnCamara.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -5f);

            // Eliminar iconos de cámara hijos que ya no se necesitan
            foreach (Transform child in btnCamara.transform)
            {
                Destroy(child.gameObject);
            }

            // Texto "Corregir Postura"
            var txtGO = new GameObject("TextoPostura", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            txtGO.transform.SetParent(btnCamara.transform, false);
            Stretch(txtGO);

            var tmp = txtGO.GetComponent<TextMeshProUGUI>();
            tmp.text = "Corregir Postura";
            tmp.color = Color.white;
            tmp.fontSize = 32f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;

            // Configurar onClick para ir a SupervisionMode sincronizando datos, como hace GestorModoSupervision
            Button btnComponentCam = btnCamara.GetComponent<Button>();
            if (btnComponentCam != null)
            {
                btnComponentCam.onClick = new Button.ButtonClickedEvent(); // LIMPIAR CUALQUIER LISTENER PERSISTENTE DEL EDITOR
                btnComponentCam.onClick.AddListener(() => {
                    PlaceExample placeEx = FindFirstObjectByType<PlaceExample>();
                    if (placeEx != null && GenosisFitDataManager.Instance != null)
                    {
                        GenosisFitDataManager.Instance.VieneDeAR = true;
                        GenosisFitDataManager.Instance.IndicePersonaje = placeEx.indicePersonajeActual;
                        GenosisFitDataManager.Instance.IndiceEjercicio = placeEx.indiceEjercicioActual;
                        foreach (var ej in ExerciseData.ObtenerCatalogo())
                        {
                            if (ej.idControlador == placeEx.indiceEjercicioActual)
                            {
                                GenosisFitDataManager.Instance.EjercicioSeleccionado = ej.nombre;
                                GenosisFitDataManager.Instance.TipoEjercicio = ej.tipoSupervision;
                                break;
                            }
                        }
                    }
                    UnityEngine.SceneManagement.SceneManager.LoadScene("SupervisionMode");
                });
            }
        }
    }

    // ─────────────────────────────────────────────
    // 2. PANEL DE CONFIGURACIÓN SUPERIOR
    // ─────────────────────────────────────────────
    void EstilizarPanelConfiguracion()
    {
        // Nombre exacto del Inspector: MenuConfiguracion
        GameObject panelConfig = BuscarPorNombre("MenuConfiguracion")
                              ?? BuscarPorNombre("Menu Configuracion")
                              ?? BuscarPorNombre("PanelConfiguracion");
        if (panelConfig == null) { Debug.Log("[EstilizadorUI] No se encontró el panel de configuración."); return; }

        // Fondo oscuro semitransparente
        Image bg = panelConfig.GetComponent<Image>();
        if (bg != null) bg.color = COLOR_PANEL_CONFIG;

        // Estilizar cada botón hijo (Ejercicios, Velocidad, Modelos)
        foreach (Transform hijo in panelConfig.transform)
        {
            string nombre = hijo.name.ToLower();

            // Botón X → rojo
            if (nombre.Contains("x") || nombre.Contains("cerrar") || nombre.Contains("close"))
            {
                Image imgX = hijo.GetComponent<Image>();
                if (imgX != null) imgX.color = COLOR_X_BTN;
                TextMeshProUGUI tmpX = hijo.GetComponentInChildren<TextMeshProUGUI>();
                if (tmpX != null) { tmpX.color = Color.white; tmpX.fontStyle = FontStyles.Bold; }
                continue;
            }

            // Botones normales → card oscura con texto claro
            Image img = hijo.GetComponent<Image>();
            if (img != null) img.color = COLOR_BOTON_CONFIG;

            TextMeshProUGUI tmp = hijo.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null)
            {
                tmp.color = COLOR_BOTON_CONFIG_TX;
                tmp.fontStyle = FontStyles.Bold;
                tmp.fontSize = Mathf.Max(tmp.fontSize, 18f);
            }

            // Bordes redondeados
            if (img != null && img.sprite == null)
            {
                img.sprite = whiteRoundedSprite;
                img.type = Image.Type.Sliced;
            }
        }
    }

    // ─────────────────────────────────────────────
    // 3. PANEL DE CHAT
    // ─────────────────────────────────────────────
    void EstilizarPanelChat()
    {
        // Nombre exacto del Inspector: MenuChat
        GameObject panelChat = BuscarPorNombre("MenuChat")
                            ?? BuscarPorNombre("Panel Chat")
                            ?? BuscarPorNombre("PanelChat");
        if (panelChat == null) { Debug.Log("[EstilizadorUI] No se encontró el panel de chat."); return; }

        // Fondo principal del chat
        Image bgChat = panelChat.GetComponent<Image>();
        if (bgChat != null) bgChat.color = COLOR_CHAT_BG;

        // Buscar elementos internos por nombre
        EstilizarHijo(panelChat, "Header",    COLOR_CHAT_HEADER, Color.white, true);
        EstilizarHijo(panelChat, "Enviar",    COLOR_ENVIAR,      Color.white, true);
        EstilizarHijo(panelChat, "InputField",COLOR_INPUT_BG,    new Color(0.9f, 0.9f, 0.9f), false, true);
        EstilizarHijo(panelChat, "Input",     COLOR_INPUT_BG,    new Color(0.9f, 0.9f, 0.9f), false, true);

        // Texto del chat → color claro para que contraste con el fondo oscuro
        TextMeshProUGUI textoChat = BuscarTMPEnHijos(panelChat, "texto");
        if (textoChat != null) textoChat.color = COLOR_TEXT_WHITE;

        // Botón X del chat → rojo
        EstilizarHijo(panelChat, "X",       COLOR_X_BTN, Color.white, true);
        EstilizarHijo(panelChat, "Cerrar",  COLOR_X_BTN, Color.white, true);

        // Desactivar el panel de chat local por completo para que no sea visible ni interfiera
        panelChat.SetActive(false);
    }

    // ─────────────────────────────────────────────
    // UTILIDADES
    // ─────────────────────────────────────────────
    void EstilizarHijo(GameObject padre, string nombreParcial, Color bgColor, Color txColor, bool negrita, bool addPadding = false)
    {
        Transform hijo = BuscarHijoPorNombreParcial(padre.transform, nombreParcial);
        if (hijo == null) return;
        Image img = hijo.GetComponent<Image>();
        if (img != null) img.color = bgColor;
        
        // Aplicar a TODOS los TMP hijos (para InputFields asegura pintar tanto el Placeholder como el Text)
        foreach (TextMeshProUGUI tmp in hijo.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            tmp.color = txColor;
            if (negrita) tmp.fontStyle = FontStyles.Bold;
            if (addPadding) tmp.margin = new Vector4(15, 0, 15, 0);
        }
    }

    Transform BuscarHijoPorNombreParcial(Transform padre, string nombreParcial)
    {
        string buscar = nombreParcial.ToLower();
        foreach (Transform t in padre.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLower().Contains(buscar)) return t;
        }
        return null;
    }

    TextMeshProUGUI BuscarTMPEnHijos(GameObject padre, string nombreParcial)
    {
        string buscar = nombreParcial.ToLower();
        foreach (TextMeshProUGUI tmp in padre.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (tmp.gameObject.name.ToLower().Contains(buscar)) return tmp;
        }
        return null;
    }

    GameObject BuscarPorNombre(string nombre)
    {
        string buscar = nombre.ToLower();

        // 1. Buscar en toda la escena incluyendo inactivos
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Canvas canvas in canvases)
        {
            Transform encontrado = BuscarEnHijos(canvas.transform, buscar);
            if (encontrado != null) return encontrado.gameObject;
        }

        // 2. Fallback: buscar en objetos raíz de la escena
        foreach (GameObject raiz in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            Transform encontrado = BuscarEnHijos(raiz.transform, buscar);
            if (encontrado != null) return encontrado.gameObject;
        }

        return null;
    }

    Transform BuscarEnHijos(Transform padre, string nombreParcialLower)
    {
        if (padre.name.ToLower().Contains(nombreParcialLower)) return padre;
        foreach (Transform hijo in padre)
        {
            Transform resultado = BuscarEnHijos(hijo, nombreParcialLower);
            if (resultado != null) return resultado;
        }
        return null;
    }

    void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }
    }

    // Generador de texturas procedimentales redondeadas suavizadas para AR
    private Sprite CreateRoundedSprite(int width, int height, int radius, Color fillColor)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        Color[] cols = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = x;
                float cy = y;
                if (x < radius) cx = radius;
                else if (x >= width - radius) cx = width - radius - 1;
                if (y < radius) cy = radius;
                else if (y >= height - radius) cy = height - radius - 1;

                Color finalColor = Color.clear;
                if (cx != x || cy != y)
                {
                    float dx = x - cx;
                    float dy = y - cy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float outerEdge = radius;

                    if (dist > outerEdge + 0.5f) finalColor = Color.clear;
                    else if (dist > outerEdge - 0.5f) finalColor = Color.Lerp(Color.clear, fillColor, (outerEdge + 0.5f - dist));
                    else finalColor = fillColor;
                }
                else
                {
                    finalColor = fillColor;
                }
                cols[y * width + x] = finalColor;
            }
        }
        tex.SetPixels(cols);
        tex.Apply();
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.Tight, border);
    }
}
