using UnityEngine;
using System.Collections;

public interface IInterpolation
{
    void SetPosition(Vector3 pos);

    void SetRotation(Quaternion rot);

    void SetTransform(Vector3 pos, Quaternion rot, bool interpolate = true);
}
