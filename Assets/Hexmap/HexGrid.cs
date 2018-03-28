using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class HexGrid : MonoBehaviour
{
    public Text         cellLabelPrefab;
    public HexCell      cellPrefab;
    public int          cellCountX = 20, cellCountZ = 15;
    public HexGridChunk chunkPrefab;
    public Texture2D    noiseSource;
    public int          seed;

    int            chunkCountX, chunkCountZ;
    HexCell[]      cells;
    HexGridChunk[] chunks;

    public HexCell GetCell(Vector3 position)
    {
        position = transform.InverseTransformPoint(position);
        HexCoordinates coordinates = HexCoordinates.FromPosition(position);
        int            index       = coordinates.X + coordinates.Z * this.cellCountX + coordinates.Z / 2;

        return cells[index];
    }

    void Awake()
    {
        HexMetrics.noiseSource = this.noiseSource;
        HexMetrics.InitializeHashGrid(seed);

        GetComponentInChildren<Canvas>();
        GetComponentInChildren<HexMesh>();
        CreateMap(cellCountX, cellCountZ);
    }

    [UsedImplicitly]
    public bool CreateMap(int x, int z)
    {
        if (x <= 0 || x % HexMetrics.chunkSizeX != 0 || z <= 0 || z % HexMetrics.chunkSizeZ != 0) {
            Debug.LogError("Unsupported map size.");

            return false;
        }

        if (chunks != null) {
            foreach (var c in this.chunks) {
                Destroy(c.gameObject);
            }
        }

        cellCountX  = x;
        cellCountZ  = z;
        chunkCountX = cellCountX / HexMetrics.chunkSizeX;
        chunkCountZ = cellCountZ / HexMetrics.chunkSizeZ;

        CreateChunks();
        CreateCells();

        return true;
    }

    void CreateCell(int x, int z, int i)
    {
        Vector3 position;
        position.x = (x + z * 0.5f - z / 2) * (HexMetrics.innerRadius * 2f);
        position.y = 0f;
        position.z = z * (HexMetrics.outerRadius * 1.5f);

        HexCell cell = cells[i] = Instantiate(cellPrefab);
        cell.transform.localPosition = position;
        cell.coordinates             = HexCoordinates.FromOffsetCoordinates(x, z);

        if (x > 0) {
            cell.SetNeighbor(HexDirection.W, cells[i - 1]);
        }

        if (z > 0) {
            if ((z & 1) == 0) {
                cell.SetNeighbor(HexDirection.SE, cells[i - this.cellCountX]);

                if (x > 0) {
                    cell.SetNeighbor(HexDirection.SW, cells[i - this.cellCountX - 1]);
                }
            } else {
                cell.SetNeighbor(HexDirection.SW, cells[i - this.cellCountX]);

                if (x < this.cellCountX - 1) {
                    cell.SetNeighbor(HexDirection.SE, cells[i - this.cellCountX + 1]);
                }
            }
        }

        Text label = Instantiate<Text>(cellLabelPrefab);
        label.rectTransform.anchoredPosition = new Vector2(position.x, position.z);
        cell.uiRect                          = label.rectTransform;

        cell.Elevation = 0;

        AddCellToChunk(x, z, cell);
    }

    public HexCell GetCell(HexCoordinates coordinates)
    {
        int z = coordinates.Z;

        if (z < 0 || z >= cellCountZ) {
            return null;
        }

        int x = coordinates.X + z / 2;

        if (x < 0 || x >= cellCountX) {
            return null;
        }

        return cells[x + z * cellCountX];
    }

    void AddCellToChunk(int x, int z, HexCell cell)
    {
        int chunkX = x / HexMetrics.chunkSizeX;
        int chunkZ = z / HexMetrics.chunkSizeZ;

        HexGridChunk chunk = chunks[chunkX + chunkZ * chunkCountX];

        int localX = x - chunkX * HexMetrics.chunkSizeX;
        int localZ = z - chunkZ * HexMetrics.chunkSizeZ;

        chunk.AddCell(localX + localZ * HexMetrics.chunkSizeX, cell);
    }

    private void CreateCells()
    {
        this.cells = new HexCell[this.cellCountZ * this.cellCountX];

        for (int z = 0, i = 0; z < this.cellCountZ; z++) {
            for (int x = 0; x < this.cellCountX; x++) {
                CreateCell(x, z, i++);
            }
        }
    }

    void CreateChunks()
    {
        chunks = new HexGridChunk[chunkCountX * chunkCountZ];

        for (int z = 0, i = 0; z < chunkCountZ; z++) {
            for (int x = 0; x < chunkCountX; x++) {
                HexGridChunk chunk = chunks[i++] = Instantiate(chunkPrefab);
                chunk.transform.SetParent(transform);
            }
        }
    }

    void OnEnable()
    {
        if (!HexMetrics.noiseSource) {
            HexMetrics.noiseSource = noiseSource;
            HexMetrics.InitializeHashGrid(seed);
        }
    }

    public void ShowUI(bool visible)
    {
        for (int i = 0; i < chunks.Length; i++) {
            chunks[i].ShowUI(visible);
        }
    }

    public void FindPath(HexCell fromCell, HexCell toCell)
    {
        StopAllCoroutines();
        StartCoroutine(Search(fromCell, toCell));
    }

    private IEnumerator Search(HexCell fromCell, HexCell toCell)
    {
        foreach (var cell in this.cells) {
            cell.Distance = int.MaxValue;
            cell.DisableHighlight();
        }

        fromCell.EnableHighlight(Color.blue);
        toCell.EnableHighlight(Color.red);

        WaitForSeconds delay = new WaitForSeconds(1 / 60f);

        var frontier = new List<HexCell>();
        fromCell.Distance = 0;
        frontier.Add(fromCell);

        while (frontier.Count > 0) {
            yield return delay;
            HexCell current = frontier[0];
            frontier.RemoveAt(0);

            if (current == toCell) {
                current = current.PathFrom;

                while (current != fromCell) {
                    current.EnableHighlight(Color.white);
                    current = current.PathFrom;
                }

                break;
            }

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++) {
                HexCell neighbor = current.GetNeighbor(d);

                if (neighbor == null) {
                    continue;
                }

                if (neighbor.IsUnderwater) {
                    continue;
                }

                HexEdgeType edgeType = current.GetEdgeType(neighbor);

                if (edgeType == HexEdgeType.Cliff) {
                    continue;
                }

                int distance = current.Distance;

                if (current.HasRoadThroughEdge(d)) {
                    distance += 1;
                } else if (current.Walled != neighbor.Walled) {
                    continue;
                } else {
                    distance += edgeType == HexEdgeType.Flat ? 5 : 10;
                    distance += neighbor.UrbanLevel + neighbor.FarmLevel + neighbor.PlantLevel;
                }

                if (neighbor.Distance == int.MaxValue) {
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                    neighbor.SearchHeuristic =
                        neighbor.coordinates.DistanceTo(toCell.coordinates);
                    frontier.Add(neighbor);
                } else if (distance < neighbor.Distance) {
                    neighbor.Distance = distance;
                    neighbor.PathFrom = current;
                }

                frontier.Sort((x, y) => x.SearchPriority.CompareTo(y.SearchPriority));
            }
        }
    }

    #region Serialization

    public void Save(BinaryWriter writer)
    {
        writer.Write(cellCountX);
        writer.Write(cellCountZ);

        foreach (var c in this.cells) {
            c.Save(writer);
        }
    }

    public void Load(BinaryReader reader, int header)
    {
        int x = 20, z = 15;

        if (header >= 1) {
            x = reader.ReadInt32();
            z = reader.ReadInt32();
        }

        if (x != cellCountX || z != cellCountZ) {
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
    }

    #endregion
}
