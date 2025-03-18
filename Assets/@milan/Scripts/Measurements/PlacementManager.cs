using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

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
    public RoomCreation roomCreation;

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
        if (numOfPoints > 0)
        {
            UpdateMeasurement();
        }
        /* else if (numOfPoints > 0 && !lineEnable)
        {
            float distanceUpdated = Vector3.Distance(previousPose, pointList[numOfPoints - 2].position);
            distanceText.text = ConvertDistance(distanceUpdated);
            midPoints[numOfPoints - 1].transform.GetChild(0).gameObject.SetActive(false);
        } */
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
    /// 
    public void PlaceNewPoint()
    {
        if (placementPoseIsValid)
        {

            // lineEnable = !lineEnable;

            GameObject newPoint = Instantiate(objectToPlace, indicator.transform.position, placementPose.rotation);
            GameObject midPoint = Instantiate(midPointObject, indicator.transform.position, placementPose.rotation);

            if (roomCreation == RoomCreation.FloorCreate)
            {
                floorPoints.Add(newPoint.transform.position);
            }

            // If closed area is formed, move to RoomHeight
            if (numOfPoints > 2 && Vector3.Distance(floorPoints[0], newPoint.transform.position) < 0.1f)
            {
                Debug.Log("Closed Area Formed");
                roomCreation = RoomCreation.RoomHeight;
                CreateMesh();
            }

            newPoint = Instantiate(objectToPlace, indicator.transform.position, placementPose.rotation);
            midPoint = Instantiate(midPointObject, indicator.transform.position, placementPose.rotation);
            distancetoCamera.Add(Vector3.Distance(arCamera.transform.position, midPoint.transform.position));

            if (numOfPoints > 1)
            {
                newPoint.transform.localScale = pointSize;
                midPoint.transform.localScale = indicator.transform.localScale * 6;
            }
            /* else
            {
                newPoint.transform.localScale = indicator.transform.localScale;
                midPoint.transform.localScale = indicator.transform.localScale * 6;
                pointSize = newPoint.transform.localScale;
            } */

            midPoints.Add(midPoint);
            pointList.Add(newPoint.transform);
            numOfPoints += 1;

            /*  if (numOfPoints > 0)
             {
                 measurementText.SetActive(true);
             } */

            /* if (!lineEnable)
            {
                midPoints[numOfPoints - 1].GetComponent<LineHandler>().AddLine(previousPose, indicator.transform.position);
            }
 */
            midPoints[numOfPoints - 1].GetComponent<LineHandler>().AddLine(previousPose, indicator.transform.position);
            previousPose = pointList[numOfPoints - 1].position;
            lineRenderer.SetPosition(0, previousPose);
            if (numOfPoints > 2 && Vector3.Distance(pointList[0].position, newPoint.transform.position) < 0.1f)
            {
                // CreateLineBetweenPoints(pointList[numOfPoints - 1].position, pointList[0].position);
                Debug.Log("Closed Area Formed");
                roomCreation = RoomCreation.RoomHeight;
            }
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
        if (roomCreation != RoomCreation.FloorCreate)
        {
            placementPoseIsValid = false;
            scanSurface.SetActive(false);
            return;
        }
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
        if (roomCreation != RoomCreation.FloorCreate)
        {
            return;
        }
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
    /// <summary>
    /// //////////////////////////////////
    /// </summary>
    /// 
    /// 
    /// 
    /// 
    /// 
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material ceilingMaterial;
    [SerializeField] private Material floorMaterial;

    private GameObject roomMeshObject;
    private GameObject floorMeshObject;
    private GameObject ceilingMeshObject;
    private Slider heightSlider;
    private float minHeight = 0.1f;
    private float maxHeight = 3.0f;
    private float currentHeight = 1.5f;
    private List<Vector3> floorPoints = new List<Vector3>();
    public void CreateMesh()
    {
        if (floorPoints.Count < 3) return;

        Mesh wallMesh = new Mesh();
        Mesh floorMesh = new Mesh();
        Mesh ceilingMesh = new Mesh();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();

        List<Vector3> floorVertices = new List<Vector3>();
        List<int> floorTriangles = new List<int>();

        List<Vector3> ceilingVertices = new List<Vector3>();
        List<int> ceilingTriangles = new List<int>();

        // Step 1: Generate Walls
        for (int i = 0; i < floorPoints.Count; i++)
        {
            Vector3 basePoint = floorPoints[i];
            Vector3 topPoint = basePoint + Vector3.up * currentHeight;

            wallVertices.Add(basePoint); // Bottom
            wallVertices.Add(topPoint);  // Top
        }

        for (int i = 0; i < floorPoints.Count - 1; i++)
        {
            int bl = i * 2;
            int tl = i * 2 + 1;
            int br = (i + 1) * 2;
            int tr = (i + 1) * 2 + 1;

            // Front face of walls
            wallTriangles.Add(bl); wallTriangles.Add(br); wallTriangles.Add(tl);
            wallTriangles.Add(tl); wallTriangles.Add(br); wallTriangles.Add(tr);
        }

        // Close last wall
        int lastBl = (floorPoints.Count - 1) * 2;
        int lastTl = (floorPoints.Count - 1) * 2 + 1;
        int firstBl = 0;
        int firstTl = 1;

        wallTriangles.Add(lastBl); wallTriangles.Add(firstBl); wallTriangles.Add(lastTl);
        wallTriangles.Add(lastTl); wallTriangles.Add(firstBl); wallTriangles.Add(firstTl);

        // Step 2: Generate Floor and Ceiling
        foreach (Vector3 point in floorPoints)
        {
            floorVertices.Add(point);                            // Floor
            ceilingVertices.Add(point + Vector3.up * currentHeight); // Ceiling
        }

        for (int i = 1; i < floorPoints.Count - 1; i++)
        {
            // Floor
            floorTriangles.Add(0);
            floorTriangles.Add(i);
            floorTriangles.Add(i + 1);

            // Ceiling
            ceilingTriangles.Add(0);
            ceilingTriangles.Add(i + 1);
            ceilingTriangles.Add(i);
        }

        // Step 3: Assign Mesh Data
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.RecalculateNormals();

        floorMesh.vertices = floorVertices.ToArray();
        floorMesh.triangles = floorTriangles.ToArray();
        floorMesh.RecalculateNormals();

        ceilingMesh.vertices = ceilingVertices.ToArray();
        ceilingMesh.triangles = ceilingTriangles.ToArray();
        ceilingMesh.RecalculateNormals();

        // Step 4: Create GameObjects
        roomMeshObject = new GameObject("Walls");
        MeshFilter wallMeshFilter = roomMeshObject.AddComponent<MeshFilter>();
        wallMeshFilter.mesh = wallMesh;
        MeshRenderer wallRenderer = roomMeshObject.AddComponent<MeshRenderer>();
        wallRenderer.material = wallMaterial;

        floorMeshObject = new GameObject("Floor");
        MeshFilter floorMeshFilter = floorMeshObject.AddComponent<MeshFilter>();
        floorMeshFilter.mesh = floorMesh;
        MeshRenderer floorRenderer = floorMeshObject.AddComponent<MeshRenderer>();
        floorRenderer.material = floorMaterial;

        ceilingMeshObject = new GameObject("Ceiling");
        MeshFilter ceilingMeshFilter = ceilingMeshObject.AddComponent<MeshFilter>();
        ceilingMeshFilter.mesh = ceilingMesh;
        MeshRenderer ceilingRenderer = ceilingMeshObject.AddComponent<MeshRenderer>();
        ceilingRenderer.material = ceilingMaterial;

        CreateHeightSlider();
    }

    void CreateHeightSlider()
    {

        GameObject sliderObj = Instantiate(sliderPrefab.gameObject);
        sliderObj.transform.SetParent(transform, false);
        sliderObj.transform.position = new Vector3(0, 1.5f, 0); // Adjust based on your need
        sliderObj.transform.rotation = Quaternion.LookRotation(Camera.main.transform.forward);

        Canvas canvas = sliderObj.GetComponentInChildren<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
        }

        heightSlider = sliderObj.GetComponentInChildren<Slider>();
        if (heightSlider != null)
        {
            heightSlider.minValue = minHeight;
            heightSlider.maxValue = maxHeight;
            heightSlider.value = minHeight;
            heightSlider.onValueChanged.AddListener(AdjustHeight);
        }
    }

    public GameObject sliderPrefab;

    public void AdjustHeight(float newHeight)
    {
        if (roomMeshObject == null || ceilingMeshObject == null) return;

        Mesh wallMesh = roomMeshObject.GetComponent<MeshFilter>().mesh;
        Vector3[] wallVertices = wallMesh.vertices;

        // Adjust top vertices of walls
        for (int i = 1; i < wallVertices.Length; i += 2)
        {
            wallVertices[i].y = newHeight;
        }

        wallMesh.vertices = wallVertices;
        wallMesh.RecalculateNormals();

        // Adjust Ceiling Mesh
        Mesh ceilingMesh = ceilingMeshObject.GetComponent<MeshFilter>().mesh;
        Vector3[] ceilingVertices = ceilingMesh.vertices;

        for (int i = 0; i < ceilingVertices.Length; i++)
        {
            ceilingVertices[i].y = newHeight;
        }

        ceilingMesh.vertices = ceilingVertices;
        ceilingMesh.RecalculateNormals();
    }
}
public enum ChartSize
{
    m, cm, mm
}
public enum RoomCreation
{
    FloorCreate, RoomHeight, Stuff, None
}
