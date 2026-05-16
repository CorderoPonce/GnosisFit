
using UnityEngine;
using UnityEngine.UI;

public class UIAutoFixer : MonoBehaviour
{
    [ContextMenu("Fix UI Layout")]
    public void FixLayout()
    {
        // 1. Canvas Scaler - Asegurar que escala con el tamaño de pantalla
        var scaler = GetComponentInParent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); // Portrait
        }

        // 2. Scroll View - Ocupar casi toda la pantalla
        var scroll = GameObject.Find("ExerciseList_Scroll")?.GetComponent<RectTransform>();
        if (scroll != null)
        {
            scroll.anchorMin = new Vector2(0, 0.1f);
            scroll.anchorMax = new Vector2(1, 0.95f);
            scroll.offsetMin = Vector2.zero;
            scroll.offsetMax = Vector2.zero;
        }

        // 3. Card Template - Tamaño base
        var card = GameObject.Find("Card_Template")?.GetComponent<RectTransform>();
        if (card != null)
        {
            card.sizeDelta = new Vector2(0, 300);
            card.gameObject.SetActive(false); // Ocultar plantilla
        }

        // 4. Content - Asegurar que el Layout Group está bien
        var content = GameObject.Find("ContentContainer")?.GetComponent<RectTransform>();
        if (content != null)
        {
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            
            var vlg = content.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.childControlHeight = true;
                vlg.childForceExpandHeight = false;
                vlg.spacing = 20;
                vlg.padding = new RectOffset(30, 30, 30, 30);
            }
        }
        
        Debug.Log("UI Layout Fixed!");
    }
}
