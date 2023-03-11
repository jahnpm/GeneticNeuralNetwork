using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarLocator : MonoBehaviour
{
    public float totalSegmentProgress;
    public int Id;

    [SerializeField] private float curveAngle;
    [SerializeField] private float radius;

    private void Start()
    {
        if (curveAngle > 0)
            totalSegmentProgress = 2 * Mathf.PI * radius * (curveAngle / 360.0f);
        else
            totalSegmentProgress = transform.localScale.z;
    }

    public float segmentProgressAtPosition(Vector3 position)
    {
        float progress = 0;

        if (curveAngle > 0)
        {
            Vector2 v1 = new Vector2(position.x, position.z) - new Vector2(transform.position.x, transform.position.z);
            Vector2 v2 = new Vector2(-transform.right.x, -transform.right.z);

            float currentAngle = Vector2.Angle(v2, v1);
            progress = (currentAngle / curveAngle) * totalSegmentProgress;
        }
        else
        {
            Vector3 carInSegmentSpace = transform.InverseTransformPoint(position);
            progress = (carInSegmentSpace.z + 0.5f) * totalSegmentProgress;
        }

        return progress;
    }
}
