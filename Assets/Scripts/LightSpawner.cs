using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LightSpawner : MonoBehaviour
{
    public GameObject SmallLight;
    public GameObject BigLight;
    public GameObject Ground;


    internal void Start()
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
                obj.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_Emit", 1);
                GameObject newLight = Instantiate(SmallLight);
                newLight.transform.position = obj.transform.position;
                newLight.transform.parent = obj.transform;
            }
            else if (obj.name.StartsWith("Crystal_Inner"))
            {
                obj.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_Emit", 1);
                GameObject newLight = Instantiate(BigLight);
                newLight.transform.position = obj.transform.position;
                newLight.transform.parent = obj.transform;
            }
            else if (obj.name.StartsWith("Bunny"))
            {
                obj.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_Emit", 1);
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
                groundPiece.transform.position = new Vector3(x, 0, z);
            }
        }
        ProcessChildren(transform);
    }

    internal void ProcessChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("Crystal_Small"))
            {
                child.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_Emit", 1);
                GameObject newLight = Instantiate(SmallLight);
                newLight.transform.position = child.transform.position;
                newLight.transform.parent = child.transform;
            }
            else if (child.name.StartsWith("Crystal_Inner"))
            {
                child.GetComponent<MeshRenderer>().sharedMaterial.SetInt("_Emit", 1);
                GameObject newLight = Instantiate(BigLight);
                newLight.transform.position = child.transform.position;
                newLight.transform.parent = child.transform;
            }
            else if (child.name.StartsWith("TreeTrunk"))
            {
                CapsuleCollider coll = child.gameObject.AddComponent<CapsuleCollider>();
                coll.center = new Vector3(0, 0, 0.04f);
                coll.radius = 0.01f;
                coll.height = 0.1f;
            }
            ProcessChildren(child);
        }
    }
}
