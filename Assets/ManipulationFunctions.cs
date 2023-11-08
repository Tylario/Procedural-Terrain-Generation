using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManipulationFunctions : MonoBehaviour
{
    public GameObject Camera;
    public TerrainScript Terrain;
    public bool generatorPerlinNoise = true;
    public float frequencyBase = 3.5f;
    public Vector2 offset;
    public int seed = 0;
    private Vector2 seedOffset;
    public List<float> octaveIntensities = new List<float> { 1.4f, 0.5f, 0.25f, 0.125f, 0.0625f, 0.03125f, 0.0155f };
    public bool flattenTerrain = true;
    public float flattenEdgeEffect = 1.0f;
    public bool normalizeHeights = true;
    public float minHeight = 0f;
    public float maxHeight = 1f;
    public bool erosion = true;
    public float blurStrength = 0.1f; 
    public int blurIterations = 5; 
    public GameObject light;


    private void Start()
    {
        seedOffset = GenerateSeedOffset();
    }

    public void GeneratePerlinNoiseHeight(ref float[,] grid)
    {
        if (!generatorPerlinNoise) return;

        for (int x = 0; x < Terrain.terrainWidth; x++)
        {
            for (int y = 0; y < Terrain.terrainHeight; y++)
            {
                float xCoord = (x / (float)Terrain.terrainWidth * frequencyBase + offset.x + seedOffset.x) % Terrain.terrainWidth;
                float yCoord = (y / (float)Terrain.terrainHeight * frequencyBase + offset.y + seedOffset.y) % Terrain.terrainWidth;

                float noiseHeight = 0;

                for (int octave = 0; octave < octaveIntensities.Count; octave++)
                {
                    float amplitude = octaveIntensities[octave];
                    float sampleX = xCoord * Mathf.Pow(2, octave);
                    float sampleY = yCoord * Mathf.Pow(2, octave);
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    noiseHeight += perlinValue * amplitude;
                }

                grid[x, y] = (noiseHeight + 1) / 2.0f; // Normalize to 0-1
            }
        }
    }

    public void FlattenTerrainEdges(ref float[,] heightGrid)
    {
        if (!flattenTerrain) return;

        float minHeight = FindMinHeight(heightGrid);

        for (int x = 0; x < Terrain.terrainWidth; x++)
        {
            for (int y = 0; y < Terrain.terrainHeight; y++)
            {
                float centerX = Terrain.terrainWidth / 2.0f;
                float centerY = Terrain.terrainHeight / 2.0f;
                float distanceToCenter = Vector2.Distance(new Vector2(x, y), new Vector2(centerX, centerY));
                float maxDistanceToCorner = Vector2.Distance(new Vector2(0, 0), new Vector2(centerX, centerY));

                float flattenAmount = Mathf.Clamp01(distanceToCenter / maxDistanceToCorner);
                heightGrid[x, y] = Mathf.Lerp(heightGrid[x, y], minHeight, flattenAmount * flattenEdgeEffect);
            }
        }
    }

    public void NormalizeHeight(ref float[,] heightGrid)
    {
        if (!normalizeHeights) return;

        // Find current minimum and maximum heights in the grid.
        float currentMinHeight = float.MaxValue;
        float currentMaxHeight = float.MinValue;
        for (int x = 0; x < Terrain.terrainWidth; x++)
        {
            for (int y = 0; y < Terrain.terrainHeight; y++)
            {
                if (heightGrid[x, y] < currentMinHeight)
                {
                    currentMinHeight = heightGrid[x, y];
                }
                if (heightGrid[x, y] > currentMaxHeight)
                {
                    currentMaxHeight = heightGrid[x, y];
                }
            }
        }

        // Check if all the values are the same.
        if (currentMinHeight == currentMaxHeight)
        {
            // If all values are the same, just set them to minHeight as normalization is not needed.
            for (int x = 0; x < Terrain.terrainWidth; x++)
            {
                for (int y = 0; y < Terrain.terrainHeight; y++)
                {
                    heightGrid[x, y] = minHeight;
                }
            }
        }
        else
        {
            // Calculate the scale and offset required to normalize the heights.
            float scale = (maxHeight - minHeight) / (currentMaxHeight - currentMinHeight);
            float offset = minHeight - currentMinHeight * scale;

            // Apply the scale and offset to each height in the grid.
            for (int x = 0; x < Terrain.terrainWidth; x++)
            {
                for (int y = 0; y < Terrain.terrainHeight; y++)
                {
                    heightGrid[x, y] = heightGrid[x, y] * scale + offset;
                }
            }
        }
    }

    private float FindMinHeight(float[,] heightGrid)
    {
        float minHeightValue = float.MaxValue;
        foreach (float height in heightGrid)
        {
            if (height < minHeightValue)
            {
                minHeightValue = height;
            }
        }
        return minHeightValue;
    }

    private Vector2 GenerateSeedOffset()
    {
        System.Random rand = new System.Random(seed);
        return new Vector2((float)rand.NextDouble() * 10000, (float)rand.NextDouble() * 10000);
    }

    public void ApplyErosion(ref float[,] heightGrid)
    {
        if (!erosion)
        {
            return;
        }

        int blurRadius = 3;

        for (int iteration = 0; iteration < blurIterations; iteration++)
        {
            // Create a copy of the heightGrid to read from while writing to the original
            float[,] heightGridCopy = (float[,])heightGrid.Clone();

            for (int x = 0; x < Terrain.terrainWidth; x++)
            {
                for (int y = 0; y < Terrain.terrainHeight; y++)
                {
                    Erode(ref heightGrid, heightGridCopy, x, y, blurRadius, blurStrength);
                }
            }
        }
    }

    private void Erode(ref float[,] heightGrid, float[,] heightGridCopy, int x, int y, int blurRadius, float mixFactor)
    {
        float totalHeight = 0;
        int count = 0;

        // Sum the heights of the neighboring points
        for (int i = -blurRadius; i <= blurRadius; i++)
        {
            for (int j = -blurRadius; j <= blurRadius; j++)
            {
                int currentX = Mathf.Clamp(x + i, 0, Terrain.terrainWidth - 1);
                int currentY = Mathf.Clamp(y + j, 0, Terrain.terrainHeight - 1);

                totalHeight += heightGridCopy[currentX, currentY];
                count++;
            }
        }

        // Calculate the average height
        float averageHeight = totalHeight / count;

        // Apply a mix of the average and the original height to the central point
        heightGrid[x, y] = Mathf.Lerp(heightGrid[x, y], averageHeight, mixFactor);
    }
}
