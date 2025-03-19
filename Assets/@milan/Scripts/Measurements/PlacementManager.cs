using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using iPAHeartBeat.Core.Dependency;

public class PlacementManager : MonoBehaviour
{
    public ChartSize chartSize;

    [Header("AR Components")]
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private GameObject indicator;
    [SerializeField] private TrackableType trackableType = TrackableType.PlaneWithinPolygon;
    [SerializeField] private GameObject warningText;

    [Header("Measurement Components")]
    private GameObject scanSurface;
    private LineRenderer lineRenderer;
    private bool isPlacementPoseValid = false;
    private float distance = 0;
    private float finalScale = 0;
    private Vector3 pointSize;
    private Vector3 midPoint = Vector3.zero;
    private Vector3 previousPose = Vector3.zero;
    private string distanceTextValue = "";
    private int numberOfPoints = 0;
    private List<Transform> pointList = new List<Transform>();
    private List<GameObject> midPoints = new List<GameObject>();
    [SerializeField] private Pose placementPose;
    [SerializeField] private Text distanceText;
    [SerializeField] private GameObject placementIndicator;
    [SerializeField] private GameObject measurementText;
    [SerializeField] private GameObject objectToPlace;
    [SerializeField] private GameObject midPointObject;
    [SerializeField] private GameObject arCamera;
    public float minCameraDistanceToIndicator = 2;
    MeshCreatorAR meshCreatorAR;

    void Awake()
    {
        DependencyResolver.Register<PlacementManager>(this);
    }


    private void Start()
    {
        meshCreatorAR ??= DependencyResolver.Resolve<MeshCreatorAR>();
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        scanSurface = GameObject.FindWithTag("ScanSurfaceAnim");
        scanSurface.SetActive(true);
    }

    void Update()
    {
        UpdatePlacementPose();
        UpdatePlacementIndicator();
        UpdateDistanceFromCamera();
        UpdateDistanceText();
    }

    void UpdateDistanceText()
    {
        if (numberOfPoints > 0)
        {
            UpdateMeasurement();
        }
    }

    void UpdateMeasurement()
    {
        distance = Vector3.Distance(previousPose, placementPose.position);
        midPoint = midPoints[numberOfPoints - 1].transform.position;
        midPoint.x = previousPose.x + (indicator.transform.position.x - previousPose.x) / 2;
        midPoint.y = previousPose.y + (indicator.transform.position.y - previousPose.y) / 2 + 0.001f;
        midPoint.z = previousPose.z + (indicator.transform.position.z - previousPose.z) / 2;
        midPoints[numberOfPoints - 1].transform.position = midPoint;

        distanceTextValue = ConvertDistance(distance);
        distanceText.text = distanceTextValue;
        midPoints[numberOfPoints - 1].GetComponent<MeasurementPlacement>().ChangeMeasurement(distanceTextValue);
        lineRenderer.SetPosition(1, indicator.transform.position);
    }

    string ConvertDistance(float distanceMeters)
    {
        switch (chartSize)
        {
            case ChartSize.m:
                return distanceMeters.ToString("F2") + " m";
            case ChartSize.cm:
                return (distanceMeters * 100).ToString("F1") + " cm";
            case ChartSize.mm:
                return (distanceMeters * 1000).ToString("F0") + " mm";
            default:
                return distanceMeters.ToString("F2") + " m";
        }
    }

