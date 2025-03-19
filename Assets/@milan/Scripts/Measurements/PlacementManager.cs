using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using System.Linq;
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
        if (isPlacementPoseValid)
        {
            GameObject newPoint = Instantiate(objectToPlace, indicator.transform.position, placementPose.rotation);
            GameObject midPoint = Instantiate(midPointObject, indicator.transform.position, placementPose.rotation);

            if (meshCreatorAR.roomCreation == RoomCreation.FloorCreate)
            {
                meshCreatorAR.floorPoints.Add(newPoint.transform.position);
            }

            if (numberOfPoints > 2 && Vector3.Distance(meshCreatorAR.floorPoints[0], newPoint.transform.position) < 0.1f)
            {
                Debug.Log("Closed Area Formed");
                meshCreatorAR.roomCreation = RoomCreation.RoomHeight;
                meshCreatorAR.CreateMesh();
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

    private void UpdatePlacementIndicator()
    {
        if (isPlacementPoseValid)
        {
            placementIndicator.SetActive(true);
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



