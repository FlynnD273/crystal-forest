using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightSpawner : MonoBehaviour
{
    public GameObject SmallLight;
    public GameObject BigLight;
    public GameObject BigTreeLight;
    public GameObject Bunny;
    public int BunnyCount;
    public GameObject Ground;
    public float Width;
    public float Depth;
    public GameObject[] Trees;
    public int TreeCount;

    private IEnumerable<GameObject> allObjects;
    private Transform _player;

    void Start()
    {
        _player = GameObject.FindGameObjectsWithTag("Player").First().transform;
        Collider _noTrees = GetComponent<Collider>();
        Vector3 minDim = new Vector3(-Width / 2, 0, -Depth / 2);
        Vector3 maxDim = new Vector3(Width / 2, 0, Depth / 2);
        for (int i = 0; i < TreeCount; i++)
        {
            var obj = Instantiate(Trees[Random.Range(0, Trees.Length)]);
            Vector3 pos;
            do
            {
                pos = new Vector3(Random.Range(minDim.x, maxDim.x), 0, Random.Range(minDim.z, maxDim.z));
            } while (_noTrees.bounds.Contains(pos));
            obj.transform.position = pos;
            obj.transform.rotation = Quaternion.EulerAngles(0, Random.Range(0, 360f), 0);
        }
        allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(static x => x.activeInHierarchy);
        bool shouldInit = true;
        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith("Crystal_Small"))
            {
                GameObject newLight = Instantiate(SmallLight);
                newLight.transform.position = obj.transform.position;
                newLight.transform.parent = obj.transform;
            }
            else if (obj.name.StartsWith("BigTree_Crystal"))
            {
                GameObject newLight = Instantiate(BigTreeLight);
                newLight.transform.position = obj.transform.position;
                newLight.transform.parent = obj.transform;
            }
            else if (obj.name.StartsWith("Crystal_Inner"))
            {
                GameObject newLight = Instantiate(BigLight);
                newLight.transform.position = obj.transform.position;
                newLight.transform.parent = obj.transform;
            }
            else if (obj.name.StartsWith("TreeTrunk"))
            {
                CapsuleCollider coll = obj.AddComponent<CapsuleCollider>();
                coll.center = new Vector3(0, 0, 0.04f);
                coll.radius = 0.01f;
                coll.height = 0.1f;
                coll.direction = 2;
            }
        }
        for (int x = (int)minDim.x; x < maxDim.x; x += 50)
        {
            for (int z = (int)minDim.z; z < maxDim.z; z += 50)
            {
                GameObject groundPiece = Instantiate(Ground);
                BoxCollider coll = groundPiece.AddComponent<BoxCollider>();
                coll.center = new Vector3(0, 0, -0.1f);
                coll.size = new Vector3(0.8f, 0.8f, 0.2f);
                groundPiece.transform.position = new Vector3(x, 0, z);
            }
        }

        for (int i = 0; i < BunnyCount; i++)
        {
            Rigidbody bunny = Instantiate(Bunny).GetComponent<Rigidbody>();
            Vector3 pos;
            do
            {
                pos = new Vector3(Random.Range(minDim.x, maxDim.x), 0, Random.Range(minDim.z, maxDim.z));
            } while (_noTrees.bounds.Contains(pos));
            bunny.position = pos;
        }
    }
}
