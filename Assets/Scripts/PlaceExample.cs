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
            if (EventSystem.current.IsPointerOverGameObject(toque.fingerId))
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

    // NUEVO: Esta función será llamada por tu Dropdown de la UI
    public void CambiarModelo(int nuevoIndice)
    {
        indiceSeleccionado = nuevoIndice;

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
}