using System.Collections;
using UnityEngine;

public class IndicatorHandler : MonoBehaviour
{
    private bool inRange = false;

    public GameObject indicator;
    public float minimumDistancePointerMove = 0.05f;

    void Start()
    {
        indicator.transform.position = transform.position;
    }

    void Update()
    {
        if (!inRange)
        {
            indicator.transform.position = transform.position;
        }
    }

    /// <summary>
    /// When indicator on point range indicator pointer move towards point
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Point")
        {
            inRange = true;
            StartCoroutine(MoveToPoint(other));
            Handheld.Vibrate();
        }
    }

    /// <summary>
    /// When indicator away from nearby point, indicator point set back to original point
    /// </summary>
    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Point")
        {
            StartCoroutine(MoveToIndicator(other));
            inRange = false;
        }
    }

    /// <summary>
    /// When other point nearby indicator, indicator point attach to nearby point
    /// </summary>
    IEnumerator MoveToPoint(Collider other)
    {
        //the amount of time you want the movement to take
        float totalMovementTime = 0.5f;
        //The amount of time that has passed
        float currentMovementTime = 0f;

        while (Vector3.Distance(indicator.transform.localPosition, other.transform.position) > 0 && inRange)
        {
            currentMovementTime += Time.deltaTime;
            indicator.transform.position = Vector3.Lerp(transform.position, other.transform.position, currentMovementTime / totalMovementTime);
            yield return null;
        }
    }

    /// <summary>
    /// Moving indicator pointer towards specific point
    /// </summary>
    IEnumerator MoveToIndicator(Collider other)
    {
        float totalMovementTime = 0.5f; //the amount of time you want the movement to take
        float currentMovementTime = 0f;//The amount of time that has passed
        while (Vector3.Distance(indicator.transform.localPosition, transform.position) > minimumDistancePointerMove)
        {
            currentMovementTime += Time.deltaTime;
            indicator.transform.position = Vector3.Lerp(indicator.transform.position, transform.position, currentMovementTime / totalMovementTime);
            yield return null;
        }
    }
}
