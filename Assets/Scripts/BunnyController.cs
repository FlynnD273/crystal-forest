using UnityEngine;

public class BunnyController : MonoBehaviour
{
  public float JumpIntervalMin = 4;
  public float JumpIntervalMax = 4;
  public float JumpDistance = 5;

  private float jumpInterval = 0;
  private Rigidbody rb;
  private Animator anim;
  private float lastJumped = 0;
  private State state = State.Idle;
  private ExpDamp rotation;
  private float? rotationOverride = null;
  private AudioSource audio;

  private enum State
  {
    Idle,
    JumpInit,
  }

  void Start()
  {
    rb = GetComponent<Rigidbody>();
    anim = GetComponent<Animator>();
    rotation = new ExpDamp(0, Random.Range(0, 360), () => { rb.MoveRotation(Quaternion.Euler(0, rotation.Value, 0)); });
    jumpInterval = Random.Range(0, JumpIntervalMax);
    audio = GetComponent<AudioSource>();
  }

  void FixedUpdate()
  {
    switch (state)
    {
      case State.Idle:
        if (Time.time - lastJumped > jumpInterval)
        {
          lastJumped = Time.time;
          if (rotationOverride != null)
          {
            rotation.TargetValue = rotationOverride.Value;
            rotationOverride = null;
          }
          rotation.TargetValue += Random.Range(-90f, 90f);
          anim.SetTrigger("Jump");
          state = State.JumpInit;
          audio.PlayDelayed(0.3f);
        }
        break;
      case State.JumpInit:
        if (Time.time - lastJumped > 0.4)
        {
          Vector3 direction = rb.rotation * Vector3.left;
          rb.AddForce(direction * JumpDistance, ForceMode.Impulse);
          state = State.Idle;
          jumpInterval = Random.Range(JumpIntervalMin, JumpIntervalMax);
        }
        break;
    }
    rotation.Next(10, Time.deltaTime);
  }

  void OnTriggerEnter(Collider other)
  {
    rotationOverride = rotation.TargetValue + 180;
  }
}
