using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Collections;

public class RayCastVisible : MonoBehaviour
{
    public float rayLength = 5.0f; // Length of the RayCast
    private LineRenderer lineRenderer;
    public Material lineMaterial;

    private Vector3 startPoint;
    private Vector3 endPoint;

    public int arcSegments = 20; // Number of segments in the arc for smoothness
    public float sineFrequency = 1.0f; // Frequency coefficient for the sine wave
    public float sineAmplitude = 2.0f; // Peak height of the sine wave

    private List<Vector3> pointsAlongLine = new List<Vector3>(); // To store arc points
    public float travelSpeed = 1.0f; // Speed of object movement along the arc
    private bool isMoving = false;

    public InputActionReference moveAlongArcAction; // Reference to an Input Action for movement
    private GameObject objectToMove; // The object that will be assigned based on the RayCast hit

    void Start()
    {
        // Get the LineRenderer component from the GameObject
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer component missing on " + gameObject.name);
            return;
        }

        lineRenderer.material = lineMaterial;

        // Set up LineRenderer properties
        lineRenderer.startColor = Color.green;
        lineRenderer.endColor = Color.green;
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;

        // Enable the Input Action and bind the method to the performed event
        moveAlongArcAction.action.Enable();
        moveAlongArcAction.action.performed += OnMoveAlongArcPerformed;
    }

    void Update()
    {
        DrawRayCast();

        // Only draw the arc when not moving the object
        if (!isMoving)
        {
            DrawArc();
        }
    }

    void DrawRayCast()
    {
        // Ensure we are setting only 2 points for the RayCast
        lineRenderer.positionCount = 2;

        // Define the start point of the RayCast as the position of the castingObject
        startPoint = transform.position;

        // Define the end point of the RayCast
        endPoint = startPoint + transform.forward * rayLength;

        // Set the positions in the LineRenderer to make the RayCast visible
        lineRenderer.SetPosition(0, startPoint);
        lineRenderer.SetPosition(1, endPoint);
    }

    void DrawArc()
    {
        pointsAlongLine.Clear(); // Clear previous points
        lineRenderer.positionCount = arcSegments + 1; // Set the correct number of points

        // Loop through each segment to calculate the sine wave points
        for (int i = 0; i <= arcSegments; i++)
        {
            float t = i / (float)arcSegments; // Normalized value between 0 and 1 for interpolation
            Vector3 pointAlongLine = Vector3.Lerp(Vector3.zero, Vector3.forward * rayLength, t); // Interpolated point along the Z axis

            // Apply the sine wave offset along the Y axis
            float arcOffset = Mathf.Sin(Mathf.PI * t * sineFrequency) * sineAmplitude;
            Vector3 arcPoint = pointAlongLine + Vector3.up * arcOffset; // Apply sine wave to Y

            // Transform the point to world space from local space
            arcPoint = transform.TransformPoint(arcPoint);

            // Record this point in the list and LineRenderer
            pointsAlongLine.Add(arcPoint);
            lineRenderer.SetPosition(i, arcPoint); // Set the position for each segment
        }
    }

    bool RaycastHitObject()
    {
        RaycastHit hit;
        if (Physics.Raycast(startPoint, transform.forward, out hit, rayLength))
        {
            Debug.Log("Hit: " + hit.collider.name);
            objectToMove = hit.collider.gameObject; // Assign the first hit object to objectToMove
            return true;
        }
        return false;
    }

    void OnMoveAlongArcPerformed(InputAction.CallbackContext context)
    {
        // Trigger movement only if not already moving and an object is hit by RayCast
        if (!isMoving && RaycastHitObject())
        {
            StartCoroutine(MoveObjectAlongArc());
        }
    }

    IEnumerator MoveObjectAlongArc()
{
    isMoving = true;

    // Disable the LineRenderer when starting the movement
    lineRenderer.enabled = false;

    // Disable gravity if the object has a Rigidbody
    Rigidbody rb = objectToMove.GetComponent<Rigidbody>();
    if (rb != null)
    {
        rb.useGravity = false;
        rb.isKinematic = true;
    }

    // Use a local copy of the arc points to avoid modification issues
    List<Vector3> arcPointsCopy = new List<Vector3>(pointsAlongLine);
    arcPointsCopy.Reverse();

    // Move along each point in the arc
    foreach (Vector3 point in arcPointsCopy)
    {
        while (Vector3.Distance(objectToMove.transform.position, point) > 0.01f)
        {
            objectToMove.transform.position = Vector3.MoveTowards(objectToMove.transform.position, point, travelSpeed * Time.deltaTime);
            yield return null; // Wait until the next frame to continue moving
        }
    }

    // Re-enable gravity and LineRenderer after movement
    if (rb != null)
    {
        rb.useGravity = true;
        rb.isKinematic = false;
    }
    
    lineRenderer.enabled = true; // Re-enable the LineRenderer

    isMoving = false;
}

    private void OnDisable()
    {
        moveAlongArcAction.action.Disable();
        moveAlongArcAction.action.performed -= OnMoveAlongArcPerformed; // Unsubscribe from the event
    }
}
