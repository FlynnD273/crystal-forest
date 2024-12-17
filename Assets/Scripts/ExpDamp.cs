using System;
using UnityEngine;

public class ExpDamp
{
  public float TargetValue { get; set; }

  private readonly Action OnSet;
  private float value;
  public float Value
  {
    get => value;
    set
    {
      this.value = value;
      OnSet?.Invoke();
    }
  }

  public ExpDamp(float value = 0, float targetValue = 0, Action onSet = null)
  {
    Value = value;
    TargetValue = targetValue;
    OnSet = onSet;
  }

  public float Next(float speed, float deltaTime)
  {
    float val = Value + ((TargetValue - Value) * (1 - Mathf.Exp(-speed * deltaTime)));
    Value = Mathf.Abs(TargetValue - val) < 0.001f ? TargetValue : val;
    return Value;
  }
}

public class ExpDampVec3
{
  private readonly Action OnSet;

  private readonly ExpDamp x = new();
  private readonly ExpDamp y = new();
  private readonly ExpDamp z = new();

  public ExpDampVec3(Vector3 value = new(), Vector3 targetValue = new(), Action onSet = null)
  {
    Value = value;
    TargetValue = targetValue;
    OnSet = onSet;
  }

  public Vector3 TargetValue
  {
    get => new Vector3(x.TargetValue, y.TargetValue, z.TargetValue);
    set
    {
      x.TargetValue = value.x;
      y.TargetValue = value.y;
      z.TargetValue = value.z;
    }
  }

  public Vector3 Value
  {
    get => new Vector3(x.Value, y.Value, z.Value);
    set
    {
      x.Value = value.x;
      y.Value = value.y;
      z.Value = value.z;
      OnSet?.Invoke();
    }
  }

  public Vector3 Next(float speed, float deltaTime)
  {
    _ = x.Next(speed, deltaTime);
    _ = y.Next(speed, deltaTime);
    _ = z.Next(speed, deltaTime);

    if ((TargetValue - Value).magnitude < 0.001f)
    {
      Value = TargetValue;
    }
    else
    {
      OnSet?.Invoke();
    }
    return Value;
  }
}
