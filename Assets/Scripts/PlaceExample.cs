using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

// Esto asegura que el script no funcione si no tienes un Raycast Manager
[RequireComponent(typeof(ARRaycastManager))]
public class PlaceExample : MonoBehaviour
{
    [Header("El modelo 3D que vas a hacer aparecer")]
    public GameObject modeloPrefab;

    private GameObject modeloInstanciado;
    private ARRaycastManager raycastManager;
    private static List<ARRaycastHit> impactos = new List<ARRaycastHit>();

    void Awake()
    {
        // Conectamos el script con el componente AR Raycast Manager
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        // Chivato 1: ¿Unity reconoce que hay dedos en la pantalla?
        if (Input.touchCount > 0)
        {
            Debug.Log("ASISTENTE DEPORTIVO: ¡Pantalla tocada! Dedos detectados: " + Input.touchCount);
            Touch toque = Input.GetTouch(0);

            if (toque.phase == TouchPhase.Began)
            {
                Debug.Log("ASISTENTE DEPORTIVO: Fase 'Began' detectada en la posición: " + toque.position);

                if (raycastManager.Raycast(toque.position, impactos, TrackableType.PlaneWithinPolygon))
                {
                    Debug.Log("ASISTENTE DEPORTIVO: ¡Raycast chocó con un plano AR!");
                    Pose poseChoque = impactos[0].pose;

                    if (modeloInstanciado == null)
                    {
                        Debug.Log("ASISTENTE DEPORTIVO: Instanciando cápsula por primera vez.");
                        modeloInstanciado = Instantiate(modeloPrefab, poseChoque.position, poseChoque.rotation);
                    }
                    else
                    {
                        Debug.Log("ASISTENTE DEPORTIVO: Moviendo cápsula existente.");
                        modeloInstanciado.transform.position = poseChoque.position;
                    }
                }
                else
                {
                    Debug.Log("ASISTENTE DEPORTIVO: El raycast se disparó, pero no tocó la malla amarilla.");
                }
            }
        }
    }
}