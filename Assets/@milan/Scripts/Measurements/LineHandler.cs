using UnityEngine;

/// <summary>
/// Line handler
/// </summary>
public class LineHandler : MonoBehaviour
{
    private LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
    }

    /// <summary>
    /// Add line connecting points
    /// </summary>
    public void AddLine(Vector3 previousPose, Vector3 newPose)
    {
        lineRenderer.SetPosition(0, previousPose);
        lineRenderer.SetPosition(1, newPose);
    }
}
