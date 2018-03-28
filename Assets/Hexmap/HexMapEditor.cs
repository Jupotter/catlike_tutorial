using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexMapEditor : MonoBehaviour
{
    public HexGrid  hexGrid;
    public Material terrainMaterial;

    HexDirection    dragDirection;
    bool            isDrag;
    private HexCell previousCell, searchFromCell, searchToCell;

    private int  activeTerrainTypeIndex;
    private int  activeElevation;
    private int  activeWaterLevel;
    private int  activeUrbanLevel, activeFarmLevel, activePlantLevel, activeSpecialIndex;
    private bool applyElevation  = true;
    private bool applyWaterLevel = true;
    private bool applyUrbanLevel, applyFarmLevel, applyPlantLevel, applySpecialIndex;
    private int  brushSize;

    private OptionalToggle riverMode, roadMode, walledMode;

    private bool editMode;

    enum OptionalToggle
    {
        [UsedImplicitly] Ignore,
        Yes,
        No
    }

    [UsedImplicitly]
    public void SetTerrainTypeIndex(int index)
    {
        activeTerrainTypeIndex = index;
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
    public void SetWalledMode(int mode)
    {
        walledMode = (OptionalToggle) mode;
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

    [UsedImplicitly]
    public void SetApplyFarmLevel(bool toggle)
    {
        applyFarmLevel = toggle;
    }

    [UsedImplicitly]
    public void SetFarmLevel(float level)
    {
        activeFarmLevel = (int) level;
    }

    [UsedImplicitly]
    public void SetApplyPlantLevel(bool toggle)
    {
        applyPlantLevel = toggle;
    }

    [UsedImplicitly]
    public void SetPlantLevel(float level)
    {
        activePlantLevel = (int) level;
    }

    [UsedImplicitly]
    public void SetUrbanLevel(float level)
    {
        activeUrbanLevel = (int) level;
    }

    [UsedImplicitly]
    public void SetApplySpecialIndex(bool toggle)
    {
        applySpecialIndex = toggle;
    }

    [UsedImplicitly]
    public void SetSpecialIndex(float index)
    {
        activeSpecialIndex = (int) index;
    }

    [UsedImplicitly]
    public void SetEditMode(bool toggle)
    {
        editMode = toggle;
        hexGrid.ShowUI(!toggle);
    }

    [UsedImplicitly]
    public void ShowGrid(bool visible)
    {
        if (visible) {
            terrainMaterial.EnableKeyword("GRID_ON");
        } else {
            terrainMaterial.DisableKeyword("GRID_ON");
        }
    }

    void EditCell(HexCell cell)
    {
        if (!cell) {
            return;
        }

        if (this.activeTerrainTypeIndex >= 0) {
            cell.TerrainTypeIndex = this.activeTerrainTypeIndex;
        }

        if (this.applyElevation) {
            cell.Elevation = this.activeElevation;
        }

        if (this.applyWaterLevel) {
            cell.WaterLevel = this.activeWaterLevel;
        }

        if (this.applyUrbanLevel) {
            cell.UrbanLevel = this.activeUrbanLevel;
        }

        if (this.applyFarmLevel) {
            cell.FarmLevel = this.activeFarmLevel;
        }

        if (this.applyPlantLevel) {
            cell.PlantLevel = this.activePlantLevel;
        }

        if (this.applySpecialIndex) {
            cell.SpecialIndex = this.activeSpecialIndex;
        }

        if (this.riverMode == OptionalToggle.No) {
            cell.RemoveRiver();
        }

        if (this.roadMode == OptionalToggle.No) {
            cell.RemoveRoads();
        }

        if (this.walledMode != OptionalToggle.Ignore) {
            cell.Walled = this.walledMode == OptionalToggle.Yes;
        }

        if (this.isDrag) {
            HexCell otherCell = cell.GetNeighbor(this.dragDirection.Opposite());

            if (!otherCell) {
                return;
            }

            if (this.riverMode == OptionalToggle.Yes) {
                otherCell.SetOutgoingRiver(this.dragDirection);
            }

            if (this.roadMode == OptionalToggle.Yes) {
                otherCell.AddRoad(this.dragDirection);
            }
        }
    }

    private void EditCells(HexCell center)
    {
        int centerX = center.coordinates.X;
        int centerZ = center.coordinates.Z;

        for (int r = 0, z = centerZ - brushSize; z <= centerZ; z++, r++) {
            for (int x = centerX - r; x <= centerX + brushSize; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }

        for (int r = 0, z = centerZ + brushSize; z > centerZ; z--, r++) {
            for (int x = centerX - brushSize; x <= centerX + r; x++) {
                EditCell(hexGrid.GetCell(new HexCoordinates(x, z)));
            }
        }
    }

    void Awake()
    {
        terrainMaterial.DisableKeyword("GRID_ON");
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

            if (editMode) {
                EditCells(currentCell);
            } else if (Input.GetKey(KeyCode.LeftShift) && searchToCell != currentCell) {
                if (searchFromCell != currentCell) {
                    if (searchFromCell) {
                        searchFromCell.DisableHighlight();
                    }

                    searchFromCell = currentCell;
                        searchFromCell.EnableHighlight(Color.blue);

                        if (searchToCell) {
                            hexGrid.FindPath(searchFromCell, searchToCell, 24);
                        }
                    }
            } else if (searchFromCell && searchFromCell != currentCell) {
                if (searchToCell != currentCell) {
                    searchToCell = currentCell;
                    hexGrid.FindPath(searchFromCell, searchToCell, 24);
                }
            }

            previousCell = currentCell;
        }
    }

    void Update()
    {
        if (Input.GetMouseButton(0)) {
            if (!EventSystem.current.IsPointerOverGameObject()) {
                HandleInput();
            } else {
                previousCell = null;
            }
        } else {
            previousCell = null;
        }
    }

    private void ValidateDrag(HexCell currentCell)
    {
        for (dragDirection = HexDirection.NE; dragDirection <= HexDirection.NW; dragDirection++) {
            if (previousCell.GetNeighbor(dragDirection) == currentCell) {
                isDrag = true;

                return;
            }
        }

        isDrag = false;
    }

    [UsedImplicitly]
    public void Save()
    {
        string path = Path.Combine(Application.persistentDataPath, "test.map");

        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
            writer.Write(1);
            hexGrid.Save(writer);
        }
    }

    [UsedImplicitly]
    public void Load()
    {
        string path = Path.Combine(Application.persistentDataPath, "test.map");

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            int header = reader.ReadInt32();

            if (header <= 1) {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            } else {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }
}
