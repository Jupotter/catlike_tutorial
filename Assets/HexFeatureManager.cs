using UnityEngine;

[System.Serializable]
public struct HexFeatureCollection
{

    public Transform[] prefabs;

    public Transform Pick(float choice)
    {
        return prefabs[(int)(choice * prefabs.Length)];
    }
}

public class HexFeatureManager : MonoBehaviour
{
    public HexFeatureCollection[] urbanCollections, farmCollections, plantCollections;

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

    public void AddFeature(HexCell cell, Vector3 position)
    {
        HexMetrics.HexHash hash = HexMetrics.SampleHashGrid(position);

        Transform prefab = PickPrefab(urbanCollections, cell.UrbanLevel, hash.a, hash.d);
        if (!prefab) {
            return;
        }
        Transform otherPrefab = PickPrefab(
            farmCollections, cell.FarmLevel, hash.b, hash.d
        );

        Transform instance     = Instantiate(prefab);
        position.y             += instance.localScale.y * 0.5f;
        instance.localPosition =  HexMetrics.Perturb(position);
        instance.localRotation =  Quaternion.Euler(0f, 360f * hash.e, 0f);
        instance.SetParent(container, false);
    }

    Transform PickPrefab(HexFeatureCollection[] collections, int level, float hash, float choice)
    {
        if (level > 0) {
            float[] thresholds = HexMetrics.GetFeatureThresholds(level - 1);
            for (int i = 0; i < thresholds.Length; i++) {
                if (hash      < thresholds[i]) {
                    return collections[i].Pick(choice);
                }
            }
        }

        return null;
    }
}