using System;
using UnityEngine;

public class LightSpawner : MonoBehaviour
{
    public GameObject SmallLight;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (Transform child in transform)
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
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
