using Sfs2X.Util;
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

public class ServerGameManager : MonoBehaviour, IGameManagerImpl
{
    public PlayerController LocalPlayer { get; set; }

    public Dictionary<User, PlayerController> RemotePlayers { get; private set; }

    public Dictionary<int, ObjectController> ObjectControllers { get; private set; }

    private SmartFox _smartFox;

    private string _gameRoomName = "GameRoom";

	private void Awake()
	{
		this._smartFox = new SmartFox(true);
		SmartFoxConnection.Connection = this._smartFox; 
	}

	private void ParseCommandLine()
	{		
//		var args = System.Environment.GetCommandLineArgs();
//		if (args.Length <= 1)
//			return;
//		
//		foreach (var arg in args)
//		{
//			if (arg.IndexOf("room") >= 0)
//			{
//				this._gameRoomName = arg.Split("=")[1];
//			}
//		}
	}
 
    private void Start()
    {
		this.ParseCommandLine();

       	RemotePlayers = new Dictionary<User, PlayerController>();
        ObjectControllers = new Dictionary<int, ObjectController>();	       

        _smartFox.AddEventListener(SFSEvent.CONNECTION, (evt) =>
        {
            var success = (bool)evt.Params["success"];
            var error = (string)evt.Params["errorMessage"];

            Debug.Log("On Connection callback got: " + success + " (error : <" + error + ">)");

            if (success)
            {
                _smartFox.Send(new LoginRequest("root", string.Empty, SmartFoxConnection.Zone));
            }
        });
        _smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, (evt) =>
        {
            Debug.LogError("Connection was lost, Reason: " + (string)evt.Params["reason"]);     
            Application.Quit();
        });
        _smartFox.AddEventListener(SFSEvent.LOGIN, (evt) =>
        {
            Debug.Log("Logged in successfully");

            if (_smartFox.RoomManager.ContainsRoom(_gameRoomName))
            {
                _smartFox.Send(new JoinRoomRequest(_gameRoomName));
            }
            else
            {
                var settings = new RoomSettings(_gameRoomName);
                settings.MaxUsers = 40;
                _smartFox.Send(new CreateRoomRequest(settings, true));
            }            
        });       
        _smartFox.AddLogListener(GameManager.Instance.logLevel, (evt) =>
        {
            var message = (string)evt.Params["message"];
            // Debug.Log("[SFS DEBUG] " + message);            
        });        
        _smartFox.AddEventListener(SFSEvent.USER_EXIT_ROOM, (evt) =>
        {          
            var user = (SFSUser)evt.Params["user"];

            Debug.Log(string.Format("User {0}({1}) exit room", user.Name, user.Id));

            GameManager.Instance.RemovePlayer(user);
        });
        _smartFox.AddEventListener(SFSEvent.OBJECT_MESSAGE, OnObjectMessage);         

        _smartFox.Connect(SmartFoxConnection.ServerName, SmartFoxConnection.ServerPort);        
    }

    private void OnObjectMessage(BaseEvent evt)
    {		
        var dataObj = (SFSObject)evt.Params["message"];
        if (dataObj.ContainsKey(Codes.Actions.kRequest))
        {
            switch (dataObj.GetUtfString(Codes.Actions.kRequest))
            {
                case Codes.Commands.kCreatePlayer:
                    this.DoCreateUserReq(evt);
                    break;

                default:
                    Debug.LogError("Not supported request");
                    break;
            }
            return;
        }

        if (!dataObj.ContainsKey(Codes.Actions.kCommand))
            return;

        var user = (SFSUser)evt.Params["sender"];
        switch (dataObj.GetUtfString(Codes.Actions.kCommand))
        {
            case Codes.Commands.kPlayerInputsSend:
                GameManager.Instance.GetPlayerController(user).ApplyRemoteInputs(dataObj);
                break;

            case Codes.Commands.kRemovePlayer:
                Debug.Log("Removing player unit " + user.Name);
                GameManager.Instance.RemovePlayer(user);
                break;
        }              
    }

    private void DoCreateUserReq(BaseEvent evt)
    {		
		var user = (SFSUser)evt.Params["sender"];
		if (this.RemotePlayers.ContainsKey(user))
			return;

        Debug.Log(string.Format("User {0}({1}) ask to join the game", user.Name, user.Id));

        var model = Random.Range(0, 3);
        var material = Random.Range(0, 4);

        var pos = Vector3.zero;
        var rot = new Quaternion();	       

        var targets = new List<User>{user};        

        // 1. sync with player objects
        foreach (var objectController in ObjectControllers)
        {
            var objData = objectController.Value.GetSyncData();
            this._smartFox.Send(new ObjectMessageRequest(objData, this._smartFox.LastJoinedRoom, targets));
        }

        // 2. create on new player others players controllers (not implemented)
        foreach (var playerPair in RemotePlayers)
        {            
            var objData = new SFSObject();

            objData.PutUtfString(Codes.Actions.kCommand, Codes.Commands.kCreatePlayer);

            objData.PutInt(Codes.Variables.kId, playerPair.Key.Id);
            objData.PutInt(Codes.Variables.kMaterial, playerPair.Value.CurrentMaterial);
            objData.PutInt(Codes.Variables.kModel, playerPair.Value.CurrentModel);
            objData.PutByteArray(Codes.Variables.kPosition, new ByteArray(ByteSerializer.Serialize(playerPair.Value.transform.position)));
            objData.PutByteArray(Codes.Variables.kRotation, new ByteArray(ByteSerializer.Serialize(playerPair.Value.transform.rotation)));
            
            this._smartFox.Send(new ObjectMessageRequest(objData, this._smartFox.LastJoinedRoom, targets));
        }

		// 3. create a new remote player
		var controller = GameManager.Instance.CreatePlayer(user, false, pos, rot, model, material);
		this.RemotePlayers.Add(user, controller);

        // 4. notify all that new player is joined
        var obj = new SFSObject();

        obj.PutUtfString(Codes.Actions.kCommand, Codes.Commands.kCreatePlayer);

        obj.PutInt(Codes.Variables.kId, user.Id);
        obj.PutInt(Codes.Variables.kMaterial, material);
        obj.PutInt(Codes.Variables.kModel, model);
        obj.PutByteArray(Codes.Variables.kPosition, new ByteArray(ByteSerializer.Serialize(pos)));
        obj.PutByteArray(Codes.Variables.kRotation, new ByteArray(ByteSerializer.Serialize(rot)));

        this._smartFox.Send(new ObjectMessageRequest(obj, this._smartFox.LastJoinedRoom));
    }

    private void FixedUpdate()
    {
        if (this._smartFox == null)
            return;

        _smartFox.ProcessEvents();	
   	}
}
