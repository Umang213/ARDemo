using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MeasurementPlacement : MonoBehaviour
{
    [SerializeField]
    Text m_Object;

    public GameObject midTextObject;

    private string measurement = "";

    private void Start()
    {
        midTextObject.SetActive(true);
    }

    void Update()
    {
        //midTextObject.transform.LookAt(Camera.main.transform);
        m_Object.text = measurement;
    }

    /// <summary>
    /// Set mesurement value
    /// </summary>
    public void ChangeMeasurement(string mesurement)
    {
        this.measurement = mesurement;
    }
}
