using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class EnlacesChat : MonoBehaviour, IPointerClickHandler
{
    private TextMeshProUGUI textoChat;
    public PlaceExample scriptPlaceExample; 

    void Awake()
    {
        textoChat = GetComponent<TextMeshProUGUI>();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // Si el Canvas es 'Overlay', la cámara debe ser null para que el raycast de texto funcione
        Canvas canvas = textoChat.canvas;
        Camera cam = (canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : eventData.pressEventCamera;

        int indiceEnlace = TMP_TextUtilities.FindIntersectingLink(textoChat, eventData.position, cam);

        if (indiceEnlace != -1) 
        {
            TMP_LinkInfo infoEnlace = textoChat.textInfo.linkInfo[indiceEnlace];
            string idEjercicio = infoEnlace.GetLinkID();
            EjecutarAccionEnAR(idEjercicio);
        }
    }

    private void EjecutarAccionEnAR(string idEjercicio)
    {
        string id = idEjercicio.Trim().ToLowerInvariant();
        Debug.Log("Link detectado: " + id);

        // Usamos los índices exactos de tu lista de elementos
        // Sumamos +1 porque tu script PlaceExample hace: indice = nuevoIndice - 1
        switch (id)
        {
            case "air squat": scriptPlaceExample.CambiarEjercicio(0 + 1); break;
            case "air squat bent arms": scriptPlaceExample.CambiarEjercicio(1 + 1); break;
            case "bicep curl": scriptPlaceExample.CambiarEjercicio(2 + 1); break;
            case "bicycle crunch": scriptPlaceExample.CambiarEjercicio(3 + 1); break;
            case "burpee": scriptPlaceExample.CambiarEjercicio(4 + 1); break;
            case "circle crunch": scriptPlaceExample.CambiarEjercicio(5 + 1); break;
            case "cross jumps": scriptPlaceExample.CambiarEjercicio(6 + 1); break;
            case "jumping jacks": scriptPlaceExample.CambiarEjercicio(9 + 1); break;
            case "pike walk": scriptPlaceExample.CambiarEjercicio(11 + 1); break;
            case "push up": scriptPlaceExample.CambiarEjercicio(12 + 1); break;
            case "situps": scriptPlaceExample.CambiarEjercicio(13 + 1); break;
            default: Debug.LogWarning("Ejercicio no mapeado: " + id); break;
        }
    }
}