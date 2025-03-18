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

    [Header("AR Components")]
    [SerializeField] private XROrigin arSessionOrigin;
    [SerializeField] private ARRaycastManager arRaycastManager;
    [SerializeField] private GameObject indicator;
    [SerializeField] private TrackableType trackableType = TrackableType.PlaneWithinPolygon;
    [SerializeField] private GameObject warningText;

    [Header("Measurement Components")]
    private GameObject scanSurface;
    private LineRenderer lineRenderer;
    private bool isPlacementPoseValid = false;
    private bool isLineEnabled = false;
    private float distance = 0;
    private float finalScale = 0;
    private Vector3 pointSize;
    private Vector3 midPoint = Vector3.zero;
    private Vector3 previousPose = Vector3.zero;
    private string distanceTextValue = "";
    private int numberOfPoints = 0;

    private List<Transform> pointList = new List<Transform>();
    private List<GameObject> midPoints = new List<GameObject>();
    private List<float> distanceToCamera = new List<float>();

    [SerializeField] private Pose placementPose;
    [SerializeField] private Text distanceText;
    [SerializeField] private GameObject placementIndicator;
    [SerializeField] private GameObject measurementText;
    [SerializeField] private GameObject objectToPlace;
    [SerializeField] private GameObject midPointObject;
    [SerializeField] private GameObject arCamera;
    public float inchesDisplayLimit = 36f;
    public float minCameraDistanceToIndicator = 2;
    private const float METERS_TO_INCHES = 39.3700787f;
    private const int INCHES_PER_FOOT = 12;

    [Header("Room Creation")]
    public RoomCreation roomCreation;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material ceilingMaterial;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Slider slider;
    private GameObject roomMeshObject;
    private GameObject floorMeshObject;
    private GameObject ceilingMeshObject;
    private float minHeight = 0.1f;
    private float maxHeight = 3.0f;
    private float currentHeight = 1.5f;
    private List<Vector3> floorPoints = new List<Vector3>();

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

            if (roomCreation == RoomCreation.FloorCreate)
            {
                floorPoints.Add(newPoint.transform.position);
            }

            if (numberOfPoints > 2 && Vector3.Distance(floorPoints[0], newPoint.transform.position) < 0.1f)
            {
                Debug.Log("Closed Area Formed");
                roomCreation = RoomCreation.RoomHeight;
                CreateMesh();
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
                roomCreation = RoomCreation.RoomHeight;
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
        if (roomCreation != RoomCreation.FloorCreate)
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
        if (roomCreation != RoomCreation.FloorCreate)
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

    #region  Mesh Creation
    void CreateMesh()
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

        for (int i = 0; i < floorPoints.Count; i++)
        {
            Vector3 basePoint = floorPoints[i];
            Vector3 topPoint = basePoint + Vector3.up * currentHeight;

            wallVertices.Add(basePoint);
            wallVertices.Add(topPoint);
        }

        for (int i = 0; i < floorPoints.Count - 1; i++)
        {
            int bl = i * 2;
            int tl = i * 2 + 1;
            int br = (i + 1) * 2;
            int tr = (i + 1) * 2 + 1;

            wallTriangles.Add(bl); wallTriangles.Add(br); wallTriangles.Add(tl);
            wallTriangles.Add(tl); wallTriangles.Add(br); wallTriangles.Add(tr);
        }

        int lastBl = (floorPoints.Count - 1) * 2;
        int lastTl = (floorPoints.Count - 1) * 2 + 1;
        int firstBl = 0;
        int firstTl = 1;

        wallTriangles.Add(lastBl); wallTriangles.Add(firstBl); wallTriangles.Add(lastTl);
        wallTriangles.Add(lastTl); wallTriangles.Add(firstBl); wallTriangles.Add(firstTl);

        foreach (Vector3 point in floorPoints)
        {
            floorVertices.Add(point);
            ceilingVertices.Add(point + Vector3.up * currentHeight);
        }

        for (int i = 1; i < floorPoints.Count - 1; i++)
        {
            // Floor Triangles (Double-Sided)
            floorTriangles.Add(0);
            floorTriangles.Add(i);
            floorTriangles.Add(i + 1);

            floorTriangles.Add(0);
            floorTriangles.Add(i + 1);
            floorTriangles.Add(i); // **Back Face (for double-sided rendering)**

            // Ceiling Triangles (Double-Sided)
            ceilingTriangles.Add(0);
            ceilingTriangles.Add(i + 1);
            ceilingTriangles.Add(i);

            ceilingTriangles.Add(0);
            ceilingTriangles.Add(i);
            ceilingTriangles.Add(i + 1); // **Back Face (for double-sided rendering)**
        }

        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.RecalculateNormals();

        floorMesh.vertices = floorVertices.ToArray();
        floorMesh.triangles = floorTriangles.ToArray();
        floorMesh.RecalculateNormals();

        ceilingMesh.vertices = ceilingVertices.ToArray();
        ceilingMesh.triangles = ceilingTriangles.ToArray();
        ceilingMesh.RecalculateNormals();

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

        AdjustHeight(1.5f);
        slider.onValueChanged.AddListener(AdjustHeight);
    }



    void AdjustHeight(float newHeight)
    {
        if (roomMeshObject == null || ceilingMeshObject == null) return;

        // Ensure height is within limits
        newHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);

        // Adjust Wall Mesh
        Mesh wallMesh = roomMeshObject.GetComponent<MeshFilter>().mesh;
        Vector3[] wallVertices = wallMesh.vertices;
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
    #endregion
}
public enum ChartSize
{
    m, cm, mm
}
public enum RoomCreation
{
    FloorCreate, RoomHeight, Stuff, None
}
