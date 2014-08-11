using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Entities.Data;
using Sfs2X.Entities.Variables;
using Sfs2X.Requests;
using Sfs2X.Logging;


public class ClientGameManager : MonoBehaviour, IGameManagerImpl
{
    public PlayerController LocalPlayer { get; set; }

    public Dictionary<User, PlayerController> RemotePlayers { get; private set; }

    public Dictionary<int, ObjectController> ObjectControllers { get; private set; }	   

    private SmartFox _smartFox;    

    private void Start()
    {
        RemotePlayers = new Dictionary<User, PlayerController>();
        ObjectControllers = new Dictionary<int, ObjectController>();

        if (!SmartFoxConnection.IsInitialized)
        {
            Application.LoadLevel("Connection");
            return;
        }

        _smartFox = SmartFoxConnection.Connection;
        
        _smartFox.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnObjectMessage);                

        _smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, (evt) =>
        {
            Debug.Log("Lost connection, load connection scene");

            _smartFox.RemoveAllEventListeners();
            Application.LoadLevel("Connection");
        });
        _smartFox.AddEventListener(SFSEvent.USER_EXIT_ROOM, (evt) =>
        {            
            var user = (SFSUser)evt.Params["user"];

            Debug.Log(string.Format("User {0}({1}) exit room", user.Name, user.Id));

            GameManager.Instance.RemovePlayer(user);
        });
        _smartFox.AddLogListener(GameManager.Instance.logLevel, (evt) =>
        {
            var message = (string)evt.Params["message"];
            // Debug.Log("[SFS DEBUG] " + message);
        });

        this.StartCoroutine(this.DoCreatePlayerReq());
    }

    private IEnumerator DoCreatePlayerReq()
    {
        var obj = new SFSObject();	

        obj.PutUtfString(Codes.Actions.kRequest, Codes.Commands.kCreatePlayer);

        while (this.LocalPlayer == null)
        {
            _smartFox.Send(new ObjectMessageRequest(obj, _smartFox.LastJoinedRoom));

            yield return new WaitForSeconds(0.5f);
        }        

        yield break;
    }

    private void FixedUpdate()
    {
        if (this._smartFox == null)
            return;

        _smartFox.ProcessEvents();
    }

    private void OnApplicationQuit()
    {
        // Before leaving, lets notify the others about this client dropping out
        GameManager.Instance.RemovePlayer(_smartFox.MySelf);        
    }

    private void OnObjectMessage(BaseEvent evt)
    {       
        var dataObj = (SFSObject)evt.Params["message"];
        if (!dataObj.ContainsKey(Codes.Actions.kCommand))
            return;

        switch (dataObj.GetUtfString(Codes.Actions.kCommand))
        {
            case Codes.Commands.kPlayerUpdate:
                this.DoPlayerUpdateCmd(dataObj);
                break;

            case Codes.Commands.kObjectUpdate:
                this.DoObjectUpdaterCmd(dataObj);
                break;

            case Codes.Commands.kCreatePlayer:				
                this.DoCreatePlayerCmd(dataObj);
                break;

            case Codes.Commands.kRemovePlayer:
                this.DoRemovePlayerCmd((SFSUser)evt.Params["sender"]);
                break;
        }              
    }

    private void DoPlayerUpdateCmd(ISFSObject data)
    {
        var playerId = data.GetInt(Codes.Variables.kId);        
        GameManager.Instance.GetPlayerController(_smartFox.UserManager.GetUserById(playerId))
            .ProcessMessageRequest(data);
    }

    private void DoObjectUpdaterCmd(ISFSObject data)
    {
        var objId = data.GetInt(Codes.Variables.kId);
        if (!this.ObjectControllers.ContainsKey(objId))
        {
            Debug.LogError( string.Format("Couldn't find object with id {0}", objId) );
            return;
        }

        this.ObjectControllers[objId].ProcessMessageRequest(data);
    }

    private void DoCreatePlayerCmd(ISFSObject data)
    {
        var userId = data.GetInt(Codes.Variables.kId);
        var user = _smartFox.UserManager.GetUserById(userId);

        var material = data.GetInt(Codes.Variables.kMaterial);
        var model = data.GetInt(Codes.Variables.kModel);
        var pos = ByteSerializer.DeserializeV(data.GetByteArray(Codes.Variables.kPosition).Bytes);
        var rot = ByteSerializer.DeserializeQ(data.GetByteArray(Codes.Variables.kRotation).Bytes);

        var controller = GameManager.Instance.CreatePlayer(user, user.IsItMe, pos, rot, model, material);
        if (user.IsItMe)
        {
            this.LocalPlayer = controller;
        }
        else
        {
            this.RemotePlayers.Add(user, controller);
        }
    }

    private void DoRemovePlayerCmd(User player)
    {
        Debug.Log("Removing player unit " + player.Name);
        GameManager.Instance.RemovePlayer(player);        
    }
}