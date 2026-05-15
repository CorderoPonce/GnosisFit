using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UIHelper
{
    // Colores del Mockup
    public static readonly Color BLANCO = Color.white;
    public static readonly Color FONDO_GRIS_CLARO = new Color(0.95f, 0.95f, 0.95f);
    public static readonly Color GRIS_OSCURO = new Color(0.15f, 0.15f, 0.15f);
    public static readonly Color GRIS_MEDIO = new Color(0.3f, 0.3f, 0.3f);
    public static readonly Color AZUL_BOTON = new Color(0.2f, 0.6f, 1f); // Azul claro/vivo
    public static readonly Color GRIS_TARJETA = new Color(0.85f, 0.85f, 0.85f);
    public static readonly Color NEGRO = Color.black;

    private static Sprite _roundedSprite;
    public static Sprite RoundedSprite
    {
        get
        {
            if (_roundedSprite == null)
                _roundedSprite = CrearSpriteRedondeado(32);
            return _roundedSprite;
        }
    }

    public static GameObject CrearPanel(string nombre, Transform padre, Color color, bool usarBordesRedondeados = false)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(padre, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        if (usarBordesRedondeados)
        {
            img.sprite = RoundedSprite;
            img.type = Image.Type.Sliced;
        }
        return go;
    }

    public static TextMeshProUGUI CrearTexto(string nombre, Transform padre, string texto, float size, Color color, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = texto;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    public static Button CrearBoton(string nombre, Transform padre, string texto, Color bgColor, Color textColor, float textSize, bool redondeado = true)
    {
        var btnGo = CrearPanel(nombre, padre, bgColor, redondeado);
        var btn = btnGo.AddComponent<Button>();

        var txt = CrearTexto("Text", btnGo.transform, texto, textSize, textColor, FontStyles.Bold);
        SetAnchorsStretch(txt.GetComponent<RectTransform>());

        return btn;
    }

    public static void SetAnchorsStretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    private static Sprite CrearSpriteRedondeado(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[] colors = new Color[size * size];
        float radius = size / 2f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                colors[y * size + x] = dist <= radius ? Color.white : Color.clear;
            }
        }

        tex.SetPixels(colors);
        tex.Apply();
        
        // Configurar los bordes (Border) para que sea un Slice sprite
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect, new Vector4(size/2 - 1, size/2 - 1, size/2 - 1, size/2 - 1));
    }
}
