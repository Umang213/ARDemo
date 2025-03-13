using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

public class LineManager : MonoBehaviour
{
    public static LineManager lineManager;
    public ObjectSpawner m_ObjectSpawner;
    public LineRenderer lineRenderer;
    public GameObject distanceText;
    TextMeshPro mText;
    public List<GameObject> gameObjects = new List<GameObject>();
    private List<TextMeshPro> distanceTexts = new List<TextMeshPro>();

    private void Awake()
    {
        if (lineManager == null) lineManager = this;
    }
    private void Start()
    {
        mText = distanceText.transform.GetChild(0).GetComponent<TextMeshPro>();
        m_ObjectSpawner.objectSpawned += OnObjectSpawned;
    }

    void OnDestroy()
    {
        m_ObjectSpawner.objectSpawned -= OnObjectSpawned;
    }

    void OnObjectSpawned(GameObject spawnedObject)
    {
        gameObjects.Add(spawnedObject);
        DrawLinesAndDistances();
    }

    private void Update()
    {
        // DrawLinesAndDistances();
    }

    private void DrawLinesAndDistances()
    {
        if (gameObjects.Count < 2) return;

        lineRenderer.positionCount = gameObjects.Count;

        // Clear previous distance texts
        foreach (var text in distanceTexts)
        {
            Destroy(text.gameObject);
        }
        distanceTexts.Clear();

        for (int i = 0; i < gameObjects.Count; i++)
        {
            Vector3 centerPoint = gameObjects[i].transform.GetChild(0).GetComponent<Renderer>().bounds.center;
            lineRenderer.SetPosition(i, centerPoint + Vector3.up * 0.1f); // Raise line slightly above surface

            if (i > 0)
            {
                Vector3 pointA = gameObjects[i - 1].transform.GetChild(0).GetComponent<Renderer>().bounds.center;
                Vector3 pointB = gameObjects[i].transform.GetChild(0).GetComponent<Renderer>().bounds.center;
                float dist = Vector3.Distance(pointA, pointB);

                TextMeshPro distText = Instantiate(mText);
                distText.text = dist.ToString("F2");

                Vector3 directionVector = (pointB - pointA).normalized;
                Vector3 normal = Vector3.up;
                Vector3 upd = Vector3.Cross(directionVector, normal).normalized;
                quaternion rotation = Quaternion.LookRotation(-normal, upd);

                distText.transform.rotation = rotation;
                // distText.transform.position = (pointA + directionVector * 0.5f) + upd * 0.2f + Vector3.up * 0.2f; // Raise text above surface
                distText.transform.position = (pointA + pointB) / 2;
                distanceTexts.Add(distText);
            }
        }
    }

    /*  void DrawLine(Transform transform)
     {
         lineRenderer.positionCount++;
         lineRenderer.SetPosition(lineRenderer.positionCount - 1, transform.position);
         if (lineRenderer.positionCount > 1)
         {
             Vector3 pointA = lineRenderer.GetPosition(lineRenderer.positionCount - 1);
             Vector3 pointB = lineRenderer.GetPosition(lineRenderer.positionCount - 2);
             float dist = Vector3.Distance(pointA, pointB);

             TextMeshPro distText = Instantiate(mText);
             distText.text = "" + dist;

             Vector3 directionVector = (pointB - pointA);
             Vector3 normal = transform.up;

             Vector3 upd = Vector3.Cross(directionVector, normal).normalized;
             quaternion rotation = Quaternion.LookRotation(-normal, upd);

             distText.transform.rotation = rotation;
             distText.transform.position = (pointA + directionVector * 0.5f) + upd * 0.2f;
         }
     } */
}
