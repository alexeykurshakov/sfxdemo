using System.Collections.Generic;
using System.Linq;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Sfs2X.Util;
using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    public const float kForwardSpeed = 15f;    

    public const float kRotationSpeed = 60f; 

    [SerializeField] private ItemTypes _type;
    public ItemTypes Type { get { return _type; } }

    private SmartFox _smartFox;

    private readonly Dictionary<string, float> _inputActions = new Dictionary<string, float>(4);

    private IInterpolation _interpolationImpl;

    private User _me;

    private Vector3 _lastPosition;

	private Quaternion _lastRotation;

    public int CurrentModel { get; private set; }

    public int CurrentMaterial { get; private set; }

	private void Start()
	{
		_lastPosition = this.transform.position;
		_lastRotation = this.transform.rotation;
	}
 
    public void Init(User me, ItemTypes type, int model, int material)
    {
        this._me = me;
        this._type = type;
        this._smartFox = SmartFoxConnection.Connection;             

        switch (_type)
        {
            case ItemTypes.Local:
                this.gameObject.AddComponent<LocalInterpolation>();
                this._interpolationImpl = this.gameObject.GetComponent<LocalInterpolation>();

                Camera.main.transform.parent = this.transform;
                break;

            case ItemTypes.Remote:
                this.gameObject.AddComponent<RemoteInterpolation>();
                this._interpolationImpl = this.gameObject.GetComponent<RemoteInterpolation>();
                break;
        }

		// set label
        this.GetComponentInChildren<TextMesh>().text = me.Name;     

		// remember model number
		this.CurrentModel = model;

		// set material
		this.gameObject.GetComponentInChildren<Renderer>().material = GameManager.Instance.playerMaterials[material];
		this.CurrentMaterial = material;
    }

    public void Dispose()
    {
        if (this.Type == ItemTypes.Local)
        {
            Camera.main.transform.parent = null;
        }
        Destroy(this.gameObject);
        this._me = null;
    }

    public void ProcessMessageRequest(ISFSObject data)
    {        
        if (this._me == null)
            return;

        if (data.ContainsKey(Codes.Variables.kPosition))
        {
            var pos = ByteSerializer.DeserializeV(data.GetByteArray(Codes.Variables.kPosition).Bytes);
            _interpolationImpl.SetPosition(pos);
        }

        if (data.ContainsKey(Codes.Variables.kRotation))
        {
            var rot = ByteSerializer.DeserializeQ(data.GetByteArray(Codes.Variables.kRotation).Bytes);
            _interpolationImpl.SetRotation(rot);
        }

        if (this._type == ItemTypes.Local)
            return;

        if (data.ContainsKey(Codes.Variables.kModel))
        {
            this.ChangePlayerModel(data.GetInt(Codes.Variables.kModel));
        }

        if (data.ContainsKey(Codes.Variables.kMaterial))
        {
            this.ChangePlayerMaterial(data.GetInt(Codes.Variables.kMaterial));            
        }
    }

    public void ChangePlayerMaterial(int numMaterial)
    {
        this.gameObject.GetComponentInChildren<Renderer>().material = GameManager.Instance.playerMaterials[numMaterial];
        this.CurrentMaterial = numMaterial;

        if (this._type != ItemTypes.Local)
            return;

        if (_inputActions.ContainsKey(Codes.Variables.kMaterial))
        {
            _inputActions[Codes.Variables.kMaterial] = numMaterial;
        }
        else
        {
            _inputActions.Add(Codes.Variables.kMaterial, numMaterial);
        }
    }

    public void ChangePlayerModel(int numModel)
    {
		if (this._type == ItemTypes.Local)
		{
			if (_inputActions.ContainsKey(Codes.Variables.kModel))
			{
				_inputActions[Codes.Variables.kModel] = numModel;
			}
			else
			{
				_inputActions.Add(Codes.Variables.kModel, numModel);
			}
			this.LocalFixedUpdate();
		}
		GameManager.Instance.CreatePlayer(this._me, true, numModel, CurrentMaterial);               
    }

    public void ApplyRemoteInputs(ISFSObject data)
    {
		var myId = this._me.Id;
        var needSend = false;
        var obj = new SFSObject();

        if (data.ContainsKey(Codes.Variables.kMaterial))
        {
            var mat = (int)data.GetFloat(Codes.Variables.kMaterial);
            this.ChangePlayerMaterial(mat);

            needSend = true;
            obj.PutInt(Codes.Variables.kMaterial, mat);            
        }

        if (data.ContainsKey(Codes.Variables.kModel))
        {
            var model = (int)data.GetFloat(Codes.Variables.kModel);
            this.ChangePlayerModel(model);

            needSend = true;
            obj.PutInt(Codes.Variables.kModel, model);            
        }

        if (data.ContainsKey("translate"))
        {
            var translate = data.GetFloat("translate");
            this.transform.Translate(0, 0, translate);			           
        }

        if (data.ContainsKey("rotation"))
        {
            var rotation = data.GetFloat("rotation");
            this.transform.Rotate(Vector3.up, rotation);			           
        }

		if (data.ContainsKey("jump") && this.IsGrounded())
		{
			this.Jump ();
		}

		if (data.ContainsKey("confirm"))
		{
			obj.PutByteArray(Codes.Variables.kPosition, new ByteArray(ByteSerializer.Serialize(this.transform.position)));
			obj.PutByteArray(Codes.Variables.kRotation, new ByteArray(ByteSerializer.Serialize(this.transform.rotation)));   

			needSend = true;
		}

        if (!needSend)
            return;

        obj.PutUtfString(Codes.Actions.kCommand, Codes.Commands.kPlayerUpdate);
		obj.PutInt(Codes.Variables.kId, myId);
        _smartFox.Send(new ObjectMessageRequest(obj, _smartFox.LastJoinedRoom));                             
    }

    private void ServerFixedUpdate()
    {
        if (false == (_lastPosition != this.transform.position 
		              || _lastRotation != this.transform.rotation))
			return;

		_lastPosition = this.transform.position;
		_lastRotation = this.transform.rotation;

        var obj = new SFSObject();
        obj.PutByteArray(Codes.Variables.kPosition, new ByteArray(ByteSerializer.Serialize(this.transform.position)));
        obj.PutByteArray(Codes.Variables.kRotation, new ByteArray(ByteSerializer.Serialize(this.transform.rotation)));   

        obj.PutUtfString(Codes.Actions.kCommand, Codes.Commands.kPlayerUpdate);
        obj.PutInt(Codes.Variables.kId, this._me.Id);
        _smartFox.Send(new ObjectMessageRequest(obj, _smartFox.LastJoinedRoom));        
    }

    private void LocalFixedUpdate()
    {
        if (!_inputActions.Any())
            return;

        var obj = new SFSObject();
        obj.PutUtfString(Codes.Actions.kCommand, Codes.Commands.kPlayerInputsSend);

        foreach (var inputAction in _inputActions)
        {
            obj.PutFloat(inputAction.Key, inputAction.Value);            
        }        

        _smartFox.Send(new ObjectMessageRequest(obj, _smartFox.LastJoinedRoom));
        _inputActions.Clear();
    }

	private bool IsGrounded()
	{
		return Physics.Raycast(transform.position, -Vector3.up, collider.bounds.extents.y+0.1f);
	}

	private void Jump()
	{
		//rigidbody.AddForce(Vector3.up *900f);
	}

    private void FixedUpdate()
    {
        if (this._smartFox == null)
            return;	

        switch (Type)
        {
            case ItemTypes.Local:
                this.LocalFixedUpdate();
                break;

            case ItemTypes.Server:
                this.ServerFixedUpdate();
                break;
        }        
    }      
  
    private void Update()
    {
        if (this._type != ItemTypes.Local)
            return;
        
        var translation = Input.GetAxis("Vertical");
        if (translation != 0)
        {
            var translateVal = translation*Time.deltaTime*kForwardSpeed;
            // this.transform.Translate(0, 0, translateVal);

            if (_inputActions.ContainsKey("translate"))
            {
                _inputActions["translate"] = _inputActions["translate"] + translateVal;
            }
            else
            {   
                _inputActions.Add("translate", translateVal);                
            }
        }
		       
        var rotation = Input.GetAxis("Horizontal");
        if (rotation != 0)
        {
            var rotationVal = rotation*Time.deltaTime*kRotationSpeed;
			// this.transform.Rotate(Vector3.up, rotationVal);

            if (_inputActions.ContainsKey("rotation"))
            {
				_inputActions["rotation"] = _inputActions["rotation"] + rotationVal;
            }
            else
            {
                _inputActions.Add("rotation", rotationVal);
            }
        }

		var jump = Input.GetAxis("Jump");
		if (jump != 0 && this.IsGrounded())
		{		
			if (_inputActions.ContainsKey("jump"))
			{
				_inputActions["jump"] = 1;
			}
			else
			{
				_inputActions.Add("jump", 1);
			}
		}
    }
}
