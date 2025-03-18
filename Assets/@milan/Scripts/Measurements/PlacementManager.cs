using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.XR.CoreUtils;

public class PlacementManager : MonoBehaviour
{
    public ChartSize chartSize;
    [SerializeField]
    private XROrigin arSessionOrigin;
    [SerializeField]
    private ARRaycastManager arRaycastManager;
    [SerializeField]
    private GameObject indicator;
    [SerializeField]
    TrackableType trackableType = TrackableType.PlaneWithinPolygon;
    [SerializeField]
    private GameObject warningText;

    private GameObject scanSurface;
    private LineRenderer lineRenderer;
    private bool placementPoseIsValid = false;
    private bool lineEnable = false;
    private float distance = 0;
    private float finalScale = 0;
    private Vector3 pointSize;
    private Vector3 mid = new Vector3(0, 0, 0);
    private Vector3 previousPose = new Vector3(0, 0, 0);
    private string distanceAsText = "";
    private int numOfPoints = 0;

    List<Transform> pointList = new List<Transform>();
    List<GameObject> midPoints = new List<GameObject>();
    List<float> distancetoCamera = new List<float>();

    public Pose placementPose;
    public Text distanceText;
    public GameObject placementIndicator;
    public GameObject measurementText;
    public GameObject objectToPlace;
    public GameObject midPointObject;
    public GameObject arCamera;
    public float inchesDisplayLimitation = 36f;
    public float minimumCameraDistanceToIndicator = 2;

    private const float METERSINTOINCHES = 39.3700787f;
    private const int INCHESFORONEFEET = 12;

    private void Start()
    {
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

    /// <summary>
    /// Get distance between two points and update the distance text based on ChartSize.
    /// </summary>
    void UpdateDistanceText()
    {
        if (numOfPoints > 0 && lineEnable)
        {
            UpdateMeasurement();
        }
        else if (numOfPoints > 0 && !lineEnable)
        {
            float distanceUpdated = Vector3.Distance(previousPose, pointList[numOfPoints - 2].position);
            distanceText.text = ConvertDistance(distanceUpdated);
            midPoints[numOfPoints - 1].transform.GetChild(0).gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Calculate and show distance between indicator and last point based on ChartSize.
    /// </summary>
    void UpdateMeasurement()
    {
        distance = Vector3.Distance(previousPose, placementPose.position);
        mid = midPoints[numOfPoints - 1].transform.position;
        mid.x = previousPose.x + (indicator.transform.position.x - previousPose.x) / 2;
        mid.y = previousPose.y + (indicator.transform.position.y - previousPose.y) / 2 + 0.001f;
        mid.z = previousPose.z + (indicator.transform.position.z - previousPose.z) / 2;
        midPoints[numOfPoints - 1].transform.position = mid;

        distanceAsText = ConvertDistance(distance);
        distanceText.text = distanceAsText;
        midPoints[numOfPoints - 1].GetComponent<MeasurementPlacement>().ChangeMeasurement(distanceAsText);
        lineRenderer.SetPosition(1, indicator.transform.position);
    }

    /// <summary>
    /// Convert distance to the selected measurement unit.
    /// </summary>
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
                return distanceMeters.ToString("F2") + " m"; // Default to meters
        }
    }
    /// <summary>
    /// Place new point and middle text object
    /// </summary>
    public void PlaceNewPoint()
    {
        if (placementPoseIsValid)
        {
            lineEnable = !lineEnable;

            GameObject newPoint;
            GameObject midPoint;

            newPoint = Instantiate(objectToPlace, indicator.transform.position, placementPose.rotation);
            midPoint = Instantiate(midPointObject, indicator.transform.position, placementPose.rotation);
            distancetoCamera.Add(Vector3.Distance(arCamera.transform.position, midPoint.transform.position));

            if (numOfPoints > 1)
            {
                newPoint.transform.localScale = pointSize;
                midPoint.transform.localScale = indicator.transform.localScale * 6;
            }
            else
            {
                newPoint.transform.localScale = indicator.transform.localScale;
                midPoint.transform.localScale = indicator.transform.localScale * 6;
                pointSize = newPoint.transform.localScale;
            }

            midPoints.Add(midPoint);
            pointList.Add(newPoint.transform);
            numOfPoints += 1;

            if (numOfPoints > 0)
            {
                measurementText.SetActive(true);
            }

            if (!lineEnable)
            {
                midPoints[numOfPoints - 1].GetComponent<LineHandler>().AddLine(previousPose, indicator.transform.position);
            }

            previousPose = pointList[numOfPoints - 1].position;
            lineRenderer.SetPosition(0, previousPose);
        }
    }

    /// <summary>
    /// Enable and Disable indicator
    /// </summary>
    private void UpdatePlacementIndicator()
    {
        if (placementPoseIsValid)
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

    /// <summary>
    /// Disable mesurement text for new measurement and reload the scene
    /// </summary>
    public void Clear()
    {
        measurementText.SetActive(false);
        SceneManager.LoadScene("ARMeasurement");
    }

    /// <summary>
    /// Load Menu scene
    /// </summary>
    public void Back()
    {
        measurementText.SetActive(false);
        SceneManager.LoadScene("Menu", LoadSceneMode.Single);
        LoaderUtility.Deinitialize();
    }

    /// <summary>
    /// Show placement indicator where raycast hit collide on surafce
    /// </summary>
    private void UpdatePlacementPose()
    {
        var screenCenter = Camera.main.ViewportToScreenPoint(new Vector3(0.5f, 0.5f));
        var hits = new List<ARRaycastHit>();
        arRaycastManager.Raycast(screenCenter, hits, trackableType);

        placementPoseIsValid = hits.Count > 0;

        if (placementPoseIsValid)
        {
            placementPose = hits[0].pose;
            scanSurface.SetActive(false);
        }
    }

    /// <summary>
    /// Check camera and surafce distance, if disatnce more than 2m indicator disable
    /// </summary>
    void UpdateDistanceFromCamera()
    {
        float cameraDistance = Vector3.Distance(arCamera.transform.position, placementPose.position);
        finalScale = cameraDistance * 2f;
        placementIndicator.transform.localScale = new Vector3(finalScale, finalScale, finalScale);
        placementIndicator.GetComponent<SphereCollider>().radius = finalScale / 50;

        indicator.transform.localScale = new Vector3(finalScale / 200, finalScale / 200, finalScale / 200);

        if (cameraDistance > minimumCameraDistanceToIndicator)
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

    /// <summary>
    /// Show warning text panel
    /// </summary>
    IEnumerator ShowWarningText()
    {
        //The amount of time that has passed
        float currentMovementTime = 0f;

        while (currentMovementTime < 1f)
        {
            currentMovementTime += Time.deltaTime;
            warningText.SetActive(true);
            yield return null;
        }
    }

}
public enum ChartSize
{
    m, cm, mm
}
