using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LineRiverGenerator : MonoBehaviour
{
    public static Vector2[] riverVertices(ref Vector2[] riverLineTop,
            ref Vector2[] riverLineDown, float sizeRiver, int posRiverX,
            int posRiverY, int riverWidth, int riverLength, float noiseScale,
            int octaves, float persistence, float lacunarity,
            AnimationCurve riverCurveBylength, AnimationCurve riverCurve)
    {
        // Объявляем массив точек реки
        Vector2[] riverVertices = new Vector2[riverLength * riverWidth];

        // Проверяем значение noiseScale, чтобы избежать деления на 0
        noiseScale = Mathf.Max(noiseScale, 0.0001f);

        // Подготовка для генерации шума
        float[] noiseMap = new float[riverLength];
        float maxNoiseHeight = float.MinValue;
        float minNoiseHeight = float.MaxValue;

        // Генерация карты шума
        for (int x = 0; x < riverLength; x++)
        {
            float amplitude = 1f;
            float frequency = 1f;
            float noiseHeight = 0f;

            for (int i = 0; i < octaves; i++)
            {
                float sampleX = x / noiseScale * frequency;
                noiseHeight += Mathf.PerlinNoise(sampleX, 0f) * amplitude;

                amplitude *= persistence;
                frequency *= lacunarity;
            }

            // Обновляем минимальные и максимальные значения шума
            maxNoiseHeight = Mathf.Max(maxNoiseHeight, noiseHeight);
            minNoiseHeight = Mathf.Min(minNoiseHeight, noiseHeight);

            noiseMap[x] = noiseHeight;
        }

        // Генерация вершин реки
        for (int x = 0; x < riverLength; x++)
        {
            float curveCof = riverCurveBylength.Evaluate((x+1) / (float)(riverLength + 1)) * sizeRiver;
            
            float normalizedHeight = Mathf.InverseLerp(minNoiseHeight, maxNoiseHeight, riverCurve.Evaluate(noiseMap[x]));
            int riverCenter = Mathf.RoundToInt(normalizedHeight * sizeRiver + posRiverY + curveCof);

            int startY = riverCenter - riverWidth;
            
            int baseIndex = x * riverWidth;

            for (int y = startY, i = 0; i < riverWidth; y++, i++)
            {
                if (y == startY) { riverLineDown[x] = new Vector2(x + posRiverX, y);  }
                if (i == riverWidth - 1) { riverLineTop[x] = new Vector2(x + posRiverX, y); }
                
                 
                // Проверяем, чтобы индекс не выходил за границы массива
                if (baseIndex + i < riverVertices.Length)
                {
                    riverVertices[baseIndex + i] = new Vector2(x + posRiverX, y);
                }
                else
                {
                    Debug.LogWarning($"Индекс {baseIndex + i} выходит за границы массива.");
                }
            }
        }

        return riverVertices;
    }
}
