using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public class HexGrid : MonoBehaviour
{
    private int            chunkCountX, chunkCountZ;
    private HexCell[]      cells;
    private HexGridChunk[] chunks;
    public  Text           cellLabelPrefab;
    public  HexCell        cellPrefab;
    public  int            cellCountX = 20, cellCountZ = 15;
    public  HexGridChunk   chunkPrefab;
    public  Texture2D      noiseSource;
    public  HexUnit        unitPrefab;
    public  int            seed;

    private void Awake()
    {
        HexMetrics.noiseSource = this.noiseSource;
        HexMetrics.InitializeHashGrid(this.seed);
        HexUnit.unitPrefab = this.unitPrefab;

        CreateMap(this.cellCountX, this.cellCountZ);
    }

    private void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        var cell = this.cells[i] = Instantiate(this.cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates             = HexCoordinates.FromOffsetCoordinates(x, z);

        if (x > 0) {
            cell.SetNeighbor(HexDirection.W, this.cells[i - 1]);
        }

        if (z > 0) {
            if ((z & 1) == 0) {
                cell.SetNeighbor(HexDirection.SE, this.cells[i - this.cellCountX]);

                if (x > 0) {
                    cell.SetNeighbor(HexDirection.SW, this.cells[i - this.cellCountX - 1]);
                }
            } else {
                cell.SetNeighbor(HexDirection.SW, this.cells[i - this.cellCountX]);

                if (x < this.cellCountX - 1) {
                    cell.SetNeighbor(HexDirection.SE, this.cells[i - this.cellCountX + 1]);
                }
            }
        }

        var label = Instantiate(this.cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        cell.uiRect                          = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    private void AddCellToChunk(int x, int z, HexCell cell)
    {
        var chunkX = x / HexMetrics.chunkSizeX;
        var chunkZ = z / HexMetrics.chunkSizeZ;

        var chunk = this.chunks[chunkX + chunkZ * this.chunkCountX];

        var localX = x - chunkX * HexMetrics.chunkSizeX;
        var localZ = z - chunkZ * HexMetrics.chunkSizeZ;

        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    private void CreateCells()
    {
        this.cells = new HexCell[this.cellCountZ * this.cellCountX];

        for (int z = 0, i = 0; z < this.cellCountZ; z++) {
            for (var x = 0; x < this.cellCountX; x++) {
                CreateCell(x, z, i++);
            }
        }
    }

    private void CreateChunks()
    {
        this.chunks = new HexGridChunk[this.chunkCountX * this.chunkCountZ];

        for (int z = 0, i = 0; z < this.chunkCountZ; z++) {
            for (var x = 0; x < this.chunkCountX; x++) {
                var chunk = this.chunks[i++] = Instantiate(this.chunkPrefab);
                chunk.transform.SetParent(this.transform);
            }
        }
    }

    private void OnEnable()
    {
        if (!HexMetrics.noiseSource) {
            HexMetrics.noiseSource = this.noiseSource;
            HexMetrics.InitializeHashGrid(this.seed);
            HexUnit.unitPrefab = this.unitPrefab;
        }
    }

    public HexCell GetCell(Vector3 position)
    {
        position = this.transform.InverseTransformPoint(position);
        var coordinates = HexCoordinates.FromPosition(position);
        var index       = coordinates.X + coordinates.Z * this.cellCountX + coordinates.Z / 2;

        return this.cells[index];
    }

    [UsedImplicitly]
    public bool CreateMap(int x, int z)
    {
        if ((x <= 0) || (x % HexMetrics.chunkSizeX != 0) || (z <= 0) || (z % HexMetrics.chunkSizeZ != 0)) {
            Debug.LogError("Unsupported map size.");

            return false;
        }

        ClearPath();
        ClearUnits();

        if (this.chunks != null) {
            foreach (var c in this.chunks) {
                Destroy(c.gameObject);
            }
        }

        this.cellCountX  = x;
        this.cellCountZ  = z;
        this.chunkCountX = this.cellCountX / HexMetrics.chunkSizeX;
        this.chunkCountZ = this.cellCountZ / HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();

        return true;
    }

    public HexCell GetCell(Ray ray)
    {
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit)) {
            return GetCell(hit.point);
        }

        return null;
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        var z = coordinates.Z;

        if ((z < 0) || (z >= this.cellCountZ)) {
            return null;
        }

        var x = coordinates.X + z / 2;

        if ((x < 0) || (x >= this.cellCountX)) {
            return null;
        }

        return this.cells[x + z * this.cellCountX];
    }

    public void ShowUI(bool visible)
    {
        for (var i = 0; i < this.chunks.Length; i++) {
            this.chunks[i].ShowUI(visible);
        }
    }

    #region Pathfinding

    private HexCellPriorityQueue searchFrontier;
    private int                  searchFrontierPhase;
    private HexCell              currentPathFrom, currentPathTo;

    public bool HasPath { get; private set; }

    private void ShowPath(int speed)
    {
        if (this.HasPath) {
            var current = this.currentPathTo;

            while (current != this.currentPathFrom) {
                var turn = (current.Distance - 1) / speed;
                current.SetLabel(turn.ToString());
                current.EnableHighlight(Color.white);
                current = current.PathFrom;
            }
        }

        this.currentPathFrom.EnableHighlight(Color.blue);
        this.currentPathTo.EnableHighlight(Color.red);
    }

    private bool Search(HexCell fromCell, HexCell toCell, int speed)
    {
        this.searchFrontierPhase += 2;

        if (this.searchFrontier == null) {
            this.searchFrontier = new HexCellPriorityQueue();
        } else {
            this.searchFrontier.Clear();
        }

        fromCell.EnableHighlight(Color.blue);
        fromCell.SearchPhase = this.searchFrontierPhase;
        fromCell.Distance    = 0;
        this.searchFrontier.Enqueue(fromCell);

        while (this.searchFrontier.Count > 0) {
            var current = this.searchFrontier.Dequeue();
            current.SearchPhase += 1;

            if (current == toCell) {
                return true;
            }

            var currentTurn = (current.Distance - 1) / speed;

            for (var d = HexDirection.NE; d <= HexDirection.NW; d++) {
                var neighbor = current.GetNeighbor(d);

                if ((neighbor == null) || (neighbor.SearchPhase > this.searchFrontierPhase) || neighbor.Unit) {
                    continue;
                }

                if (neighbor.IsUnderwater) {
                    continue;
                }

                var edgeType = current.GetEdgeType(neighbor);

                if (edgeType == HexEdgeType.Cliff) {
                    continue;
                }

                int moveCost;

                if (current.HasRoadThroughEdge(d)) {
                    moveCost = 1;
                } else if (current.Walled != neighbor.Walled) {
                    continue;
                } else {
                    moveCost =  edgeType == HexEdgeType.Flat ? 5 : 10;
                    moveCost += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                }

                var distance = current.Distance + moveCost;
                var turn     = (distance - 1) / speed;

                if (turn > currentTurn) {
                    distance = turn * speed + moveCost;
                }

                if (neighbor.SearchPhase < this.searchFrontierPhase) {
                    neighbor.SearchPhase     = this.searchFrontierPhase;
                    neighbor.Distance        = distance;
                    neighbor.PathFrom        = current;
                    neighbor.SearchHeuristic = neighbor.coordinates.DistanceTo(toCell.coordinates);
                    this.searchFrontier.Enqueue(neighbor);
                } else if (distance < neighbor.Distance) {
                    var oldPriority = neighbor.SearchPriority;
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    this.searchFrontier.Change(neighbor, oldPriority);
                }
            }
        }

        return false;
    }

    public void FindPath(HexCell fromCell, HexCell toCell, int speed)
    {
        var sw = new Stopwatch();
        sw.Start();

        ClearPath();
        this.currentPathFrom = fromCell;
        this.currentPathTo   = toCell;
        this.HasPath         = Search(fromCell, toCell, speed);
        ShowPath(speed);

        sw.Stop();
        Debug.Log(sw.ElapsedMilliseconds);
    }

    public void ClearPath()
    {
        if (this.HasPath) {
            var current = this.currentPathTo;

            while (current != this.currentPathFrom) {
                current.SetLabel(null);
                current.DisableHighlight();
                current = current.PathFrom;
            }

            current.DisableHighlight();
            this.HasPath = false;
        }

        this.currentPathFrom = this.currentPathTo = null;
    }

    public List<HexCell> GetPath()
    {
        if (!HasPath) {
            return null;
        }

        List<HexCell> path = ListPool<HexCell>.Get();

        for (HexCell c = currentPathTo; c != currentPathFrom; c = c.PathFrom) {
            path.Add(c);
        }
        path.Add(currentPathFrom);
        path.Reverse();

        return path;
    }

    #endregion

    #region Serialization

    public void Save(BinaryWriter writer)
    {
        writer.Write(this.cellCountX);
        writer.Write(this.cellCountZ);

        foreach (var c in this.cells) {
            c.Save(writer);
        }

        writer.Write(this.units.Count);

        for (var i = 0; i < this.units.Count; i++) {
            this.units[i].Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        ClearPath();
        ClearUnits();
        int x = 20, z = 15;

        if (header >= 1) {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        if ((x != this.cellCountX) || (z != this.cellCountZ)) {
            if (!CreateMap(x, z)) {
                return;
            }
        }

        foreach (var c in this.cells) {
            c.Load(reader);
        }

        foreach (var c in this.chunks) {
            c.Refresh();
        }

        if (header >= 2) {
            var unitCount = reader.ReadInt32();

            for (var i = 0; i < unitCount; i++) {
                HexUnit.Load(reader, this);
            }
        }
    }

    #endregion

    #region Units

    private readonly List<HexUnit> units = new List<HexUnit>();

    private void ClearUnits()
    {
        for (var i = 0; i < this.units.Count; i++) {
            this.units[i].Die();
        }

        this.units.Clear();
    }

    public void AddUnit(HexUnit unit, HexCell location, float orientation)
    {
        this.units.Add(unit);
        unit.transform.SetParent(this.transform, false);
        unit.Location    = location;
        unit.Orientation = orientation;
    }

    public void RemoveUnit(HexUnit unit)
    {
        this.units.Remove(unit);
        unit.Die();
    }

    #endregion
}
