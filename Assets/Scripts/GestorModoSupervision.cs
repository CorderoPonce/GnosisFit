using System.Collections;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

public class GestorModoSupervision : MonoBehaviour
{
    [Header("Referencias AR")]
    public ARCameraManager cameraManager;
    public PlaceExample scriptPlaceExample; 

    [Header("UI y PIP")]
    public GameObject panelSupervisionUI; 
    public Transform contenedorPIP; 

    private GameObject clonPIPActual;
    private bool supervisionActivada = false;

    [Header("Visión por Computadora")]
    public GameObject motorMediaPipe; 

    public void AlternarModoSupervision()
    {
        supervisionActivada = !supervisionActivada;

        if (supervisionActivada)
        {
            // 1. Ocultar el modelo original de forma segura
            if (scriptPlaceExample != null && scriptPlaceExample.modeloInstanciado != null)
            {
                scriptPlaceExample.modeloInstanciado.SetActive(false);
            }

            if (scriptPlaceExample != null) scriptPlaceExample.enabled = false;
            
            // 2. Encender la UI
            if (panelSupervisionUI != null) panelSupervisionUI.SetActive(true);

            // 3. Crear el clon (Protegido contra errores)
            GenerarClonPIP();

            // 4. Iniciar el semáforo con la dirección correcta
            StartCoroutine(ReiniciarCamaraAR(CameraFacingDirection.User));
        }
        else
        {
            StartCoroutine(ReiniciarCamaraAR(CameraFacingDirection.World));
            
            if (scriptPlaceExample != null) scriptPlaceExample.enabled = true;
            if (panelSupervisionUI != null) panelSupervisionUI.SetActive(false);

            if (scriptPlaceExample != null && scriptPlaceExample.modeloInstanciado != null)
            {
                scriptPlaceExample.modeloInstanciado.SetActive(true);
            }
            
            if (clonPIPActual != null) Destroy(clonPIPActual);
        }
    }

    private IEnumerator ReiniciarCamaraAR(CameraFacingDirection nuevaDireccion)
    {
        ARSession arSession = FindFirstObjectByType<ARSession>();
        ARPlaneManager planeManager = FindFirstObjectByType<ARPlaneManager>();

        // 1. APAGAMOS TODO (Liberamos hardware)
        if (arSession != null) arSession.enabled = false;
        if (planeManager != null) planeManager.enabled = false;
        if (cameraManager != null) cameraManager.enabled = false;
        if (motorMediaPipe != null) 
        {
            // Obligamos a la IA a detener el procesamiento de imagen antes de apagarla
            motorMediaPipe.SendMessage("Stop", SendMessageOptions.DontRequireReceiver);
            motorMediaPipe.SetActive(false);
        }
        
        // --- LA PAUSA CRUCIAL AJUSTADA A 1.5 SEGUNDOS ---
        yield return new WaitForSeconds(1.5f); 
        
        // 2. ENCENDEMOS LO NECESARIO
        // ENCENDEMOS MEDIAPIPE Y LA INTERFAZ
        if (nuevaDireccion == CameraFacingDirection.User)
        {
            if (motorMediaPipe != null) 
            {
                motorMediaPipe.SetActive(true);
                
                // --- LA MICRO-PAUSA SALVAVIDAS ---
                // Le damos tiempo a MediaPipe de cargar sus redes neuronales antes de exigirle video
                yield return new WaitForSeconds(0.5f); 
                
                motorMediaPipe.SendMessage("Play", SendMessageOptions.DontRequireReceiver);
            }
        }
        else
        {
            if (cameraManager != null) cameraManager.requestedFacingDirection = CameraFacingDirection.World; 
            yield return new WaitForSeconds(0.3f); 
            
            if (cameraManager != null) cameraManager.enabled = true; 
            if (arSession != null) arSession.enabled = true;
            if (planeManager != null) planeManager.enabled = true;
        }
    }

    private void GenerarClonPIP()
    {
        // Seguro: Si no hay referencias, no intentamos clonar nada para no crashear la app
        if (scriptPlaceExample == null || scriptPlaceExample.avataresPrefabs == null || scriptPlaceExample.avataresPrefabs.Length == 0) return;

        if (clonPIPActual != null) Destroy(clonPIPActual);

        int idPersonaje = scriptPlaceExample.indicePersonajeActual;
        int idEjercicio = scriptPlaceExample.indiceEjercicioActual;

        // Seguro de índices
        if (idPersonaje >= scriptPlaceExample.avataresPrefabs.Length) idPersonaje = 0;

        clonPIPActual = Instantiate(scriptPlaceExample.avataresPrefabs[idPersonaje], contenedorPIP.position, contenedorPIP.rotation);
        
        Animator animator = clonPIPActual.GetComponent<Animator>();
        if (animator != null && scriptPlaceExample.ejerciciosControllers != null && idEjercicio < scriptPlaceExample.ejerciciosControllers.Length)
        {
            animator.runtimeAnimatorController = scriptPlaceExample.ejerciciosControllers[idEjercicio];
        }
    }
}