using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceExample : MonoBehaviour
{
    [Header("1. Lista de Personajes (Ej: Mark, X-Bot)")]
    public GameObject[] avataresPrefabs; 

    [Header("2. Lista de Ejercicios (Archivos .controller)")]
    public RuntimeAnimatorController[] ejerciciosControllers;

    public GameObject modeloInstanciado;
    private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> impactos = new List<ARRaycastHit>();
    
    // Rastrean el estado actual elegido por el usuario en ambos menús
    // Cambia esto:
    public int indicePersonajeActual = 0; 
    public int indiceEjercicioActual = 0;
    
    // Almacena la velocidad deseada elegida en el menú
    private float velocidadActual = 1.0f;

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch toque = Input.GetTouch(0);

            // Escudo anti-clicks fantasma en la UI
            if (FallaSeguraTocoUI(toque.position))
            {
                return; 
            }

            if (toque.phase == TouchPhase.Began)
            {
                if (raycastManager.Raycast(toque.position, impactos, TrackableType.PlaneWithinPolygon))
                {
                    Pose poseChoque = impactos[0].pose;

                    if (modeloInstanciado == null)
                    {
                        // Instanciamos el muñeco físico
                        modeloInstanciado = Instantiate(avataresPrefabs[indicePersonajeActual], poseChoque.position, poseChoque.rotation);
                        // Inmediatamente le descargamos la animación correcta al cerebro
                        ActualizarCerebroDelModelo();
                    }
                    else
                    {
                        modeloInstanciado.transform.position = poseChoque.position;
                    }

                    Vector3 posicionCamara = Camera.main.transform.position;
                    posicionCamara.y = modeloInstanciado.transform.position.y; 
                    modeloInstanciado.transform.LookAt(posicionCamara);
                }
            }
        }
    }

    // NUEVO: Método Maestro para inyectar la animación y velocidad al muñeco actual
    private void ActualizarCerebroDelModelo()
    {
        if (modeloInstanciado != null)
        {
            Animator animator = modeloInstanciado.GetComponent<Animator>();
            if (animator != null && ejerciciosControllers.Length > 0)
            {
                // Inyectamos el archivo .controller que el usuario haya seleccionado del Dropdown 2
                animator.runtimeAnimatorController = ejerciciosControllers[indiceEjercicioActual];
                animator.speed = velocidadActual;
            }
        }
    }

    // LLAMADO POR DROPDOWN 1: CAMBIAR PERSONAJE
    public void CambiarPersonaje(int nuevoIndice)
    {
        if (nuevoIndice == 0) return; // Ignoramos el Título del Menu
        indicePersonajeActual = nuevoIndice - 1;

        if (modeloInstanciado != null)
        {
            Vector3 posicionActual = modeloInstanciado.transform.position;
            Quaternion rotacionActual = modeloInstanciado.transform.rotation;
            
            // Destruimos la "Carne" del muñeco viejo
            Destroy(modeloInstanciado); 
            // Instanciamos la "Carne" del muñeco nuevo
            modeloInstanciado = Instantiate(avataresPrefabs[indicePersonajeActual], posicionActual, rotacionActual);
            
            Vector3 posicionCamara = Camera.main.transform.position;
            posicionCamara.y = modeloInstanciado.transform.position.y;
            modeloInstanciado.transform.LookAt(posicionCamara);
            
            // Hacemos que el muñeco nuevo retome mágicamente el ejercicio que estaba haciendo el viejo
            ActualizarCerebroDelModelo();
        }
    }

    // LLAMADO POR DROPDOWN 2: CAMBIAR EJERCICIO
    public void CambiarEjercicio(int nuevoIndice)
    {
        if (nuevoIndice == 0) return; // Ignoramos el Título del Menu
        indiceEjercicioActual = nuevoIndice - 1;

        // Solo cambiamos el "alma/cerebro", por ende NO destruimos al muñeco
        ActualizarCerebroDelModelo();
    }

    // LLAMADO POR BOTÓN: PAUSA/PLAY
    public void AlternarPausaAnimacion()
    {
        if (modeloInstanciado != null)
        {
            Animator animator = modeloInstanciado.GetComponent<Animator>();
            if (animator != null)
            {
                if (animator.speed > 0f) animator.speed = 0f; 
                else animator.speed = velocidadActual; 
            }
        }
    }

    // LLAMADO POR DROPDOWN 3: CÁMARA LENTA
    public void CambiarVelocidadAnimacion(int indiceOpcion)
    {
        if (indiceOpcion == 0) return;

        if (indiceOpcion == 1) velocidadActual = 1.0f;
        else if (indiceOpcion == 2) velocidadActual = 0.75f;
        else if (indiceOpcion == 3) velocidadActual = 0.5f;

        if (modeloInstanciado != null)
        {
            Animator animator = modeloInstanciado.GetComponent<Animator>();
            if (animator != null)
            {
                if (indiceOpcion == 4 && animator.speed > 0f) animator.speed = 0f; 
                else animator.speed = velocidadActual; 
            }
        }
    }

    // ESCUDO ANDROID
    private bool FallaSeguraTocoUI(Vector2 posicionTacto)
    {
        if (EventSystem.current == null) return false;
        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = posicionTacto;
        List<RaycastResult> resultados = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, resultados);
        return resultados.Count > 0;
    }
}