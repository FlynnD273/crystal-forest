using System.Linq;
using UnityEngine;

public class BigLightController : MonoBehaviour
{
    public float MinDist = 5;
    public float MaxDist = 20;
    public float Dim = 3;
    public float Bright = 50;

    private Transform _player;
    private Light _light;
    private Material _material;

    void Start()
    {
        _player = GameObject.FindGameObjectsWithTag("Player").First().transform;
        _light = GetComponent<Light>();
        var renderer = transform.parent.GetComponent<MeshRenderer>();
        _material = renderer.sharedMaterial;
    }

    void Update()
    {
        Vector3 offset = _player.position - transform.position;
        offset = new Vector2(offset.x, offset.z);
        float latDist = offset.sqrMagnitude;
        float maxSqr = MaxDist * MaxDist;
        float minSqr = MinDist * MinDist;
        latDist = (Mathf.Clamp(latDist, minSqr, maxSqr) - minSqr) / (maxSqr - minSqr);
        _light.intensity = Mathf.Lerp(Bright, Dim, latDist);
        _material.SetFloat("_Emit_Brightness", Mathf.Lerp(10, 0.1f, latDist));
    }
}
