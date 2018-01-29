using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class HexGridChunk : MonoBehaviour
{
    HexCell[] cells;

    Canvas gridCanvas;
    public HexMesh terrain;

    public void AddCell(int index, HexCell cell)
    {
        cells[index] = cell;
        cell.chunk = this;
        cell.transform.SetParent(transform, false);
        cell.uiRect.SetParent(gridCanvas.transform, false);
    }

    public void Refresh()
    {
        //		terrain.Triangulate(cells);
        enabled = true;
    }

    void LateUpdate()
    {
        terrain.Triangulate(cells);
        enabled = false;
    }

    void Awake()
    {
        gridCanvas = GetComponentInChildren<Canvas>();
        terrain = GetComponentInChildren<HexMesh>();

        cells = new HexCell[HexMetrics.chunkSizeX * HexMetrics.chunkSizeZ];
        ShowUI(false);
    }

    public void ShowUI(bool visible)
    {
        gridCanvas.gameObject.SetActive(visible);
    }
}

