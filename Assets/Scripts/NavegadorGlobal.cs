
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NavegadorGlobal : MonoBehaviour
{
    public void IrAHome() => SceneManager.LoadScene("Scene_Menu");
    public void IrAChat() => SceneManager.LoadScene("ChatbotMode");
    public void IrASupervision() => SceneManager.LoadScene("SupervisionMode");
    
    // Método para botones de la UI
    public void CargarEscena(string nombreEscena)
    {
        SceneManager.LoadScene(nombreEscena);
    }
}
