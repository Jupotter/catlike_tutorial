using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.EventSystems;

public class HexGameUI : MonoBehaviour
{
    public  HexGrid grid;
    private HexUnit selectedUnit;
    private HexCell currentCell;

    private void DoMove()
    {
        if (grid.HasPath) {
            this.selectedUnit.Travel(this.grid.GetPath());
            grid.ClearPath();
        }
    }

    private void DoPathfinding()
    {
        if (UpdateCurrentCell()) {
            if (currentCell && selectedUnit.IsValidDestination(currentCell)) {
                grid.FindPath(selectedUnit.Location, currentCell, selectedUnit);
            } else {
                grid.ClearPath();
            }
        }
    }

    private void DoSelection()
    {
        UpdateCurrentCell();

        if (this.currentCell) {
            this.selectedUnit = this.currentCell.Unit;
        }
    }

    private bool UpdateCurrentCell()
    {
        var cell = this.grid.GetCell(Camera.main.ScreenPointToRay(Input.mousePosition));

        if (cell != this.currentCell) {
            this.currentCell = cell;

            return true;
        }

        return false;
    }

    private void Update()
    {
        if (!EventSystem.current.IsPointerOverGameObject()) {
            if (Input.GetMouseButtonDown(0)) {
                DoSelection();
            } else if (this.selectedUnit) {
                if (Input.GetMouseButtonDown(1)) {
                    DoMove();
                } else {
                    DoPathfinding();
                }
            }
        }
    }

    [UsedImplicitly]
    public void SetEditMode(bool toggle)
    {
        this.enabled = !toggle;
        this.grid.ShowUI(!toggle);

        if (toggle) {
            Shader.EnableKeyword("HEX_MAP_EDIT_MODE");
        } else {
            Shader.DisableKeyword("HEX_MAP_EDIT_MODE");
        }
    }
}
