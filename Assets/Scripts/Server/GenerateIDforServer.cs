/*using UnityEngine;
using UnityEditor;
using System.IO;

[ExecuteInEditMode]
public class GenerateIDforServer : MonoBehaviour
{
    private string savePath;
    private int lastId = 0;

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            GenerateIDs();
        }
    }

    private void GenerateIDs()
    {
        savePath = Application.dataPath + "/Editor/last_id.txt";
        LoadLastId();

        foreach (Transform child in transform)
        {
            ServerId sr = child.GetComponent<ServerId>();

            // Якщо немає компонента або його serverId == 0 (або -1 як початкове значення), то додаємо
            if (sr == null)
            {
                sr = Undo.AddComponent<ServerId>(child.gameObject);
                sr.serverId = lastId++;
                EditorUtility.SetDirty(sr);
                Debug.Log($"[NEW] {child.name} → ID = {sr.serverId}");
            }
            else if (sr.serverId == 0)
            {
                sr.serverId = lastId++;
                EditorUtility.SetDirty(sr);
                Debug.Log($"[SET] {child.name} → ID = {sr.serverId}");
            }
            else
            {
                Debug.Log($"[SKIP] {child.name} вже має ID = {sr.serverId}");
            }
        }

        SaveLastId();
        AssetDatabase.SaveAssets();
    }

    private void LoadLastId()
    {
        if (File.Exists(savePath))
        {
            string content = File.ReadAllText(savePath);
            int.TryParse(content, out lastId);
        }
        else
        {
            lastId = 1; // або 0 — як хочеш починати ID
        }
    }

    private void SaveLastId()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(savePath));
        File.WriteAllText(savePath, lastId.ToString());
    }
}
*/