using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class TerrainScript : MonoBehaviour
{
    public ManipulationFunctions Manipulator;
    public int terrainWidth = 256;
    public int terrainHeight = 256;

    private float[,] heightGrid;
    private Mesh terrainMesh;

    private void GenerateHeightGrid()
    {
        heightGrid = new float[terrainWidth, terrainHeight];
        Manipulator.GeneratePerlinNoiseHeight(ref heightGrid);
        Manipulator.FlattenTerrainEdges(ref heightGrid);
        Manipulator.NormalizeHeight(ref heightGrid);
        Manipulator.ApplyErosion(ref heightGrid);
    }

    void Start()
    {
        terrainMesh = new Mesh();
        GetComponent<MeshFilter>().mesh = terrainMesh;
        GenerateTerrain();
    }

    void Update()
    {
        GenerateTerrain();
    }

    void GenerateTerrain()
    {
        terrainMesh.Clear();

        GenerateHeightGrid();

        // Generate mesh data
        Vector3[] vertices = new Vector3[terrainWidth * terrainHeight];
        int[] triangles = new int[(terrainWidth - 1) * (terrainHeight - 1) * 6];
        Vector2[] uvs = new Vector2[vertices.Length];

        float widthOffset = terrainWidth / 2f; // Offset to center the terrain on X
        float heightOffset = terrainHeight / 2f; // Offset to center the terrain on Z

        for (int y = 0, i = 0; y < terrainHeight; y++)
        {
            for (int x = 0; x < terrainWidth; x++, i++)
            {
                float height = heightGrid[x, y];
                vertices[i] = new Vector3(x - widthOffset, height * 50, y - heightOffset); // Adjust for centering
                uvs[i] = new Vector2((float)x / terrainWidth, (float)y / terrainHeight);
            }
        }

        // Generate triangles
        int ti = 0, vi = 0;
        for (int y = 0; y < terrainHeight - 1; y++, vi++)
        {
            for (int x = 0; x < terrainWidth - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + terrainWidth;
                triangles[ti + 5] = vi + terrainWidth + 1;
            }
        }

        // Assign mesh data
        terrainMesh.vertices = vertices;
        terrainMesh.triangles = triangles;
        terrainMesh.uv = uvs;

        terrainMesh.RecalculateNormals(); // Recalculate normals for proper lighting
    }
}
