using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.Windows;

public class CarController : MonoBehaviour
{
    public NeuralNet brain;
    public bool killed;

    [SerializeField] private float maximumAcceleration;
    [SerializeField] private float maximumSteering;
    [SerializeField] private float maximumSpeed;
    [SerializeField] private bool humanControlled;
    [SerializeField] private LayerMask raycastTargets;

    private CarLocator currentSegment = null;
    private float cummulativeProgress = 0;
    private float progressOnSegment = 0;
    private float progress = 0;
    private float timestamp = 0;
    private float stopWatch = 0;

    private float accelerationInput = 0;
    private float steeringInput = 0;
    private float currentSpeed = 0;
    private float currentMaximumSpeed = 0;
    private bool isTouchingWall = false;

    private Camera cam;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cam = Camera.main;
    }

    public void Reset()
    {
        transform.position = new Vector3(-25f, 0.51f, -7f);
        transform.rotation = Quaternion.identity;

        killed = false;

        currentSegment = null;
        cummulativeProgress = 0;
        progressOnSegment = 0;
        progress = 0;
        timestamp = 0;
        stopWatch = 0;

        accelerationInput = 0;
        steeringInput = 0;
        currentSpeed = 0;
        currentMaximumSpeed = 0;
        isTouchingWall = false;
    }

    void Update()
    {
        stopWatch += Time.deltaTime;

        if (stopWatch >= 25.0f)
        {
            killed = true;

            if (humanControlled)
                Reset();
        }

        if (humanControlled)
        {
            accelerationInput = UnityEngine.Input.GetAxis("Trigger");
            steeringInput = UnityEngine.Input.GetAxis("Horizontal");
        }
        else
        {
            if (!killed)
                brain.fitness = progress;

            FeedNeuralNetwork();
        }

        if (accelerationInput > 0)
        {
            currentSpeed += accelerationInput * Time.deltaTime * maximumAcceleration;
        }

        if (currentMaximumSpeed > (accelerationInput * maximumSpeed))
        {
            currentMaximumSpeed -= maximumAcceleration * Time.deltaTime;
        }

        if (currentMaximumSpeed < (accelerationInput * maximumSpeed))
        {
            currentMaximumSpeed = accelerationInput * maximumSpeed;
        }

        currentSpeed = Mathf.Clamp(currentSpeed, 0, isTouchingWall ? 0.5f * currentMaximumSpeed : currentMaximumSpeed);

        updateTrackProgress();
    }

    private void FeedNeuralNetwork()
    {
        float[] inputs = new float[5];

        float distanceRight = 0;
        float distanceLeft = 0;
        bool rightIsInnerWall = false;
        bool leftIsInnerWall = false;

        Ray ray1 = new Ray(transform.position + 0.4f * Vector3.up, Quaternion.AngleAxis(45, Vector3.up) * transform.forward);
        Ray ray2 = new Ray(transform.position + 0.4f * Vector3.up, Quaternion.AngleAxis(-45, Vector3.up) * transform.forward);

        Debug.DrawRay(ray1.origin, 10.0f * ray1.direction, Color.white);
        Debug.DrawRay(ray2.origin, 10.0f * ray2.direction, Color.white);

        RaycastHit hitInfo1 = new RaycastHit();
        RaycastHit hitInfo2 = new RaycastHit();

        bool hit1 = Physics.Raycast(ray1, out hitInfo1, 10.0f, raycastTargets);
        bool hit2 = Physics.Raycast(ray2, out hitInfo2, 10.0f, raycastTargets);

        if (hit1 && hitInfo1.transform.CompareTag("Wall"))
        {
            distanceRight = 10.0f - hitInfo1.distance;
            rightIsInnerWall = hitInfo1.transform.GetComponent<WallSegment>().isInnerWall;
        }
        else
        {
            distanceRight = 0;
        }

        if (hit2 && hitInfo2.transform.CompareTag("Wall"))
        {
            distanceLeft = 10.0f - hitInfo2.distance;
            leftIsInnerWall = hitInfo2.transform.GetComponent<WallSegment>().isInnerWall;
        }
        else
        {
            distanceLeft = 0;
        }

        inputs[0] = distanceRight;
        inputs[1] = distanceLeft;
        inputs[2] = leftIsInnerWall ? 1f : 0f;
        inputs[3] = rightIsInnerWall ? 1f : 0f;
        inputs[4] = currentSpeed;

        Matrix<float> output = brain.Fire(inputs);

        accelerationInput = (output[0, 0] + 1.0f) / 2.0f;
        steeringInput = output[0, 1];
    }

    private void updateTrackProgress()
    {
        Ray ray = new Ray(transform.position, Vector3.down);
        RaycastHit hitInfo = new RaycastHit();

        bool hit = Physics.Raycast(ray, out hitInfo, 0.1f, raycastTargets);

        if (hit && hitInfo.transform.CompareTag("Floor"))
        {
            CarLocator hitSegment = hitInfo.transform.parent.GetComponent<CarLocator>();
            float prog = hitSegment.segmentProgressAtPosition(transform.position);

            if (currentSegment == null)
                currentSegment = hitSegment;

            if (hitSegment.Id == currentSegment.Id)
            {
                if (prog > progressOnSegment)
                {
                    progressOnSegment = prog;
                }
            }
            else if (hitSegment.Id == currentSegment.Id + 1 || (hitSegment.Id == 0 && currentSegment.Id == 13))
            {
                cummulativeProgress += currentSegment.totalSegmentProgress;
                progressOnSegment = prog;

                currentSegment = hitSegment;
            }
        }

        float newProg = cummulativeProgress + progressOnSegment;

        if (newProg > progress)
        {
            timestamp = stopWatch;
        }

        progress = newProg;
    }

    private void LateUpdate()
    {
        if (humanControlled)
        {
            cam.transform.position = transform.position + 10 * Vector3.up;
            cam.transform.LookAt(transform.position);
        }
    }

    private void FixedUpdate()
    {
        rb.MovePosition(transform.position + currentSpeed * Time.fixedDeltaTime * transform.forward);

        Quaternion deltaRotation = Quaternion.Euler(0, steeringInput * maximumSteering * Time.fixedDeltaTime, 0);
        rb.MoveRotation(rb.rotation * deltaRotation);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
            isTouchingWall = true;
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
            isTouchingWall = false;
    }
}
