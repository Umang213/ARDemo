using UnityEngine;

/// <summary>
/// Rotate the ditance showing cards
/// </summary>
public class CardRotation : MonoBehaviour
{
    private GameObject arCamera;
    
    void Start()
    {
        arCamera = GameObject.FindWithTag("MainCamera");
    }

    void Update()
    {
        transform.LookAt(arCamera.transform);
    }
}
