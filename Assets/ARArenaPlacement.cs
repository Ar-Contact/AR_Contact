using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.EventSystems;

public class ARArenaPlacement : MonoBehaviour
{
    public GameObject arenaPrefab;
    private ARRaycastManager raycastManager;
    private GameObject spawnedObject;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();

    void Start() { raycastManager = GetComponent<ARRaycastManager>(); }

    void Update()
    {
        if (Input.touchCount == 0) return;
        Touch touch = Input.GetTouch(0);

        // UI'a dokunuyorsak AR iþlemini iptal et
        if (EventSystem.current.IsPointerOverGameObject(touch.fingerId)) return;

        if (touch.phase == TouchPhase.Began)
        {
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                if (spawnedObject == null)
                {
                    spawnedObject = Instantiate(arenaPrefab, hits[0].pose.position, hits[0].pose.rotation);

                    // ArenaManager'a haber ver
                    if (ArenaManager.Instance != null)
                        ArenaManager.Instance.StartGameAfterPlacement();
                }
            }
        }
    }
}