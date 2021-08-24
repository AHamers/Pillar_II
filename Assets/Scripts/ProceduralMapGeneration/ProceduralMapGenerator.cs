using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CubeCornerState
{
    INACTIVE = 0,
    ACTIVE = 1,
    RENDERED = 3
}

public class CubeCorner
{
    public CubeCornerState state;
    public float perlinValue;

    public CubeCorner(float perlinValue)
    {
        this.perlinValue = perlinValue;
    }
}

[System.Serializable]
public class PerlinNoise
{
    public string name;
    public bool enabled;
    public float overallDensity;
    public AnimationCurve densityOverHeight;
    public float zoom;
    public Vector3 offset;

    public float sampleNoise(Vector3 coordinates, float maxHeight)
    {
        if (!enabled)
            return 0.0f;

        float toReturn = PerlinNoise3D( zoom * coordinates.x + offset.x,
                                        zoom * coordinates.y + offset.y,
                                        zoom * coordinates.z + offset.z);
        toReturn *= densityOverHeight.Evaluate(coordinates.y / maxHeight);
        toReturn *= overallDensity;

        return toReturn;
    }

    public void randomizeOffset()
    {
        offset = new Vector3(Random.value * 100000, Random.value * 100000, Random.value * 100000);
    }

    public static float PerlinNoise3D(float x, float y, float z)
    {
        float xy = Mathf.PerlinNoise(x, y);
        float yz = Mathf.PerlinNoise(y, z);
        float xz = Mathf.PerlinNoise(x, z);
        float yx = Mathf.PerlinNoise(y, x);
        float zy = Mathf.PerlinNoise(z, y);
        float zx = Mathf.PerlinNoise(z, x);

        float sum = xy + yz + xz + yx + zy + zx;
        return sum / 6.0f;
    }
}

[ExecuteInEditMode]
public class ProceduralMapGenerator : MonoBehaviour
{
    public bool generate;
    public bool displayCubeCornersMap;
    public bool reset;

    [Space(2)]
    [Header("Generation")]
    public float maxHeight;
    public float resolution;
    public float radius;
    public float minNoiseDensity;
    public bool randomizeNoiseOffsets;
    public List<PerlinNoise> noise;
    public bool interpolate;

    [Space(2)]
    [Header("Color")]
    public Gradient colorMap;

    private int[,] triangulationTable;
    private Mesh mesh;
    private CubeCorner[,,] activeCubeCornersMap;
    private List<Vector3Int> cubesToRender;

