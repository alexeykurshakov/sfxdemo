using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Sfs2X.Util;
using UnityEngine;
using System.Collections;

public class ObjectController : MonoBehaviour
{
    [SerializeField]
    private ItemTypes _type;
    public ItemTypes Type { get { return _type; } }

    public int Id;

    private SmartFox _smartFox;

    private Vector3 _currentPos;

    private Quaternion _currentRot;

    private IInterpolation _interpolationImpl;

    private void Start()
    {
        if (GameManager.Instance.IsServer)
        {
            this._type = ItemTypes.Server;        
        }
        else
        {
            this._type = ItemTypes.Remote;        
            this.gameObject.AddComponent<RemoteInterpolation>();
            _interpolationImpl = this.gameObject.GetComponent<RemoteInterpolation>();          
        }
        
        this._smartFox = SmartFoxConnection.Connection;

        _currentPos = this.transform.localPosition;
        _currentRot = this.transform.localRotation;

        GameManager.Instance.RegisterObjectController(this.Id, this);
    }

    public void ProcessMessageRequest(ISFSObject data)
    {      		
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
    }

    public SFSObject GetSyncData()
    {
        var obj = new SFSObject();
		obj.PutUtfString(Codes.Actions.kCommand, Codes.Commands.kObjectUpdate);
		obj.PutInt(Codes.Variables.kId, this.Id);	

        obj.PutByteArray(Codes.Variables.kPosition, new ByteArray(ByteSerializer.Serialize(this.transform.localPosition)));
        obj.PutByteArray(Codes.Variables.kRotation, new ByteArray(ByteSerializer.Serialize(this.transform.localRotation)));
        return obj;
    }

    private void FixedUpdate()
    {        		
       	if (!GameManager.Instance.IsServer || _smartFox == null || _smartFox.LastJoinedRoom == null)
            return;        
      
        var obj = new SFSObject();
        var nothingToSend = true;
      
        if (_currentPos != this.transform.localPosition)
        {		
            nothingToSend = false;
            _currentPos = this.transform.localPosition;
            obj.PutByteArray(Codes.Variables.kPosition, new ByteArray(ByteSerializer.Serialize(_currentPos)));
        }

        if (_currentRot != this.transform.localRotation)
        {
            nothingToSend = false;
            _currentRot = this.transform.localRotation;
            obj.PutByteArray(Codes.Variables.kRotation, new ByteArray(ByteSerializer.Serialize(_currentRot)));
        }                

        if (nothingToSend)
            return;

        obj.PutUtfString(Codes.Actions.kCommand, Codes.Commands.kObjectUpdate);
        obj.PutInt(Codes.Variables.kId, this.Id);	

        _smartFox.Send(new ObjectMessageRequest(obj, _smartFox.LastJoinedRoom));
    }
}
