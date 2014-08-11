using System;
using UnityEngine;
using System.Collections;

public class LocalInterpolation : MonoBehaviour, IInterpolation
{
	// Extremely simple and dumb interpolation script
	private Vector3 _desiredPos;
	private Quaternion _desiredRot;
	
	private const float kDampingFactor = 5f;
	
	void Start()
	{
		_desiredPos = this.transform.position;
		_desiredRot = this.transform.rotation;

		this.rigidbody.isKinematic = true;
	}
	
	public void SetPosition(Vector3 pos)
	{
		this._desiredPos = pos;
	}
	
	public void SetRotation(Quaternion rot)
	{
		this._desiredRot = rot;
	}
	
	public void SetTransform(Vector3 pos, Quaternion rot, bool interpolate = true)
	{
		// If interpolation, then set the desired pos+rot - else force set (for spawning new models)
		if (interpolate)
		{
			_desiredPos = pos;
			_desiredRot = rot;
		}
		else
		{
			this.transform.position = pos;
			this.transform.rotation = rot;				
		}
	}
	
	void Update()
	{
		// Really dumb interpolation, but works for this example
		this.transform.position = Vector3.Lerp(transform.position, _desiredPos, Time.deltaTime * kDampingFactor);
		this.transform.rotation = Quaternion.Slerp(transform.rotation, _desiredRot, Time.deltaTime * kDampingFactor);
	}
}