    private void Awake()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if(generate)
        {
            generate = false;
            generateMap();
        }
        if(displayCubeCornersMap)
        {
            displayCubeCornersMap = false;
            _displayCubeCornersMap();
        }
        if(reset)
        {
            reset = false;
            _reset();
        }
        if (randomizeNoiseOffsets)
        {
            randomizeNoiseOffsets = false;
            _randomizeNoiseOffsets();
        }
    }

    void generateMap()
    {
        generateCubesCornersMap();
        generateMesh();
    }

    void generateCubesCornersMap()
    {
        this.activeCubeCornersMap = new CubeCorner[Mathf.FloorToInt( 2 * radius / resolution), Mathf.FloorToInt(maxHeight / resolution), Mathf.FloorToInt(2 * radius / resolution)];
        this.cubesToRender = new List<Vector3Int>();

        for (int x = 0; x < activeCubeCornersMap.GetLength(0); x++)
        {
            for (int y = 0; y < activeCubeCornersMap.GetLength(1); y++)
            {
                for (int z = 0; z < activeCubeCornersMap.GetLength(2); z++)
                {
                    float noiseDensity = 0;
                    for(int i = 0; i < noise.Count; i++)
                    {
                        noiseDensity += noise[i].sampleNoise(cubesCornerMapIndexToWorldCoordinates(new Vector3Int(x, y, z)), maxHeight);
                    }

                    activeCubeCornersMap[x, y, z] = new CubeCorner(noiseDensity);

                    if(noiseDensity > minNoiseDensity)
                    {
                        activeCubeCornersMap[x, y, z].state = CubeCornerState.ACTIVE;
                        for (int _x = x - 1; _x <= x + 1; _x++)
                        {
                            for (int _y = y - 1; _y <= y + 1; _y++)
                            {
                                for (int _z = z - 1; _z <= z + 1; _z++)
                                {
                                    if (_x >= 0 && _x < activeCubeCornersMap.GetLength(0) - 1
                                     && _y >= 0 && _y < activeCubeCornersMap.GetLength(1) - 1
                                     && _z >= 0 && _z < activeCubeCornersMap.GetLength(2) - 1)
                                        cubesToRender.Add(new Vector3Int(_x, _y, _z));
                                }
                            }
                        }
                        cubesToRender.Add(new Vector3Int(x, y, z));
                    }
                    else
                    {
                        activeCubeCornersMap[x, y, z].state = CubeCornerState.INACTIVE;
                    }
                }
            }
        }
    }

    void generateMesh()
    {
        this.triangulationTable = generateTriangulationTable();
        this.mesh = new Mesh();
        this.mesh.Clear();

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Color32> colors = new List<Color32>();

        for (int i = 0; i < this.cubesToRender.Count; i++)
        {
            int x = cubesToRender[i].x;
            int y = cubesToRender[i].y;
            int z = cubesToRender[i].z;

            Vector3[] edgesRelativeToCornerOffset = getEdgesRelativeToCornerOffset(cubesToRender[i]);

            Vector3 worldCoordinates = cubesCornerMapIndexToWorldCoordinates(new Vector3Int(x, y, z));

            if (activeCubeCornersMap[x, y, z].state == CubeCornerState.RENDERED)
                continue;

            int cubeBitMask = 0;
            bool[] cubeActiveCorners = getCubeCorners(x, y, z);
            for (int j = 0; j < 7; j++)
            {
                if (cubeActiveCorners[j])
                    cubeBitMask = cubeBitMask | (1 << j);
            }
            if (cubeActiveCorners[7])
                cubeBitMask = cubeBitMask | (1 << 7);

            for (int j = 0; j < 15; j += 3)
            {
                if (this.triangulationTable[cubeBitMask, j] == -1)
                    break;
                vertices.Add(worldCoordinates + edgesRelativeToCornerOffset[triangulationTable[cubeBitMask, j]]);
                vertices.Add(worldCoordinates + edgesRelativeToCornerOffset[triangulationTable[cubeBitMask, j + 1]]);
                vertices.Add(worldCoordinates + edgesRelativeToCornerOffset[triangulationTable[cubeBitMask, j + 2]]);

                Vector3 averagePoint = (vertices[vertices.Count - 3] + vertices[vertices.Count - 2] + vertices[vertices.Count - 1]) / 3.0f;
                Color32 selectedColor = calculateTriangleColor(averagePoint);
                colors.Add(selectedColor);
                colors.Add(selectedColor);
                colors.Add(selectedColor);

                triangles.Add(vertices.Count - 3);
                triangles.Add(vertices.Count - 2);
                triangles.Add(vertices.Count - 1);
            }

            if (activeCubeCornersMap[x, y, z].state == CubeCornerState.ACTIVE)
                activeCubeCornersMap[x, y, z].state = CubeCornerState.RENDERED;
        }

        cleanAndApplyMesh(vertices, triangles, colors);
    }

    private void cleanAndApplyMesh(List<Vector3> vertices, List<int> triangles, List<Color32> colors)
    {

        this.mesh.Clear();
        this.mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        this.mesh.vertices = vertices.ToArray();
        this.mesh.triangles = triangles.ToArray();
        this.mesh.colors32 = colors.ToArray();
        this.mesh.Optimize();

        this.mesh.RecalculateNormals();
        this.mesh.RecalculateBounds();
        this.mesh.RecalculateTangents();

        DestroyImmediate(this.GetComponent<MeshCollider>());
        MeshCollider collider = this.gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = this.mesh;

        this.GetComponent<MeshFilter>().sharedMesh = this.mesh;
    }

    void _reset()
    {
        activeCubeCornersMap = null;
        mesh = null;
        this.GetComponent<MeshFilter>().mesh = null;
        DestroyImmediate(this.GetComponent<MeshCollider>());
        this.triangulationTable = null;
        cubesToRender = null;

        for(int i = this.transform.childCount - 1; i >= 0; i--)
            GameObject.DestroyImmediate(this.transform.GetChild(i).gameObject);
    }

    void _randomizeNoiseOffsets()
    {
        for (int i = 0; i < noise.Count; i++)
            noise[i].randomizeOffset();
    }

    void _displayCubeCornersMap()
    {
        if(activeCubeCornersMap == null)
        {
            Debug.LogWarning("No cornerMap found !");
            return;
        }

        for(int x = 0; x < activeCubeCornersMap.GetLength(0); x++)
        {
            for (int y = 0; y < activeCubeCornersMap.GetLength(1); y++)
            {
                for (int z = 0; z < activeCubeCornersMap.GetLength(2); z++)
                {
                    if (activeCubeCornersMap[x, y, z].state == CubeCornerState.ACTIVE || activeCubeCornersMap[x, y, z].state == CubeCornerState.RENDERED)
                    {
                        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        sphere.transform.position = cubesCornerMapIndexToWorldCoordinates(new Vector3Int(x, y, z));
                        sphere.transform.parent = this.transform;
                    }
                }
            }
        }
    }

    Vector3 cubesCornerMapIndexToWorldCoordinates(Vector3Int coordinates)
    {
        Vector3 toReturn = new Vector3((coordinates.x - activeCubeCornersMap.GetLength(0) / 2.0f) * resolution,
                                       (coordinates.y) * resolution,
                                       (coordinates.z - activeCubeCornersMap.GetLength(2) / 2.0f) * resolution);

        return toReturn;
    }

    private Vector3[] getEdgesRelativeToCornerOffset(Vector3Int cube)
    {
        Vector3[] toReturn = new Vector3[12];

        if (interpolate
             && cube.x >= 0 && cube.x < activeCubeCornersMap.GetLength(0) - 1
             && cube.y >= 0 && cube.y < activeCubeCornersMap.GetLength(1) - 1
             && cube.z >= 0 && cube.z < activeCubeCornersMap.GetLength(2) - 1)
        {
            toReturn[0] = new Vector3(  (minNoiseDensity - activeCubeCornersMap[cube.x, cube.y, cube.z].perlinValue) / (activeCubeCornersMap[cube.x + 1, cube.y, cube.z].perlinValue - activeCubeCornersMap[cube.x, cube.y, cube.z].perlinValue) * resolution, 0.0f, 0.0f);
            toReturn[1] = new Vector3(resolution, 0.0f, (minNoiseDensity - activeCubeCornersMap[cube.x + 1, cube.y, cube.z].perlinValue) / (activeCubeCornersMap[cube.x + 1, cube.y, cube.z + 1].perlinValue - activeCubeCornersMap[cube.x + 1, cube.y, cube.z].perlinValue) * resolution);
            toReturn[2] = new Vector3((minNoiseDensity - activeCubeCornersMap[cube.x, cube.y, cube.z + 1].perlinValue) / (activeCubeCornersMap[cube.x + 1, cube.y, cube.z + 1].perlinValue - activeCubeCornersMap[cube.x, cube.y, cube.z + 1].perlinValue) * resolution, 0.0f, resolution);
            toReturn[3] = new Vector3(0.0f, 0.0f, (minNoiseDensity - activeCubeCornersMap[cube.x, cube.y, cube.z].perlinValue) / (activeCubeCornersMap[cube.x, cube.y, cube.z + 1].perlinValue - activeCubeCornersMap[cube.x, cube.y, cube.z].perlinValue) * resolution);
            toReturn[4] = new Vector3((minNoiseDensity - activeCubeCornersMap[cube.x, cube.y + 1, cube.z].perlinValue) / (activeCubeCornersMap[cube.x + 1, cube.y + 1, cube.z].perlinValue - activeCubeCornersMap[cube.x, cube.y + 1, cube.z].perlinValue) * resolution, resolution, 0.0f);
            toReturn[5] = new Vector3(resolution, resolution, (minNoiseDensity - activeCubeCornersMap[cube.x + 1, cube.y + 1, cube.z].perlinValue) / (activeCubeCornersMap[cube.x + 1, cube.y + 1, cube.z + 1].perlinValue - activeCubeCornersMap[cube.x + 1, cube.y + 1, cube.z].perlinValue) * resolution);
            toReturn[6] = new Vector3((minNoiseDensity - activeCubeCornersMap[cube.x, cube.y + 1, cube.z + 1].perlinValue) / (activeCubeCornersMap[cube.x + 1, cube.y + 1, cube.z+1].perlinValue - activeCubeCornersMap[cube.x, cube.y+1, cube.z+1].perlinValue) * resolution, resolution, resolution);
            toReturn[7] = new Vector3(0.0f, resolution, (minNoiseDensity - activeCubeCornersMap[cube.x, cube.y + 1, cube.z].perlinValue) / (activeCubeCornersMap[cube.x, cube.y+1, cube.z + 1].perlinValue - activeCubeCornersMap[cube.x, cube.y+1, cube.z].perlinValue) * resolution);
            toReturn[8] = new Vector3(0.0f, (minNoiseDensity - activeCubeCornersMap[cube.x, cube.y, cube.z].perlinValue) / (activeCubeCornersMap[cube.x, cube.y + 1, cube.z].perlinValue - activeCubeCornersMap[cube.x, cube.y, cube.z].perlinValue) * resolution, 0.0f);
            toReturn[9] = new Vector3(resolution, (minNoiseDensity - activeCubeCornersMap[cube.x + 1, cube.y, cube.z].perlinValue) / (activeCubeCornersMap[cube.x+1, cube.y + 1, cube.z].perlinValue - activeCubeCornersMap[cube.x+1, cube.y, cube.z].perlinValue) * resolution, 0.0f);
            toReturn[10] = new Vector3(resolution, (minNoiseDensity - activeCubeCornersMap[cube.x + 1, cube.y, cube.z + 1].perlinValue) / (activeCubeCornersMap[cube.x+1, cube.y + 1, cube.z+1].perlinValue - activeCubeCornersMap[cube.x+1, cube.y, cube.z+1].perlinValue) * resolution, resolution);
            toReturn[11] = new Vector3(0.0f, (minNoiseDensity - activeCubeCornersMap[cube.x, cube.y, cube.z + 1].perlinValue) / (activeCubeCornersMap[cube.x, cube.y + 1, cube.z+1].perlinValue - activeCubeCornersMap[cube.x, cube.y, cube.z+1].perlinValue) * resolution, resolution);

            for(int i = 0; i < toReturn.Length; i++)
            {
                if (toReturn[i].x > resolution || toReturn[i].x < 0)
                    toReturn[i].x = resolution / 2.0f;
                if (toReturn[i].y > resolution || toReturn[i].y < 0)
                    toReturn[i].y = resolution / 2.0f;
                if (toReturn[i].z > resolution || toReturn[i].z < 0)
                    toReturn[i].z = resolution / 2.0f;
            }
        }
        else
        {
            toReturn[0] = new Vector3(resolution / 2.0f, 0.0f, 0.0f);
            toReturn[1] = new Vector3(resolution, 0.0f, resolution / 2.0f);
            toReturn[2] = new Vector3(resolution / 2.0f, 0.0f, resolution);
            toReturn[3] = new Vector3(0.0f, 0.0f, resolution / 2.0f);
            toReturn[4] = new Vector3(resolution / 2.0f, resolution, 0.0f);
            toReturn[5] = new Vector3(resolution, resolution, resolution / 2.0f);
            toReturn[6] = new Vector3(resolution / 2.0f, resolution, resolution);
            toReturn[7] = new Vector3(0.0f, resolution, resolution / 2.0f);
            toReturn[8] = new Vector3(0.0f, resolution / 2.0f, 0.0f);
            toReturn[9] = new Vector3(resolution, resolution / 2.0f, 0.0f);
            toReturn[10] = new Vector3(resolution, resolution / 2.0f, resolution);
            toReturn[11] = new Vector3(0.0f, resolution / 2.0f, resolution);
        }
        return toReturn;
    }

    private bool[] getCubeCorners(int x, int y, int z)
    {
        bool[] toReturn = new bool[8];
        if (x >= 0 && x < activeCubeCornersMap.GetLength(0) - 1
         && y >= 0 && y < activeCubeCornersMap.GetLength(1) - 1
         && z >= 0 && z < activeCubeCornersMap.GetLength(2) - 1)
        {
            toReturn[0] = activeCubeCornersMap[x + 0, y + 0, z + 0].state == CubeCornerState.RENDERED
                || activeCubeCornersMap[x + 0, y + 0, z + 0].state == CubeCornerState.ACTIVE;
            toReturn[1] = activeCubeCornersMap[x + 1, y + 0, z + 0].state == CubeCornerState.RENDERED
                || activeCubeCornersMap[x + 1, y + 0, z + 0].state == CubeCornerState.ACTIVE;
            toReturn[2] = activeCubeCornersMap[x + 1, y + 0, z + 1].state == CubeCornerState.RENDERED
                || activeCubeCornersMap[x + 1, y + 0, z + 1].state == CubeCornerState.ACTIVE;
            toReturn[3] = activeCubeCornersMap[x + 0, y + 0, z + 1].state == CubeCornerState.RENDERED
                || activeCubeCornersMap[x + 0, y + 0, z + 1].state == CubeCornerState.ACTIVE;
            toReturn[4] = activeCubeCornersMap[x + 0, y + 1, z + 0].state == CubeCornerState.RENDERED
                || activeCubeCornersMap[x + 0, y + 1, z + 0].state == CubeCornerState.ACTIVE;
            toReturn[5] = activeCubeCornersMap[x + 1, y + 1, z + 0].state == CubeCornerState.RENDERED
                || activeCubeCornersMap[x + 1, y + 1, z + 0].state == CubeCornerState.ACTIVE;
            toReturn[6] = activeCubeCornersMap[x + 1, y + 1, z + 1].state == CubeCornerState.RENDERED
                || activeCubeCornersMap[x + 1, y + 1, z + 1].state == CubeCornerState.ACTIVE;
            toReturn[7] = activeCubeCornersMap[x + 0, y + 1, z + 1].state == CubeCornerState.RENDERED
                || activeCubeCornersMap[x + 0, y + 1, z + 1].state == CubeCornerState.ACTIVE;
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                toReturn[i] = false;
            }
        }

        return toReturn;
    }

    private Color32 calculateTriangleColor(Vector3 triangleCenter)
    {
        Color32 toReturn;

        toReturn = colorMap.Evaluate(triangleCenter.y / maxHeight);

        return toReturn;
    }

    private int[,] generateTriangulationTable()
    {
        return new int[,]
        {
            { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
            { 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
            { 3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
            { 3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
            { 9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
            { 9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
            { 2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
            { 8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
            { 9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
            { 4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
            { 3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
            { 1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
            { 4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
            { 4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
            { 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
            { 5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
            { 2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
            { 9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
            { 0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
            { 2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
            { 10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
            { 4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
            { 5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
            { 5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
            { 9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
            { 0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
            { 1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
            { 10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
            { 8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
            { 2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
            { 7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
            { 9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
            { 2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
            { 11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
            { 9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
            { 5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
            { 11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
            { 11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
            { 1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
            { 9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
            { 5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
            { 2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
            { 0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
            { 5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
            { 6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
            { 3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
            { 6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
            { 5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
            { 1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
            { 10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
            { 6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
            { 8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
            { 7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
            { 3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
            { 5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
            { 0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
            { 9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
            { 8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
            { 5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
            { 0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
            { 6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
            { 10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
            { 10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
            { 8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
            { 1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
            { 3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
            { 0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
            { 10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
            { 3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
            { 6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
            { 9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
            { 8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
            { 3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
            { 6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
            { 0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
            { 10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
            { 10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
            { 2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
            { 7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
            { 7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
            { 2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
            { 1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
            { 11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
            { 8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
            { 0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
            { 7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
            { 10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
            { 2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
            { 6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
            { 7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
            { 2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
            { 1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
            { 10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
            { 10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
            { 0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
            { 7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
            { 6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
            { 8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
            { 9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
            { 6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
            { 4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
            { 10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
            { 8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
            { 0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
            { 1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
            { 8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
            { 10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
            { 4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
            { 10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
            { 5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
            { 11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
            { 9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
            { 6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
            { 7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
            { 3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
            { 7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
            { 9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
            { 3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
            { 6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
            { 9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
            { 1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
            { 4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
            { 7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
            { 6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
            { 3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
            { 0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
            { 6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
            { 0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
            { 11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
            { 6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
            { 5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
            { 9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
            { 1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
            { 1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
            { 10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
            { 0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
            { 5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
            { 10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
            { 11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
            { 9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
            { 7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
            { 2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
            { 8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
            { 9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
            { 9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
            { 1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
            { 9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
            { 9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
            { 5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
            { 0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
            { 10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
            { 2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
            { 0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
            { 0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
            { 9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
            { 5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
            { 3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
            { 5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
            { 8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
            { 9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
            { 0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
            { 1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
            { 3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
            { 4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
            { 9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
            { 11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
            { 11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
            { 2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
            { 9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
            { 3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
            { 1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
            { 4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
            { 4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
            { 0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
            { 3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
            { 3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
            { 0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
            { 9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
            { 1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { 0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
            { -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
        };
    }
}
