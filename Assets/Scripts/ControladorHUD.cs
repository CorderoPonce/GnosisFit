using UnityEngine;

public class ControladorHUD : MonoBehaviour
{
    [Header("Paneles Principales")]
    public GameObject panelConfiguracion;
    public GameObject panelChat;

    // Función para iniciar la UI nueva
    void Start()
    {
        if (GetComponent<GestorCatalogoVisual>() == null)
        {
            gameObject.AddComponent<GestorCatalogoVisual>();
        }
        if (GetComponent<GestorControlesAR>() == null)
        {
            gameObject.AddComponent<GestorControlesAR>();
        }
    }

    // Función para el Botón de Configuración
    public void AlternarConfiguracion()
    {
        bool estadoActual = panelConfiguracion.activeSelf;
        
        // Primero cerramos todo para que no se traslapen
        panelConfiguracion.SetActive(false);
        panelChat.SetActive(false);

        // Si antes estaba apagado, lo encendemos
        if (!estadoActual)
        {
            panelConfiguracion.SetActive(true);
        }
    }

    // Función para el Botón de Chat
    public void AlternarChat()
    {
        bool estadoActual = panelChat.activeSelf;

        panelConfiguracion.SetActive(false);
        panelChat.SetActive(false);

        if (!estadoActual)
        {
            panelChat.SetActive(true);
        }
    }

    // Función para limpiar la pantalla (Botones X)
    public void CerrarTodo()
    {
        panelConfiguracion.SetActive(false);
        panelChat.SetActive(false);
    }
}