using UnityEngine;
using TMPro;

public class MeasurementPlacement : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI m_Object; 

    public GameObject midTextObject;

    private string measurement = "";

    private void Start()
    {
        midTextObject.SetActive(true);
    }

    void Update()
    {
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
