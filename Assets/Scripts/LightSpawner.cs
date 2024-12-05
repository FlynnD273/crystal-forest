using System;
using UnityEngine;

public class LightSpawner : MonoBehaviour
{
    public GameObject SmallLight;
    public GameObject BigLight;

    void Start()
    {
        ProcessChildren(transform);
    }

    void ProcessChildren(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.StartsWith("Crystal_Small"))
            {
                var renderer = child.GetComponent<MeshRenderer>();
                var mat = renderer.sharedMaterial;
                mat.SetInt("_Emit", 1);
                var newLight = Instantiate(SmallLight);
                newLight.transform.position = child.transform.position;
                newLight.transform.parent = child.transform;
            }
            else if (child.name.StartsWith("Crystal_Inner"))
            {
                var renderer = child.GetComponent<MeshRenderer>();
                var mat = renderer.sharedMaterial;
                mat.SetInt("_Emit", 1);
                var newLight = Instantiate(BigLight);
                newLight.transform.position = child.transform.position;
                newLight.transform.parent = child.transform;
            }
            ProcessChildren(child);
        }
    }
}
