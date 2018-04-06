﻿using System;
using UnityEngine;

public class HexMapCamera : MonoBehaviour
{
    static HexMapCamera instance;

    Transform swivel, stick;

    float         zoom = 1f;
    private float rotationAngle;

    public float stickMinZoom,     stickMaxZoom;
    public float swivelMinZoom,    swivelMaxZoom;
    public float moveSpeedMinZoom, moveSpeedMaxZoom;
    public float rotationSpeed;

    public HexGrid grid;

    public static bool Locked
    {
        set { if (instance != null) instance.enabled = !value; }
    }

    public static void ValidatePosition()
    {
        if (instance != null)
            instance.AdjustPosition(0f, 0f);
    }

    void Awake()
    {
        instance = this;
        swivel   = transform.GetChild(0);
        stick    = swivel.GetChild(0);
    }

    void Update()
    {
        const float ε         = 0.001f;
        float       zoomDelta = Input.GetAxis("Mouse ScrollWheel");

        if (Math.Abs(zoomDelta) > ε) {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");

        if (Math.Abs(rotationDelta) > ε) {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");

        if (Math.Abs(xDelta) > ε || Math.Abs(zDelta) > ε) {
            AdjustPosition(xDelta, zDelta);
        }
    }

    private void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;

        if (rotationAngle < 0f) {
            rotationAngle += 360f;
        } else if (rotationAngle >= 360f) {
            rotationAngle -= 360f;
        }

        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }

    void AdjustPosition(float xDelta, float zDelta)
    {
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        float   damping   = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float   distance  = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom) * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position                += direction * distance;
        transform.localPosition =  ClampPosition(position);
    }

    Vector3 ClampPosition(Vector3 position)
    {
        float xMax = (grid.cellCountX - 0.5f) * (2f * HexMetrics.innerRadius);
        position.x = Mathf.Clamp(position.x, 0f, xMax);

        float zMax = (grid.cellCountZ - 1) * (1.5f * HexMetrics.outerRadius);
        position.z = Mathf.Clamp(position.z, 0f, zMax);

        return position;
    }

    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0f, 0f, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0f, 0f);
    }
}
