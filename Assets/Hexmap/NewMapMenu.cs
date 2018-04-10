using JetBrains.Annotations;
using UnityEngine;

public class NewMapMenu : MonoBehaviour
{
    public HexGrid         hexGrid;
    public HexMapGenerator mapGenerator;

    bool generateMaps = true;

    private void CreateMap(int x, int z)
    {
        if (generateMaps) {
            mapGenerator.GenerateMap(x, z);
        } else {
            hexGrid.CreateMap(x, z);
        }

        HexMapCamera.ValidatePosition();
        Close();
    }

    [UsedImplicitly]
    public void Open()
    {
        this.gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    [UsedImplicitly]
    public void Close()
    {
        this.gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    [UsedImplicitly]
    public void CreateSmallMap()
    {
        CreateMap(20, 15);
    }

    [UsedImplicitly]
    public void CreateMediumMap()
    {
        CreateMap(40, 30);
    }

    [UsedImplicitly]
    public void CreateLargeMap()
    {
        CreateMap(80, 60);
    }

    [UsedImplicitly]
    public void ToggleMapGeneration(bool toggle)
    {
        generateMaps = toggle;
    }
}
