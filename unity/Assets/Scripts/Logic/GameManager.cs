using System;
using System.CodeDom;
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
using System.Linq;

public class GameManager : MonoBehaviour
{
    public bool IsServer;

    public static GameManager Instance { get; private set; }

    public GameObject[] playerModels;

    public Material[] playerMaterials;

    public LogLevel logLevel = LogLevel.DEBUG;

    private IGameManagerImpl _currentImpl;
   
    public List<User> OtherPlayers
    {
        get { return _currentImpl.RemotePlayers.Keys.ToList(); }
    }    

    public PlayerController GetPlayerController(User user)
    {
        if (user.IsItMe)
            return _currentImpl.LocalPlayer;

        return this._currentImpl.RemotePlayers.ContainsKey(user)
            ? this._currentImpl.RemotePlayers[user]
            : null;        
    }

    public void RegisterObjectController(int objectId, ObjectController objectController)
    {
        this._currentImpl.ObjectControllers.Add(objectId, objectController);
    }

    private void Awake()
    {
        Instance = this;
		if (this.IsServer)
		{
			this.StartAsServer();
		}
		else
		{
			this.StartAsClient();
		}      
    }

    private void StartAsServer()
    {
        this.gameObject.AddComponent<ServerGameManager>();
        this._currentImpl = this.gameObject.GetComponent<ServerGameManager>();
        this.gameObject.AddComponent<ServerUI>();        
    }

    private void StartAsClient()
    {
        this.gameObject.AddComponent<ClientGameManager>();
        this._currentImpl = this.gameObject.GetComponent<ClientGameManager>();
        this.gameObject.AddComponent<ClientUI>();
    }

    public PlayerController CreatePlayer(User user, bool local, int numModel, int numMaterial)
    {
        var playerController = this.GetPlayerController(user);
        if (playerController == null)
        {
            throw new InvalidOperationException("No controller found for user " + user.Name);               
        }

        var pos = playerController.transform.position;
        var rot = playerController.transform.rotation;
        playerController.Dispose();

		var newPlayer = this.CreatePlayer(user, local, pos, rot, numModel, numMaterial);
		if (this._currentImpl.LocalPlayer == playerController)
		{
			this._currentImpl.LocalPlayer = newPlayer;
		}
		else if (this._currentImpl.RemotePlayers.ContainsKey(user))
		{
			this._currentImpl.RemotePlayers[user] = newPlayer;
		}

        return newPlayer;
    }

    public PlayerController CreatePlayer(User user, bool local, Vector3 pos, Quaternion rot, int numModel, int numMaterial)
    {        
        var player = Instantiate(playerModels[numModel]) as GameObject;
        player.transform.position = pos;
        player.transform.rotation = rot;

        var playerController = player.AddComponent<PlayerController>();        
		playerController.Init(user, this.IsServer ? ItemTypes.Server : local ? ItemTypes.Local : ItemTypes.Remote, 
		                      numModel, numMaterial);
        return playerController;        
    }

    public void RemovePlayer(User user)
    {
        if (this._currentImpl.RemotePlayers.ContainsKey(user))
        {
            this.RemoveRemotePlayer(user);
        }
        else if (user.Id == SmartFoxConnection.Connection.MySelf.Id)
        {
            this.RemoveLocalPlayer();
        }
    }

    private void RemoveLocalPlayer()
    {        
		var connection = SmartFoxConnection.Connection;

        this._currentImpl.LocalPlayer.Dispose();
        this._currentImpl.LocalPlayer = null;

        var obj = new SFSObject();
        obj.PutUtfString(Codes.Actions.kCommand, Codes.Commands.kRemovePlayer);                
		connection.Send(new ObjectMessageRequest(obj, connection.LastJoinedRoom));
    }

    private void RemoveRemotePlayer(User user)
    {   
        var playerController = this._currentImpl.RemotePlayers[user];
        playerController.Dispose();
        this._currentImpl.RemotePlayers.Remove(user);       
    }
}