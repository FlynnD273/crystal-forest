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

    private ExpDamp brightness;
    private bool isCharged = false;

    void Start()
    {
        brightness = new ExpDamp(0, 0, () =>
      {
          _light.intensity = brightness?.Value ?? 0;
          _material.SetFloat("_Emit_Brightness", brightness?.Value ?? 0 / 10);
      });

        _player = GameObject.FindGameObjectsWithTag("Player").First().transform;
        _light = GetComponent<Light>();
        MeshRenderer renderer = transform.parent.GetComponent<MeshRenderer>();
        _material = renderer.sharedMaterial;
        _material.SetInt("_Emit", 1);
    }

    void Update()
    {
        if (!isCharged)
        {
            Vector3 offset = _player.position - transform.position;
            if (offset.sqrMagnitude < 4)
            {
                isCharged = true;
                brightness.TargetValue = 50;
                brightness.Value = 10000;
                GetComponent<AudioSource>().Play();
            }
            offset = new Vector2(offset.x, offset.z);
            float latDist = offset.sqrMagnitude;
            float maxSqr = MaxDist * MaxDist;
            float minSqr = MinDist * MinDist;
            latDist = (Mathf.Clamp(latDist, minSqr, maxSqr) - minSqr) / (maxSqr - minSqr);
            _light.intensity = Mathf.Lerp(Bright, Dim, latDist);
            _material.SetFloat("_Emit_Brightness", Mathf.Lerp(10, 0.1f, latDist));
        }
        else
        {
            _ = brightness.Next(3f, Time.deltaTime);
        }
    }
}
