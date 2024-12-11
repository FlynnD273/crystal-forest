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


    void Start()
    {
        IEnumerable<GameObject> allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None).Where(static x => x.activeInHierarchy);
        bool shouldInit = true;
        Vector3 minDim = Vector3.zero;
        Vector3 maxDim = Vector3.zero;
        foreach (GameObject obj in allObjects)
        {
            if (shouldInit)
            {
                shouldInit = false;
                minDim = obj.transform.position;
                maxDim = obj.transform.position;
            }
            minDim = Vector3.Min(minDim, obj.transform.position);
            maxDim = Vector3.Max(maxDim, obj.transform.position);

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
            }
        }
        minDim -= Vector3.one * 50;
        maxDim += Vector3.one * 50;
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
            bunny.position = new Vector3(Random.Range(minDim.x, maxDim.x), 0, Random.Range(minDim.z, maxDim.z));
        }
    }
}
