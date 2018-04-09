using System;
using System.IO;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;

public class SaveLoadMenu : MonoBehaviour
{
    private const int mapFileVersion = 3;

    private bool saveMode;
    public  HexGrid    hexGrid;
    public  Text       menuLabel, actionButtonLabel;
    public  InputField nameInput;

    public RectTransform listContent;
    public SaveLoadItem  itemPrefab;
    string GetSelectedPath()
    {
        string mapName = nameInput.text;

        if (mapName.Length == 0) {
            return null;
        }

        return Path.Combine(Application.persistentDataPath, mapName + ".map");
    }

    private void Save(string path)
    {
        using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.Create))) {
            writer.Write(mapFileVersion);
            hexGrid.Save(writer);
        }
    }

    private void Load(string path)
    {
        StopAllCoroutines();
        if (!File.Exists(path)) {
            Debug.LogError("File does not exist " + path);

            return;
        }

        using (BinaryReader reader = new BinaryReader(File.OpenRead(path))) {
            int header = reader.ReadInt32();

            if (header <= mapFileVersion) {
                hexGrid.Load(reader, header);
                HexMapCamera.ValidatePosition();
            } else {
                Debug.LogWarning("Unknown map format " + header);
            }
        }
    }

    void FillList()
    {
        for (int i = 0; i < listContent.childCount; i++) {
            Destroy(listContent.GetChild(i).gameObject);
        }

        string[] paths = Directory.GetFiles(Application.persistentDataPath, "*.map");
        Array.Sort(paths);

        foreach (var p in paths) {
            SaveLoadItem item = Instantiate(itemPrefab);
            item.menu    = this;
            item.MapName = Path.GetFileNameWithoutExtension(p);
            item.transform.SetParent(listContent, false);
        }
    }

    [UsedImplicitly]
    public void Action()
    {
        string path = GetSelectedPath();

        if (path == null) {
            return;
        }

        if (saveMode) {
            Save(path);
        } else {
            Load(path);
        }

        Close();
    }

    [UsedImplicitly]
    public void Open(bool saveMode)
    {
        this.saveMode = saveMode;

        if (saveMode) {
            menuLabel.text         = "Save Map";
            actionButtonLabel.text = "Save";
        } else {
            menuLabel.text         = "Load Map";
            actionButtonLabel.text = "Load";
        }

        FillList();
        gameObject.SetActive(true);
        HexMapCamera.Locked = true;
    }

    [UsedImplicitly]
    public void Close()
    {
        gameObject.SetActive(false);
        HexMapCamera.Locked = false;
    }

    [UsedImplicitly]
    public void Delete()
    {
        string path = GetSelectedPath();

        if (path == null) {
            return;
        }

        if (File.Exists(path)) {
            File.Delete(path);
        }

        nameInput.text = "";
        FillList();
    }

    [UsedImplicitly]
    public void SelectItem(string name)
    {
        nameInput.text = name;
    }
}