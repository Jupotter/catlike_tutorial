using UnityEngine;

public static class HexMetrics
{
    public const float blendFactor  = 1f - solidFactor;
    public const int   chunkSizeX   = 5, chunkSizeZ = 5;
    public const float innerRadius  = outerRadius * outerToInner;
    public const float innerToOuter = 1f          / outerToInner;
    public const float outerRadius  = 10f;
    public const float outerToInner = 0.866025404f;
    public const float solidFactor  = 0.8f;

    #region Corners

    static Vector3[] corners =
    {
        new Vector3(0f,           0f, outerRadius),
        new Vector3(innerRadius,  0f, 0.5f  * outerRadius),
        new Vector3(innerRadius,  0f, -0.5f * outerRadius),
        new Vector3(0f,           0f, -outerRadius),
        new Vector3(-innerRadius, 0f, -0.5f * outerRadius),
        new Vector3(-innerRadius, 0f, 0.5f  * outerRadius),
        new Vector3(0f,           0f, outerRadius)
    };

    public static Vector3 GetBridge(HexDirection direction)
    {
        return (corners[(int) direction] + corners[(int) direction + 1]) * blendFactor;
    }

    public static Vector3 GetFirstCorner(HexDirection direction)
    {
        return corners[(int) direction];
    }

    public static Vector3 GetFirstSolidCorner(HexDirection direction)
    {
        return corners[(int) direction] * solidFactor;
    }

    public static Vector3 GetSecondCorner(HexDirection direction)
    {
        return corners[(int) direction + 1];
    }

    public static Vector3 GetSecondSolidCorner(HexDirection direction)
    {
        return corners[(int) direction + 1] * solidFactor;
    }

    public static Vector3 GetSolidEdgeMiddle(HexDirection direction)
    {
        return (corners[(int) direction] + corners[(int) direction + 1]) * (0.5f * solidFactor);
    }

    #endregion

    #region Terrace

    public const float elevationStep    = 2f;
    public const int   terracesPerSlope = 2;
    public const int   terraceSteps     = terracesPerSlope * 2 + 1;

    public const float verticalTerraceStepSize   = 1f / (terracesPerSlope + 1);
    public const float horizontalTerraceStepSize = 1f / terraceSteps;

    public static HexEdgeType GetEdgeType(int elevation1, int elevation2)
    {
        if (elevation1 == elevation2) {
            return HexEdgeType.Flat;
        }

        int delta = elevation2 - elevation1;

        if (delta == 1 || delta == -1) {
            return HexEdgeType.Slope;
        }

        return HexEdgeType.Cliff;
    }

    public static Vector3 TerraceLerp(Vector3 a, Vector3 b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;
        a.x += (b.x - a.x) * h;
        a.z += (b.z - a.z) * h;
        // ReSharper disable once PossibleLossOfFraction
        float v = ((step + 1) / 2) * HexMetrics.verticalTerraceStepSize;
        a.y += (b.y - a.y) * v;

        return a;
    }

    public static Color TerraceLerp(Color a, Color b, int step)
    {
        float h = step * HexMetrics.horizontalTerraceStepSize;

        return Color.Lerp(a, b, h);
    }

    #endregion

    #region Noise

    public const float cellPerturbStrength      = 4f;
    public const float elevationPerturbStrength = 1f;
    public const float noiseScale               = 0.003f;

    public static Texture2D noiseSource;

    public static Vector3 Perturb(Vector3 position)
    {
        Vector4 sample = SampleNoise(position);
        position.x += (sample.x * 2f - 1f) * cellPerturbStrength;
        position.z += (sample.z * 2f - 1f) * cellPerturbStrength;

        return position;
    }

    public static Vector4 SampleNoise(Vector3 position)
    {
        return noiseSource.GetPixelBilinear(position.x * noiseScale, position.z * noiseScale);
    }

    #endregion

    #region Rivers

    public const float waterElevationOffset     = -0.5f;
    public const float streamBedElevationOffset = -1.75f;

    #endregion

    #region Water

    public const float waterFactor = 0.6f;

    public const float waterBlendFactor = 1f - waterFactor;

    public static Vector3 GetFirstWaterCorner(HexDirection direction)
    {
        return corners[(int) direction] * waterFactor;
    }

    public static Vector3 GetSecondWaterCorner(HexDirection direction)
    {
        return corners[(int) direction + 1] * waterFactor;
    }

    public static Vector3 GetWaterBridge(HexDirection direction)
    {
        return (corners[(int) direction] + corners[(int) direction + 1]) * waterBlendFactor;
    }

    #endregion

    #region Hashgrid

    public struct HexHash
    {
        public float a, b, c, d, e;

        public static HexHash Create()
        {
            HexHash hash;
            hash.a = Random.value * 0.999f;
            hash.b = Random.value * 0.999f;
            hash.c = Random.value * 0.999f;
            hash.d = Random.value * 0.999f;
            hash.e = Random.value * 0.999f;

            return hash;
        }
    }

    static       HexHash[] hashGrid;
    public const int       hashGridSize  = 256;
    public const float     hashGridScale = 0.25f;

    public static void InitializeHashGrid(int seed)
    {
        hashGrid = new HexHash[hashGridSize * hashGridSize];

        Random.State currentState = Random.state;
        Random.InitState(seed);

        for (int i = 0; i < hashGrid.Length; i++) {
            hashGrid[i] = HexHash.Create();
        }

        Random.state = currentState;
    }

    public static HexHash SampleHashGrid(Vector3 position)
    {
        int x = (int) (position.x * hashGridScale) % hashGridSize;

        if (x < 0) {
            x += hashGridSize;
        }

        int z = (int) (position.z * hashGridScale) % hashGridSize;

        if (z < 0) {
            z += hashGridSize;
        }

        return hashGrid[x + z * hashGridSize];
    }

    #endregion

    #region Features

    static float[][] featureThresholds =
    {
        new float[] { 0.0f, 0.0f, 0.4f },
        new float[] { 0.0f, 0.4f, 0.6f },
        new float[] { 0.4f, 0.6f, 0.8f }
    };

    public const float wallHeight          = 4f;
    public const float wallThickness       = 0.75f;
    public const float wallYOffset         = -1f;
    public const float wallElevationOffset = verticalTerraceStepSize;
    public const float wallTowerThreshold  = 0.5f;

    public const float bridgeDesignLength = 7f;

    public static float[] GetFeatureThresholds(int level)
    {
        return featureThresholds[level];
    }

    public static Vector3 WallThicknessOffset(Vector3 near, Vector3 far)
    {
        Vector3 offset;
        offset.x = far.x - near.x;
        offset.y = 0f;
        offset.z = far.z - near.z;

        return offset.normalized * (wallThickness * 0.5f);
    }

    public static Vector3 WallLerp(Vector3 near, Vector3 far)
    {
        near.x += (far.x - near.x) * 0.5f;
        near.z += (far.z - near.z) * 0.5f;
        float v = near.y < far.y ? wallElevationOffset : (1f - wallElevationOffset);
        near.y += (far.y - near.y) * v + wallYOffset;

        return near;
    }

    #endregion
}