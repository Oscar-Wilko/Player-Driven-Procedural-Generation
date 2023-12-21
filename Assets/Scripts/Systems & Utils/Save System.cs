using UnityEngine;
using System.IO;

[System.Serializable]
public class SavedImage
{
    public MapInfo info;
    public SavedImage(MapInfo n_info)
    {
        info = n_info;
    }
}

public static class SaveSystem
{
    /// <summary>
    /// Save image information with file name
    /// </summary>
    /// <param name="info">SavedImage of image information</param>
    /// <param name="file_name">String of filename to save with</param>
    public static void SaveImageInfo(SavedImage info, string file_name)
    {
        FolderCheck();
        string saved_data = JsonUtility.ToJson(info, true);
        File.WriteAllText(GetSaveFileLocation(file_name), saved_data);
    }

    /// <summary>
    /// Save image information with file name
    /// </summary>
    /// <param name="info">MapInfo of image information</param>
    /// <param name="file_name">String of filename to save with</param>
    public static void SaveImageInfo(MapInfo info, string file_name)
    {
        SaveImageInfo(new SavedImage(info), file_name);
    }

    /// <summary>
    /// Load image information from file name
    /// </summary>
    /// <param name="file_name">String of file name</param>
    /// <returns>SavedImage of exported image information</returns>
    public static SavedImage LoadImageInfo(string file_name)
    {
        FolderCheck();
        if (File.Exists(GetSaveFileLocation(file_name)))
        {
            string loaded_data = File.ReadAllText(GetSaveFileLocation(file_name));
            SavedImage data = JsonUtility.FromJson<SavedImage>(loaded_data);
            if (data != null)
                return data;
        }
        return null;
    }

    /// <summary>
    /// Get File Location Of Saved Information
    /// </summary>
    /// <returns>String of file location</returns>
    private static string GetSaveFileLocation(string file_name)
    {
        return GetSavedDataLocation() + "/" + file_name + ".json";
    }

    /// <summary>
    /// Get File Location Of SavedData Folder
    /// </summary>
    /// <returns>String of file location</returns>
    private static string GetSavedDataLocation()
    {
        return "Assets/SavedData";
    }

    /// <summary>
    /// Checks if save folder exists, if it doesn't, then it creates it
    /// </summary>
    private static void FolderCheck()
    {
        if (!Directory.Exists(GetSavedDataLocation()))
            Directory.CreateDirectory(GetSavedDataLocation());
    }
}