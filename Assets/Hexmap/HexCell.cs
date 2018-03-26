using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class HexCell : MonoBehaviour
{
    public HexGridChunk chunk;

    public HexCoordinates coordinates;
    public RectTransform  uiRect;

    private int terrainTypeIndex;
    private int distance;
    private int elevation = int.MinValue;

    [SerializeField] HexCell[] neighbors;

    public int Distance
    {
        get
        {
            return distance;
        }
        set
        {
            distance = value;
            UpdateDistanceLabel();
        }
    }

    public int TerrainTypeIndex
    {
        get { return terrainTypeIndex; }
        set
        {
            if (terrainTypeIndex != value) {
                terrainTypeIndex = value;
                Refresh();
            }
        }
    }

    public int Elevation
    {
        get { return this.elevation; }
        set
        {
            if (elevation == value) {
                return;
            }

            this.elevation = value;
            RefreshPosition();
            ValidateRivers();

            for (int i = 0; i < roads.Length; i++) {
                if (roads[i] && GetElevationDifference((HexDirection) i) > 1) {
                    SetRoad(i, false);
                }
            }

            Refresh();
        }
    }

    public Vector3 Position
    {
        get { return transform.localPosition; }
    }

    private void RefreshPosition()
    {
        Vector3 position = this.transform.localPosition;
        position.y = elevation * HexMetrics.elevationStep;
        position.y +=
            (HexMetrics.SampleNoise(position).y * 2f - 1f) * HexMetrics.elevationPerturbStrength;
        this.transform.localPosition = position;

        Vector3 uiPosition = this.uiRect.localPosition;
        uiPosition.z              = -position.y;
        this.uiRect.localPosition = uiPosition;
    }
    public HexEdgeType GetEdgeType(HexDirection direction)
    {
        return HexMetrics.GetEdgeType(elevation, neighbors[(int) direction].elevation);
    }

    public HexEdgeType GetEdgeType(HexCell otherCell)
    {
        return HexMetrics.GetEdgeType(elevation, otherCell.elevation);
    }

    public HexCell GetNeighbor(HexDirection direction)
    {
        return neighbors[(int) direction];
    }

    public void SetNeighbor(HexDirection direction, HexCell cell)
    {
        neighbors[(int) direction]                 = cell;
        cell.neighbors[(int) direction.Opposite()] = this;
    }

    void Refresh()
    {
        if (chunk) {
            chunk.Refresh();

            for (int i = 0; i < neighbors.Length; i++) {
                HexCell neighbor = neighbors[i];

                if (neighbor != null && neighbor.chunk != chunk) {
                    neighbor.chunk.Refresh();
                }
            }
        }
    }

    void RefreshSelfOnly()
    {
        chunk.Refresh();
    }

    public int GetElevationDifference(HexDirection direction)
    {
        int difference = elevation - GetNeighbor(direction).elevation;

        return difference >= 0 ? difference : -difference;
    }

    void UpdateDistanceLabel()
    {
        Text label = uiRect.GetComponent<Text>();
        label.text = distance == int.MaxValue ? "" : distance.ToString();
    }

    #region rivers

    bool         hasIncomingRiver, hasOutgoingRiver;
    HexDirection incomingRiver,    outgoingRiver;

    public HexDirection RiverBeginOrEndDirection
    {
        get { return hasIncomingRiver ? incomingRiver : outgoingRiver; }
    }

    public bool HasIncomingRiver
    {
        get { return hasIncomingRiver; }
    }

    public bool HasOutgoingRiver
    {
        get { return hasOutgoingRiver; }
    }

    public bool HasRiver
    {
        get { return hasIncomingRiver || hasOutgoingRiver; }
    }

    public bool HasRiverBeginOrEnd
    {
        get { return hasIncomingRiver != hasOutgoingRiver; }
    }

    public HexDirection IncomingRiver
    {
        get { return incomingRiver; }
    }

    public HexDirection OutgoingRiver
    {
        get { return outgoingRiver; }
    }

    public float RiverSurfaceY
    {
        get { return (elevation + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep; }
    }

    public float StreamBedY
    {
        get { return (elevation + HexMetrics.streamBedElevationOffset) * HexMetrics.elevationStep; }
    }

    public bool HasRiverThroughEdge(HexDirection direction)
    {
        return hasIncomingRiver && incomingRiver == direction || hasOutgoingRiver && outgoingRiver == direction;
    }

    public void RemoveIncomingRiver()
    {
        if (!hasIncomingRiver) {
            return;
        }

        hasIncomingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(incomingRiver);
        neighbor.hasOutgoingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveOutgoingRiver()
    {
        if (!hasOutgoingRiver) {
            return;
        }

        hasOutgoingRiver = false;
        RefreshSelfOnly();

        HexCell neighbor = GetNeighbor(outgoingRiver);
        neighbor.hasIncomingRiver = false;
        neighbor.RefreshSelfOnly();
    }

    public void RemoveRiver()
    {
        RemoveOutgoingRiver();
        RemoveIncomingRiver();
    }

    public void SetOutgoingRiver(HexDirection direction)
    {
        if (hasOutgoingRiver && outgoingRiver == direction) {
            return;
        }

        HexCell neighbor = GetNeighbor(direction);

        if (!IsValidRiverDestination(neighbor)) {
            return;
        }

        RemoveOutgoingRiver();

        if (hasIncomingRiver && incomingRiver == direction) {
            RemoveIncomingRiver();
        }

        hasOutgoingRiver = true;
        outgoingRiver    = direction;
        specialIndex     = 0;

        neighbor.RemoveIncomingRiver();
        neighbor.hasIncomingRiver = true;
        neighbor.incomingRiver    = direction.Opposite();
        neighbor.specialIndex     = 0;

        SetRoad((int) direction, false);
    }

    bool IsValidRiverDestination(HexCell neighbor)
    {
        return neighbor && (elevation >= neighbor.elevation || waterLevel == neighbor.elevation);
    }

    void ValidateRivers()
    {
        if (hasOutgoingRiver && !IsValidRiverDestination(GetNeighbor(outgoingRiver))) {
            RemoveOutgoingRiver();
        }

        if (hasIncomingRiver && !GetNeighbor(incomingRiver).IsValidRiverDestination(this)) {
            RemoveIncomingRiver();
        }
    }

    #endregion rivers

    #region Roads

    [SerializeField] bool[] roads;

    public bool HasRoadThroughEdge(HexDirection direction)
    {
        return roads[(int) direction];
    }

    public bool HasRoads
    {
        get { return this.roads.Any(t => t); }
    }

    public void RemoveRoads()
    {
        for (int i = 0; i < neighbors.Length; i++) {
            if (roads[i]) {
                SetRoad(i, false);
            }
        }
    }

    private void SetRoad(int i, bool state)
    {
        this.roads[i]                                                = state;
        this.neighbors[i].roads[(int) ((HexDirection) i).Opposite()] = state;
        this.neighbors[i].RefreshSelfOnly();
        RefreshSelfOnly();
    }

    public void AddRoad(HexDirection direction)
    {
        if (!roads[(int) direction]
            && !HasRiverThroughEdge(direction)
            && !IsSpecial
            && !GetNeighbor(direction).IsSpecial
            && GetElevationDifference(direction) <= 1) {
            int i = (int) direction;
            SetRoad(i, true);
        }
    }

    #endregion

    #region Water

    int waterLevel;

    public int WaterLevel
    {
        get { return waterLevel; }
        set
        {
            if (waterLevel == value) {
                return;
            }

            waterLevel = value;
            ValidateRivers();
            Refresh();
        }
    }

    public bool IsUnderwater
    {
        get { return waterLevel > elevation; }
    }

    public float WaterSurfaceY
    {
        get { return (waterLevel + HexMetrics.waterElevationOffset) * HexMetrics.elevationStep; }
    }

    #endregion

    #region Features

    int urbanLevel, farmLevel, plantLevel;
    int specialIndex;

    public int UrbanLevel
    {
        get { return urbanLevel; }
        set
        {
            if (urbanLevel != value) {
                urbanLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int FarmLevel
    {
        get { return farmLevel; }
        set
        {
            if (farmLevel != value) {
                farmLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int PlantLevel
    {
        get { return plantLevel; }
        set
        {
            if (plantLevel != value) {
                plantLevel = value;
                RefreshSelfOnly();
            }
        }
    }

    public int SpecialIndex
    {
        get { return specialIndex; }
        set
        {
            if (specialIndex != value && !HasRiver) {
                specialIndex = value;
                RemoveRoads();
                RefreshSelfOnly();
            }
        }
    }

    public bool IsSpecial
    {
        get { return specialIndex > 0; }
    }

    #endregion

    #region Walls

    public bool Walled
    {
        get { return walled; }
        set
        {
            if (walled != value) {
                walled = value;
                Refresh();
            }
        }
    }

    bool walled;

    #endregion

    #region Serialization

    public void Save(BinaryWriter writer)
    {
        writer.Write((byte) terrainTypeIndex);
        writer.Write((byte) elevation);
        writer.Write((byte) waterLevel);
        writer.Write((byte) urbanLevel);
        writer.Write((byte) farmLevel);
        writer.Write((byte) plantLevel);
        writer.Write((byte) specialIndex);
        writer.Write(walled);

        if (hasIncomingRiver) {
            writer.Write((byte) (incomingRiver + 128));
        } else {
            writer.Write((byte) 0);
        }

        if (hasOutgoingRiver) {
            writer.Write((byte) (outgoingRiver + 128));
        } else {
            writer.Write((byte) 0);
        }

        int roadFlags = 0;

        for (int i = 0; i < roads.Length; i++) {
            if (roads[i]) {
                roadFlags |= 1 << i;
            }
        }

        writer.Write((byte) roadFlags);
    }

    public void Load(BinaryReader reader)
    {
        terrainTypeIndex = reader.ReadByte();
        elevation        = reader.ReadByte();
        RefreshPosition();
        waterLevel   = reader.ReadByte();
        urbanLevel   = reader.ReadByte();
        farmLevel    = reader.ReadByte();
        plantLevel   = reader.ReadByte();
        specialIndex = reader.ReadByte();
        walled       = reader.ReadBoolean();

        byte riverData = reader.ReadByte();

        if (riverData >= 128) {
            hasIncomingRiver = true;
            incomingRiver    = (HexDirection) (riverData - 128);
        } else {
            hasIncomingRiver = false;
        }

        riverData = reader.ReadByte();

        if (riverData >= 128) {
            hasOutgoingRiver = true;
            outgoingRiver    = (HexDirection) (riverData - 128);
        } else {
            hasOutgoingRiver = false;
        }

        int roadFlags = reader.ReadByte();

        for (int i = 0; i < roads.Length; i++) {
            roads[i] = (roadFlags & (1 << i)) != 0;
        }
    }

    #endregion
}
