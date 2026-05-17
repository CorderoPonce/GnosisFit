using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiagnosticTool
{
    [MenuItem("GenosisFit/Transition/Go To Chatbot")]
    public static void GoToChatbot()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Please enter Play Mode first!");
            return;
        }
        
        Debug.Log("Transitioning to ChatbotMode at runtime...");
        SceneManager.LoadScene("ChatbotMode");
    }

    [MenuItem("GenosisFit/Transition/Go To Menu")]
    public static void GoToMenu()
    {
        if (!Application.isPlaying)
        {
            Debug.LogWarning("Please enter Play Mode first!");
            return;
        }
        
        Debug.Log("Transitioning to Scene_Menu at runtime...");
        SceneManager.LoadScene("Scene_Menu");
    }

    [MenuItem("GenosisFit/Inspect Chatbot UI State")]
    public static void InspectChatbotUI()
    {
        Debug.Log("=== CHATBOT UI STATE INSPECTION ===");
        
        var canvas = GameObject.Find("ChatCanvas");
        if (canvas != null)
        {
            Debug.Log($"Found ChatCanvas: {canvas.name}");
            var textObj = GameObject.Find("ChatText");
            if (textObj != null)
            {
                var tmp = textObj.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log($"ChatText Content (Length: {tmp.text.Length}):");
                Debug.Log($"[{tmp.text}]");
            }
            else
            {
                Debug.LogError("ChatText GameObject not found!");
            }
        }
        else
        {
            Debug.LogError("ChatCanvas GameObject not found!");
        }

        var llms = Object.FindObjectsByType<EntrenadorLLM>(FindObjectsSortMode.None);
        Debug.Log($"Found {llms.Length} EntrenadorLLM instances:");
        foreach (var llm in llms)
        {
            Debug.Log($"- GameObject: {llm.gameObject.name}, Scene: {llm.gameObject.scene.name}, UI refs assigned -> textChat: {llm.textoChat != null}, inputUsuario: {llm.inputUsuario != null}, botonEnviar: {llm.botonEnviar != null}");
            if (llm.botonEnviar != null)
            {
                // Inspect onClick listeners if possible, or just print count
                Debug.Log($"  BotonEnviar active in hierarchy: {llm.botonEnviar.gameObject.activeInHierarchy}");
            }
        }
        
        Debug.Log("==================================");
    }
}
