using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SamplingType
{
    PERLIN,
    JITTERED
}

[System.Serializable]
public struct ObjectAndOffset
{
    public GameObject prefab;
    public Vector3 offset;
    public Vector2 sizeRandomizationBounds;
}

[System.Serializable]
public class ObjectSet
{
    public string setName;
    public bool place;

    [Space(1)]

    public SamplingType samplingType;

    [Header("Jittered")]
    public float averageDistance;

    [Header("Perlin")]
    public float zoom;
    public float limit;
    public float rayDensity;
    public Vector2 offset;
    public float smoothness;


    [Space(2)]

    public bool randomizeRotationAlongY;
    public List<ObjectAndOffset> objectsPrefabs;
}

[ExecuteInEditMode]
public class GameObjectRandomPlacer : MonoBehaviour
{
    public bool reset;
    public string targetTag;
    public Vector2 areaSize;
    public float rayTracingHeight;
    public List<ObjectSet> objectSets; 

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(reset)
        {
            while(this.transform.childCount > 0)
            {
                GameObject.DestroyImmediate(this.transform.GetChild(0).gameObject);
            }
            reset = false;
        }
        
        for (int i = 0; i < objectSets.Count; i++)
        {
            if(objectSets[i].place == true)
            {
                OnPlaceClicked(objectSets[i]);
                objectSets[i].place = false;
            }
        }
    }

    private void OnDrawGizmos()
    {
        //Placing area gizmos
        RaycastHit hit;
        Physics.Raycast(new Vector3(this.transform.position.x - areaSize.x / 2.0f, this.transform.position.y, this.transform.position.z - areaSize.y / 2.0f),
                        new Vector3(0, -1, 0),
                        out hit);
        Vector3 point1 = hit.point;
        Physics.Raycast(new Vector3(this.transform.position.x + areaSize.x / 2.0f, this.transform.position.y, this.transform.position.z - areaSize.y / 2.0f),
                        new Vector3(0, -1, 0),
                        out hit);
        Vector3 point2 = hit.point;
        Physics.Raycast(new Vector3(this.transform.position.x + areaSize.x / 2.0f, this.transform.position.y, this.transform.position.z + areaSize.y / 2.0f),
                        new Vector3(0, -1, 0),
                        out hit);
        Vector3 point3 = hit.point;
        Physics.Raycast(new Vector3(this.transform.position.x - areaSize.x / 2.0f, this.transform.position.y, this.transform.position.z + areaSize.y / 2.0f),
                        new Vector3(0, -1, 0),
                        out hit);
        Vector3 point4 = hit.point;
        Gizmos.color = Color.red;
        Gizmos.DrawLine(point1, point2);
        Gizmos.DrawLine(point2, point3);
        Gizmos.DrawLine(point3, point4);
        Gizmos.DrawLine(point4, point1);
        Gizmos.DrawLine(point1, new Vector3(point1.x, rayTracingHeight, point1.z));
        Gizmos.DrawLine(point2, new Vector3(point2.x, rayTracingHeight, point2.z));
        Gizmos.DrawLine(point3, new Vector3(point3.x, rayTracingHeight, point3.z));
        Gizmos.DrawLine(point4, new Vector3(point4.x, rayTracingHeight, point4.z));
        Gizmos.DrawLine(new Vector3(point1.x, rayTracingHeight, point1.z), new Vector3(point2.x, rayTracingHeight, point2.z));
        Gizmos.DrawLine(new Vector3(point2.x, rayTracingHeight, point2.z), new Vector3(point3.x, rayTracingHeight, point3.z));
        Gizmos.DrawLine(new Vector3(point3.x, rayTracingHeight, point3.z), new Vector3(point4.x, rayTracingHeight, point4.z));
        Gizmos.DrawLine(new Vector3(point4.x, rayTracingHeight, point4.z), new Vector3(point1.x, rayTracingHeight, point1.z));
    }

    public void OnPlaceClicked(ObjectSet selected)
    {
        switch(selected.samplingType)
        {
            case SamplingType.JITTERED:
                {
                    generateWithJitteredSamplingType(selected);
                    break;
                }
            case SamplingType.PERLIN:
                {
                    generateWithPerlinSamplingType(selected);
                    break;
                }
            default:
                {
                    Debug.LogError("ERROR : sampling type unrecognized");
                    break;
                }
        }
    }

    void generateWithJitteredSamplingType(ObjectSet selected)
    {
        int numberOfPointsAlongXAxis = Mathf.FloorToInt(areaSize.x / selected.averageDistance);
        int numberOfPointsAlongYAxis = Mathf.FloorToInt(areaSize.y / selected.averageDistance);

        //set the origin of the grid
        Vector3 origin = new Vector3(this.transform.position.x - areaSize.x / 2.0f, rayTracingHeight, this.transform.position.z - areaSize.y / 2.0f);

        for (int i = 0; i < numberOfPointsAlongXAxis; i++)
        {
            for (int j = 0; j < numberOfPointsAlongYAxis; j++)
            {
                //setting the raycast origin and direction
                Vector3 rayOrigin = origin + new Vector3(i * selected.averageDistance, 0, j * selected.averageDistance);
                rayOrigin += new Vector3(Random.Range(0.0f, selected.averageDistance), 0, Random.Range(0.0f, selected.averageDistance));
                Vector3 rayDirection = new Vector3(0, -1.0f, 0);

                //throwing ray
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, rayDirection, out hit)
                && hit.collider.gameObject.CompareTag(targetTag))
                {
                    //Generate object
                    int selectedObjectIndex = Random.Range(0, selected.objectsPrefabs.Count);
                    GameObject generated = GameObject.Instantiate(selected.objectsPrefabs[selectedObjectIndex].prefab);

                    //randomize object
                    generated.transform.Rotate(new Vector3(0, 1, 0), Random.Range(0, 360), Space.World);
                    float scaleMultiplyer = Random.Range(selected.objectsPrefabs[selectedObjectIndex].sizeRandomizationBounds.x, selected.objectsPrefabs[selectedObjectIndex].sizeRandomizationBounds.y);
                    generated.transform.localScale *= scaleMultiplyer;

                    //set position and parent
                    generated.transform.position = hit.point + selected.objectsPrefabs[selectedObjectIndex].offset * scaleMultiplyer;
                    generated.transform.SetParent(this.transform);
                }
            }
        }
    }

    void generateWithPerlinSamplingType(ObjectSet selected)
    {
        Vector3 origin = new Vector3(this.transform.position.x - areaSize.x / 2.0f, rayTracingHeight, this.transform.position.z - areaSize.y / 2.0f);

        int numberOfRays = Mathf.FloorToInt(selected.rayDensity * areaSize.x * areaSize.y);
        for (int i = 0; i < numberOfRays; i++)
        {
            Vector3 rayOrigin = origin + new Vector3(Random.Range(0.0f, areaSize.x), 0, Random.Range(0.0f, areaSize.y));
            if (Mathf.PerlinNoise(rayOrigin.x * selected.zoom + selected.offset.x, rayOrigin.z * selected.zoom + selected.offset.y) + Random.Range(0, selected.smoothness) > selected.limit)
            {

                Vector3 rayDirection = new Vector3(0, -1.0f, 0);

                //throwing ray
                RaycastHit hit;
                if (Physics.Raycast(rayOrigin, rayDirection, out hit)
                && hit.collider.gameObject.CompareTag(targetTag))
                {
                    //Generate object
                    int selectedObjectIndex = Random.Range(0, selected.objectsPrefabs.Count);
                    GameObject generated = GameObject.Instantiate(selected.objectsPrefabs[selectedObjectIndex].prefab);

                    //randomize object
                    generated.transform.Rotate(new Vector3(0, 1, 0), Random.Range(0, 360), Space.World);
                    float scaleMultiplyer = Random.Range(selected.objectsPrefabs[selectedObjectIndex].sizeRandomizationBounds.x, selected.objectsPrefabs[selectedObjectIndex].sizeRandomizationBounds.y);
                    generated.transform.localScale *= scaleMultiplyer;

                    //set position and parent
                    generated.transform.position = hit.point + selected.objectsPrefabs[selectedObjectIndex].offset * scaleMultiplyer;
                    generated.transform.SetParent(this.transform);
                }
            }
        }
    }
}
