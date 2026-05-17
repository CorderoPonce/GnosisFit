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
    static readonly Color COLOR_BOTON_ACTIVO    = new Color(0.00f, 0.75f, 0.65f, 1f);    // Teal
    static readonly Color COLOR_BOTON_INACTIVO  = new Color(0.55f, 0.55f, 0.60f, 1f);    // Gris
    static readonly Color COLOR_PANEL_CONFIG    = new Color(0.10f, 0.10f, 0.14f, 0.95f); // Oscuro
    static readonly Color COLOR_BOTON_CONFIG    = new Color(0.18f, 0.18f, 0.24f, 1f);    // Card oscura
    static readonly Color COLOR_BOTON_CONFIG_TX = new Color(0.95f, 0.95f, 0.95f, 1f);    // Texto claro
    static readonly Color COLOR_X_BTN           = new Color(0.9f,  0.25f, 0.25f, 1f);    // Rojo suave
    static readonly Color COLOR_CHAT_HEADER     = new Color(0.06f, 0.06f, 0.10f, 1f);
    static readonly Color COLOR_CHAT_BG         = new Color(0.12f, 0.12f, 0.18f, 1f);
    static readonly Color COLOR_ENVIAR          = new Color(0.00f, 0.75f, 0.65f, 1f);
    static readonly Color COLOR_INPUT_BG        = new Color(0.18f, 0.18f, 0.24f, 1f);
    static readonly Color COLOR_TEXT_WHITE      = new Color(0.95f, 0.95f, 0.95f, 1f);

    void Start()
    {
        // Pequeña espera para que todos los componentes estén inicializados
        Invoke(nameof(AplicarEstilos), 0.05f);
    }

    void AplicarEstilos()
    {
        EstilizarNavbarInferior();
        EstilizarPanelConfiguracion();
        EstilizarPanelChat();
    }

    // ─────────────────────────────────────────────
    // 1. NAVBAR INFERIOR
    // ─────────────────────────────────────────────
    void EstilizarNavbarInferior()
    {
        // Buscar la barra inferior por nombre
        GameObject navbar = BuscarPorNombre("BarraInferior") 
                         ?? BuscarPorNombre("Barra Inferior") 
                         ?? BuscarPorNombre("NavBar") 
                         ?? BuscarPorNombre("Bottom Bar")
                         ?? BuscarPorNombre("HUD Bottom")
                         ?? BuscarPorNombre("HUDBottom");
        if (navbar == null) { Debug.Log("[EstilizadorUI] No se encontró la barra inferior (escena secundaria)."); return; }

        // Fondo oscuro con opacidad
        Image bg = navbar.GetComponent<Image>();
        if (bg != null) bg.color = COLOR_NAVBAR_BG;

        // Íconos: aplicar color a todos los Text/TMP hijos directos
        foreach (Transform hijo in navbar.transform)
        {
            // Fondo de cada botón → transparente
            Image imgBoton = hijo.GetComponent<Image>();
            if (imgBoton != null) imgBoton.color = new Color(0, 0, 0, 0);

            // Texto/Icono → color inactivo por defecto
            TextMeshProUGUI tmp = hijo.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.color = COLOR_BOTON_INACTIVO;
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
        if (panelConfig == null) { Debug.Log("[EstilizadorUI] No se encontró el panel de configuración (escena secundaria)."); return; }

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

            // Bordes redondeados (requiere sprite con borde redondeado nativo de Unity)
            if (img != null && img.sprite == null)
            {
                img.sprite = Resources.Load<Sprite>("UI/Rounded") ?? img.sprite;
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
        if (panelChat == null) { Debug.Log("[EstilizadorUI] No se encontró el panel de chat (escena secundaria)."); return; }

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
        // Buscamos en los Canvas de la escena recursivamente
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
}
