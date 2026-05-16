using UnityEngine;

public class ControladorHUD : MonoBehaviour
{
    [Header("Paneles Principales")]
    public GameObject panelConfiguracion;
    public GameObject panelChat;

    // Función para iniciar la UI nueva
    void Start()
    {
        if (GetComponent<GestorControlesAR>() == null)
        {
            gameObject.AddComponent<GestorControlesAR>();
        }
    }

    // Función para el Botón de Configuración (ahora usado como Volver)
    public void AlternarConfiguracion()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Scene_Menu");
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