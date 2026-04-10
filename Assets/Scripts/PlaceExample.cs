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
        if (Input.touchCount > 0)
        {
            Touch toque = Input.GetTouch(0);

            if (toque.phase == TouchPhase.Began)
            {
                if (raycastManager.Raycast(toque.position, impactos, TrackableType.PlaneWithinPolygon))
                {
                    Pose poseChoque = impactos[0].pose;

                    if (modeloInstanciado == null)
                    {
                        modeloInstanciado = Instantiate(modeloPrefab, poseChoque.position, poseChoque.rotation);
                    }
                    else
                    {
                        modeloInstanciado.transform.position = poseChoque.position;
                    }

                    // --- NUEVAS LÍNEAS PARA QUE TE MIRE ---
                    // 1. Obtenemos la posición de la cámara de tu celular
                    Vector3 posicionCamara = Camera.main.transform.position;
                    
                    // 2. Bloqueamos la altura (Y) para que el modelo no se incline hacia arriba o abajo si tienes el celular alto
                    posicionCamara.y = modeloInstanciado.transform.position.y; 
                    
                    // 3. Le decimos al modelo que mire hacia esa posición
                    modeloInstanciado.transform.LookAt(posicionCamara);
                    // --------------------------------------
                }
            }
        }
    }
}