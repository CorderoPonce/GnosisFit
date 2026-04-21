using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems; // NUEVO: Necesario para detectar la UI

[RequireComponent(typeof(ARRaycastManager))]
public class PlaceExample : MonoBehaviour
{
    [Header("Tus modelos 3D (Ej: 0=Flexión, 1=Sentadilla)")]
    // Cambiamos a un Array (lista) de GameObjects
    public GameObject[] modelosPrefabs; 

    private GameObject modeloInstanciado;
    private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> impactos = new List<ARRaycastHit>();
    
    // Rastrea qué ejercicio está seleccionado en el menú
    private int indiceSeleccionado = 0; 
    
    // NUEVO: Almacena la velocidad deseada elegida en el menú de velocidad
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

            // NUEVO: ¡El escudo antibugs! Si el dedo está sobre la UI, ignoramos el toque para el mundo 3D
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
                        // Instanciamos el modelo basado en el índice seleccionado
                        modeloInstanciado = Instantiate(modelosPrefabs[indiceSeleccionado], poseChoque.position, poseChoque.rotation);
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

    // NUEVO: Función para cambiar modelos ignorando el "Título"
    public void CambiarModelo(int nuevoIndice)
    {
        // Si el usuario toca el Título del menú (Opción 0), ignoramos la acción
        if (nuevoIndice == 0) return; 

        // Le restamos 1 porque tu Opción 1 del menú ahora carga el Prefab número 0
        indiceSeleccionado = nuevoIndice - 1;


        // Si ya hay un modelo haciendo ejercicio en el piso, lo borramos y ponemos el nuevo en su mismo lugar
        if (modeloInstanciado != null)
        {
            Vector3 posicionActual = modeloInstanciado.transform.position;
            Quaternion rotacionActual = modeloInstanciado.transform.rotation;
            
            Destroy(modeloInstanciado); // Borra el viejo
            
            // Crea el nuevo
            modeloInstanciado = Instantiate(modelosPrefabs[indiceSeleccionado], posicionActual, rotacionActual);
            
            // Hacemos que nos mire inmediatamente
            Vector3 posicionCamara = Camera.main.transform.position;
            posicionCamara.y = modeloInstanciado.transform.position.y;
            modeloInstanciado.transform.LookAt(posicionCamara);
        }
    }

    // NUEVO: Función para Pausar/Reanudar animación interactuando con la interfaz
    public void AlternarPausaAnimacion()
    {
        // Revisamos que haya un mono creado y pisando el tablero primeramente
        if (modeloInstanciado != null)
        {
            // Atrapamos su cerebro (Animator)
            Animator animator = modeloInstanciado.GetComponent<Animator>();
            
            if (animator != null)
            {
                // Si se está moviendo (velocidad > 0), lo congelamos (0f)
                if (animator.speed > 0f)
                {
                    animator.speed = 0f; 
                }
                // Si ya estaba en pausa, lo regresamos a su velocidad asignada actual
                else
                {
                    animator.speed = velocidadActual; 
                }
            }
        }
    }

    // NUEVO: Función para cambiar la velocidad desde un Menú Desplegable (Dropdown)
    public void CambiarVelocidadAnimacion(int indiceOpcion)
    {
        // Si toca el Título "Velocidades" (Opción 0), lo ignoramos
        if (indiceOpcion == 0) return;

        // Configuramos la tabla de velocidades pero rodada un espacio
        // (1 = x1 | 2 = x0.75 | 3 = x0.5)
        if (indiceOpcion == 1) velocidadActual = 1.0f;
        else if (indiceOpcion == 2) velocidadActual = 0.75f;
        else if (indiceOpcion == 3) velocidadActual = 0.5f;

        // Si hay un mono en el piso y NO estaba pausado, le inyectamos la nueva velocidad de inmediato
        if (modeloInstanciado != null)
        {
            Animator animator = modeloInstanciado.GetComponent<Animator>();
            if (animator != null && animator.speed > 0f)
            {
                animator.speed = velocidadActual;
            }
        }
    }

    // NUEVO: Extractor de Colisión UI (Infalible para pantallas táctiles Android/iOS en AR)
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