using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class OriginalTerrainHeight : MonoBehaviour
{
    public Terrain terrain;

    private const string FileName = "terrain_heights.json";

    [ContextMenu("SaveHeightsToJson")]
    public void SaveHeightsToBinary()
    {
        if (terrain == null) return;

        var terrainData = terrain.terrainData;
        var heights = terrainData.GetHeights(0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);

        var filePath = Path.Combine(Application.dataPath, FileName);

        // Использование BinaryWriter для записи в бинарный файл
        using (var stream = new FileStream(filePath, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            int resolution = terrainData.heightmapResolution;
            writer.Write(resolution);  // Сохраняем разрешение карты высот
            foreach (var height in heights)
            {
                writer.Write(height);  // Записываем каждое значение высоты
            }
        }

        Debug.Log($"Heights saved to {filePath}");
    }

    public float[,] LoadHeightsFromBinary()
    {
        var filePath = Path.Combine(Application.dataPath, FileName);
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return null;  // Возвращаем null, если файл не найден
        }

        // Использование BinaryReader для чтения бинарного файла
        using (var stream = new FileStream(filePath, FileMode.Open))
        using (var reader = new BinaryReader(stream))
        {
            int resolution = reader.ReadInt32();  // Читаем разрешение карты высот
            if (terrain.terrainData.heightmapResolution != resolution)
            {
                Debug.LogError("Resolution mismatch between terrain and loaded data.");
                return null;
            }

            var heights = new float[resolution, resolution];
            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    heights[i, j] = reader.ReadSingle();  // Читаем каждое значение высоты
                }
            }

            return heights;
        }
    }

    public void SaveRiversToBinary(Dictionary<int, River> rivers)
    {
        var filePath = Path.Combine(Application.dataPath, "rivers.dat");

        using (var stream = new FileStream(filePath, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(rivers.Count);  // Сохраняем количество рек

            foreach (var riverPair in rivers)
            {
                var river = riverPair.Value;

                writer.Write(riverPair.Key);  // Сохраняем ключ (ID реки)

                // Сохраняем все поля объекта River
                writer.Write(river.posRiverX);
                writer.Write(river.posRiverY);
                writer.Write(river.riverWidth);
                writer.Write(river.riverLength);
                writer.Write(river.riverHeight);

                writer.Write(river.noiseScale);
                writer.Write(river.octaves);
                writer.Write(river.persistance);
                writer.Write(river.lacunarity);
                writer.Write(river.sizeRiver);

                // Сохраняем AnimationCurve как массив ключевых точек
                WriteAnimationCurve(writer, river.riverCurveByLength);
                WriteAnimationCurve(writer, river.riverCurve);

                // Сохраняем массив Vector2
                writer.Write(river.heightsTerrainPos.Length);
                foreach (var pos in river.heightsTerrainPos)
                {
                    writer.Write(pos.x);
                    writer.Write(pos.y);
                }

                writer.Write(river.riverLineDown.Length);
                foreach (var pos in river.riverLineDown)
                {
                    writer.Write(pos.x);
                    writer.Write(pos.y);
                }

                writer.Write(river.riverLineTop.Length);
                foreach (var pos in river.riverLineTop)
                {
                    writer.Write(pos.x);
                    writer.Write(pos.y);
                }

                // Сохраняем GameObject (если нужно только имя объекта)
                writer.Write(river.meshObject != null ? river.meshObject.name : string.Empty);
            }
        }

        Debug.Log($"Rivers saved to {filePath}");
    }

    public Dictionary<int, River> LoadRiversFromBinary()
    {
        var filePath = Path.Combine(Application.dataPath, "rivers.dat");
        if (!File.Exists(filePath))
        {
            Debug.LogError($"File not found: {filePath}");
            return null;
        }

        var rivers = new Dictionary<int, River>();

        using (var stream = new FileStream(filePath, FileMode.Open))
        using (var reader = new BinaryReader(stream))
        {
            int riversCount = reader.ReadInt32();  // Читаем количество рек

            for (int i = 0; i < riversCount; i++)
            {
                int riverId = reader.ReadInt32();  // Читаем ID реки
                Debug.Log($"Loading river with ID: {riverId}");

                River river = new River
                {
                    posRiverX = reader.ReadInt32(),
                    posRiverY = reader.ReadInt32(),
                    riverWidth = reader.ReadInt32(),
                    riverLength = reader.ReadInt32(),
                    riverHeight = reader.ReadSingle(),

                    noiseScale = reader.ReadSingle(),
                    octaves = reader.ReadInt32(),
                    persistance = reader.ReadSingle(),
                    lacunarity = reader.ReadSingle(),
                    sizeRiver = reader.ReadSingle(),

                    riverCurveByLength = ReadAnimationCurve(reader),
                    riverCurve = ReadAnimationCurve(reader)
                };

                int heightsCount = reader.ReadInt32();
                river.heightsTerrainPos = new Vector2[heightsCount];
                for (int j = 0; j < heightsCount; j++)
                {
                    river.heightsTerrainPos[j] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }

                int heightsCount1 = reader.ReadInt32();
                river.riverLineDown = new Vector2[heightsCount1];
                for (int j = 0; j < heightsCount1; j++)
                {
                    river.riverLineDown[j] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }

                int heightsCount2 = reader.ReadInt32();
                river.riverLineTop = new Vector2[heightsCount2];
                for (int j = 0; j < heightsCount2; j++)
                {
                    river.riverLineTop[j] = new Vector2(reader.ReadSingle(), reader.ReadSingle());
                }

                // Загружаем GameObject по имени (предположительно нужно искать объект по имени)
                string meshObjectName = reader.ReadString();
                river.meshObject = GameObject.Find(meshObjectName);

                // Добавляем реку в словарь, проверяем, что ключ уникален
                if (!rivers.ContainsKey(riverId))
                {
                    rivers[riverId] = river;
                }
                else
                {
                    Debug.LogWarning($"River with ID {riverId} already exists in dictionary. Skipping.");
                }
            }
        }

        Debug.Log("Rivers loaded.");
        return rivers;
    }

    private void WriteAnimationCurve(BinaryWriter writer, AnimationCurve curve)
    {
        writer.Write(curve.length);
        foreach (var key in curve.keys)
        {
            writer.Write(key.time);
            writer.Write(key.value);
        }
    }

    private AnimationCurve ReadAnimationCurve(BinaryReader reader)
    {
        int length = reader.ReadInt32();
        AnimationCurve curve = new AnimationCurve();
        for (int i = 0; i < length; i++)
        {
            float time = reader.ReadSingle();
            float value = reader.ReadSingle();
            curve.AddKey(time, value);
        }
        return curve;
    }
}



