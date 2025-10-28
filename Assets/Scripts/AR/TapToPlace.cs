using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARRaycastManager))]
public class TapToPlace : MonoBehaviour
{
    public GameObject placeablePrefab; // Inspector’dan ayarlayacaðýn prefab
    private ARRaycastManager raycastManager;
    private List<ARRaycastHit> hits = new List<ARRaycastHit>();
    private GameObject spawnedObject;
    private bool objectPlaced = false; // Nesneyi bir kere yerleþtirdikten sonra tekrar koymasýn

    void Awake()
    {
        raycastManager = GetComponent<ARRaycastManager>();
    }

    void Update()
    {
        if (objectPlaced) return;
        if (Input.touchCount == 0) return;

        Touch touch = Input.GetTouch(0);

        if (touch.phase == TouchPhase.Began)
        {
            if (raycastManager.Raycast(touch.position, hits, TrackableType.PlaneWithinPolygon))
            {
                Pose hitPose = hits[0].pose;

                if (placeablePrefab != null)
                {
                    spawnedObject = Instantiate(placeablePrefab, hitPose.position, hitPose.rotation);
                    objectPlaced = true;
                }
            }
        }
    }
}
