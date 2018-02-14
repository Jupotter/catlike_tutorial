using System;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public Color[] colors;
    public HexGrid hexGrid;

    HexDirection dragDirection;
    bool         isDrag;
    HexCell      previousCell;

    private Color activeColor;
    private int   activeElevation;
    private int   activeWaterLevel;
    private int   activeUrbanLevel, activeFarmLevel, activePlantLevel;
    private bool  applyColor;
    private bool  applyElevation  = true;
    private bool  applyWaterLevel = true;
    private bool  applyUrbanLevel, applyFarmLevel, applyPlantLevel;
    private int   brushSize;

    private OptionalToggle riverMode, roadMode;

    enum OptionalToggle
    {
        [UsedImplicitly] Ignore,
        Yes,
        No
    }

    [UsedImplicitly]
    public void SelectColor(int index)
    {
        applyColor = index >= 0;
        if (applyColor) {
            activeColor = colors[index];
        }
    }

    [UsedImplicitly]
    public void SetApplyElevation(bool toggle)
    {
        applyElevation = toggle;
    }

    [UsedImplicitly]
    public void SetBrushSize(float size)
    {
        brushSize = (int) size;
    }

    [UsedImplicitly]
    public void SetElevation(float elevation)
    {
        activeElevation = (int) elevation;
    }

    [UsedImplicitly]
    public void SetRiverMode(int mode)
    {
        riverMode = (OptionalToggle) mode;
    }

    [UsedImplicitly]
    public void SetRoadMode(int mode)
    {
        roadMode = (OptionalToggle) mode;
    }

    [UsedImplicitly]
    public void SetApplyWaterLevel(bool toggle)
    {
        applyWaterLevel = toggle;
    }

    [UsedImplicitly]
    public void SetWaterLevel(float level)
    {
        activeWaterLevel = (int) level;
    }

    [UsedImplicitly]
    public void SetApplyUrbanLevel(bool toggle)
    {
        applyUrbanLevel = toggle;
    }

    public void SetApplyFarmLevel(bool toggle)
    {
        applyFarmLevel = toggle;
    }

    public void SetFarmLevel(float level)
    {
        activeFarmLevel = (int)level;
    }

    public void SetApplyPlantLevel(bool toggle)
    {
        applyPlantLevel = toggle;
    }

    public void SetPlantLevel(float level)
    {
        activePlantLevel = (int)level;
    }

    [UsedImplicitly]
    public void SetUrbanLevel(float level)
    {
        activeUrbanLevel = (int) level;
    }

    [UsedImplicitly]
    public void ShowUI(bool visible)
    {
        hexGrid.ShowUI(visible);
    }

    void Awake()
    {
        SelectColor(0);
    }

    void EditCell(HexCell cell)
    {
        if (cell) {
            if (applyColor) {
                cell.Color = activeColor;
            }

            if (applyElevation) {
                cell.Elevation = this.activeElevation;
            }

            if (applyWaterLevel) {
                cell.WaterLevel = activeWaterLevel;
            }

            if (applyUrbanLevel) {
                cell.UrbanLevel = activeUrbanLevel;
            }
            if (applyFarmLevel) {
                cell.FarmLevel = activeFarmLevel;
            }
            if (applyPlantLevel) {
                cell.PlantLevel = activePlantLevel;
            }

            if (riverMode == OptionalToggle.No) {
                cell.RemoveRiver();
            }

            if (roadMode == OptionalToggle.No) {
                cell.RemoveRoads();
            }

            if (isDrag) {
                HexCell otherCell = cell.GetNeighbor(dragDirection.Opposite());
                if (otherCell) {
                    if (riverMode == OptionalToggle.Yes) {
                        otherCell.SetOutgoingRiver(dragDirection);
                    }

                    if (roadMode == OptionalToggle.Yes) {
                        otherCell.AddRoad(dragDirection);
                    }
                }
            }
        }
    }

    void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
            for (int x = centerX    - r; x         <= centerX + brushSize; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
            for (int x = centerX    - brushSize; x <= centerX + r; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    void HandleInput()
    {
        Ray        inputRay = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(inputRay, out hit)) {
            HexCell currentCell = hexGrid.GetCell(hit.point);
            if (previousCell && currentCell != previousCell) {
                ValidateDrag(currentCell);
            } else {
                isDrag = false;
            }

            EditCells(currentCell);
            previousCell = currentCell;
        }
    }

    void Update()
    {
        if (Input.GetMouseButton(0) && !EventSystem.current.IsPointerOverGameObject()) {
            HandleInput();
        } else {
            previousCell = null;
        }
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++) {
            if (previousCell.GetNeighbor(dragDirection)     == currentCell) {
                isDrag = true;
                return;
            }
        }

        isDrag = false;
    }
}