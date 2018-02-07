﻿using UnityEngine;

public class HexFeatureManager : MonoBehaviour
{
    public Transform featurePrefab;

    private Transform container;

    public void Clear()
    {
        if (container) {
            Destroy(container.gameObject);
        }

        container = new GameObject("Features Container").transform;
        container.SetParent(transform, false);
    }

    public void Apply()
    { }

    public void AddFeature(Vector3 position)
    {
        Transform instance     = Instantiate(featurePrefab);
        position.y             += instance.localScale.y * 0.5f;
        instance.localPosition =  HexMetrics.Perturb(position);
        instance.localRotation =  Quaternion.Euler(0f, 360f * Random.value, 0f);
        instance.SetParent(container, false);
    }
}