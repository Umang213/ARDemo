using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
    }

    private void Update()
    {
        if (mainCamera != null)
        {
            transform.LookAt(mainCamera.transform);
            // transform.rotation = Quaternion.Euler(0f, transform.rotation.eulerAngles.y, 0f); // Keeps it upright
        }
    }
}