    public void PlaceNewPoint()
    {
        if (isPlacementPoseValid && !IsSelfIntersecting(previousPose, placementPose.position))
        {
            GameObject newPoint = Instantiate(objectToPlace, indicator.transform.position, placementPose.rotation);
            GameObject midPoint = Instantiate(midPointObject, indicator.transform.position, placementPose.rotation);

            if (meshCreatorAR.roomCreation == RoomCreation.FloorCreate)
            {
                meshCreatorAR.floorPoints.Add(newPoint.transform.position);
            }

            if (numberOfPoints > 2 && Vector3.Distance(pointList[0].position, newPoint.transform.position) < 0.1f)
            {
                Debug.Log("Room Closed Successfully");
                meshCreatorAR.roomCreation = RoomCreation.RoomHeight;
                meshCreatorAR.CreateMesh();
                return;
            }

            newPoint.transform.localScale = pointSize;
            midPoint.transform.localScale = indicator.transform.localScale * 6;

            midPoints.Add(midPoint);
            pointList.Add(newPoint.transform);
            numberOfPoints += 1;


            if (numberOfPoints == 1)
            {
                previousPose = newPoint.transform.position;
            }
            else
            {
                midPoints[numberOfPoints - 1].GetComponent<LineHandler>().AddLine(previousPose, newPoint.transform.position);
                previousPose = newPoint.transform.position;
            }

            lineRenderer.SetPosition(0, previousPose);

            if (numberOfPoints > 2 && Vector3.Distance(pointList[0].position, newPoint.transform.position) < 0.1f)
            {
                Debug.Log("Closed Area Formed");
                meshCreatorAR.roomCreation = RoomCreation.RoomHeight;
            }
        }
    }
    private bool IsSelfIntersecting(Vector3 start, Vector3 end)
    {
        int lastIndex = pointList.Count - 1;
        for (int i = 0; i < lastIndex - 1; i++) // Ignore last line as it's not finalized yet
        {
            Vector3 lineStart = pointList[i].position;
            Vector3 lineEnd = pointList[i + 1].position;

            if (DoLinesIntersect(lineStart, lineEnd, start, end))
            {
                //                       Debug.LogError("Self-intersecting line detected!");
                return true;
            }
        }
        return false;
    }
    private bool DoLinesIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        float d = (a2.x - a1.x) * (b2.z - b1.z) - (a2.z - a1.z) * (b2.x - b1.x);
        if (Mathf.Approximately(d, 0)) return false;

        float u = ((b1.x - a1.x) * (b2.z - b1.z) - (b1.z - a1.z) * (b2.x - b1.x)) / d;
        float v = ((b1.x - a1.x) * (a2.z - a1.z) - (b1.z - a1.z) * (a2.x - a1.x)) / d;

        return (u >= 0 && u <= 1 && v >= 0 && v <= 1);
    }
    private void UpdatePlacementIndicator()
    {
        if (isPlacementPoseValid)
        {
            bool isValidPlacement = pointList.Count < 2 || !IsSelfIntersecting(previousPose, placementPose.position);
            placementIndicator.SetActive(isValidPlacement);
            placementIndicator.GetComponentInChildren<MeshRenderer>().material.color = isValidPlacement ? Color.white : Color.red;
            placementIndicator.transform.position = placementPose.position;
            placementIndicator.transform.rotation = placementPose.rotation;
        }
        else
        {
            placementIndicator.SetActive(false);
        }
    }

    public void Clear()
    {
        measurementText.SetActive(false);
        SceneManager.LoadScene("ARMeasurement");
    }

    public void Back()
    {
        measurementText.SetActive(false);
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        LoaderUtility.Deinitialize();
    }

    private void UpdatePlacementPose()
    {
        if (meshCreatorAR.roomCreation != RoomCreation.FloorCreate)
        {
            isPlacementPoseValid = false;
            scanSurface.SetActive(false);
            return;
        }
        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        arRaycastManager.Raycast(screenCenter, hits, trackableType);

        isPlacementPoseValid = hits.Count > 0;

        if (isPlacementPoseValid)
        {
            placementPose = hits[0].pose;
            scanSurface.SetActive(false);
        }
    }

    void UpdateDistanceFromCamera()
    {
        if (meshCreatorAR.roomCreation != RoomCreation.FloorCreate)
        {
            return;
        }
        float cameraDistance = Vector3.Distance(arCamera.transform.position, placementPose.position);
        finalScale = cameraDistance * 2f;
        placementIndicator.transform.localScale = new Vector3(finalScale, finalScale, finalScale);
        placementIndicator.GetComponent<SphereCollider>().radius = finalScale / 50;

        indicator.transform.localScale = new Vector3(finalScale / 200, finalScale / 200, finalScale / 200);

        if (cameraDistance > minCameraDistanceToIndicator)
        {
            StartCoroutine(ShowWarningText());
            indicator.SetActive(false);
            placementIndicator.SetActive(false);
        }
        else
        {
            indicator.SetActive(true);
            warningText.SetActive(false);
            placementIndicator.SetActive(true);
        }
    }

    IEnumerator ShowWarningText()
    {
        float currentMovementTime = 0f;

        while (currentMovementTime < 1f)
        {
            currentMovementTime += Time.deltaTime;
            warningText.SetActive(true);
            yield return null;
        }
    }

}



