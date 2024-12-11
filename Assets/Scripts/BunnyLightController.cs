using System.Linq;
using UnityEngine;

public class BunnyLightController : MonoBehaviour
{
    public float MinDist = 5;
    public float MaxDist = 20;
    public float Bright = 50;

    private Transform _player;
    private Light _light;
    private Material _material;
    private Rigidbody rb;

    void Start()
    {
        _player = GameObject.FindGameObjectsWithTag("Player").First().transform;
        _light = GetComponent<Light>();
        rb = transform.parent.parent.GetComponent<Rigidbody>();
        SkinnedMeshRenderer renderer = transform.parent.GetComponent<SkinnedMeshRenderer>();
        _material = renderer.material;
        _material.SetInt("_Emit", 1);
    }

    void Update()
    {
        Vector3 offset = _player.position - rb.position;
        offset = new Vector3(offset.x, 0, offset.z);
        float maxSqr = MaxDist * MaxDist;
        if (offset.sqrMagnitude > maxSqr)
        {
            _light.enabled = false;
            _material.SetFloat("_Emit_Brightness", 0.1f);
        }
        else
        {
            _light.enabled = true;
            float minSqr = MinDist * MinDist;
            float latDist = offset.sqrMagnitude;
            latDist = (Mathf.Clamp(latDist, minSqr, maxSqr) - minSqr) / (maxSqr - minSqr);
            _light.intensity = Mathf.Lerp(Bright, 0, latDist);
            _material.SetFloat("_Emit_Brightness", Mathf.Lerp(1, 0.1f, latDist));
        }
    }
}

