using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using Sfs2X;
using Sfs2X.Core;
using Sfs2X.Entities;
using Sfs2X.Requests;
using Sfs2X.Logging;
using System.Net;

public class ConnectionGUI : MonoBehaviour
{
    public GUISkin sfsSkin;
    public LogLevel logLevel = LogLevel.DEBUG;
	 
    private SmartFox _smartFox;
    private string _username = string.Empty;
    private string _gameRoom = "GameRoom";
    private string _serverIp = SmartFoxConnection.ServerName;
    private string _serverPort = SmartFoxConnection.ServerPort.ToString();
    private string _errorMessage = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _isJoining = false;

    private void Start()
    {        
        _smartFox = new SmartFox(true);
        
        _smartFox.AddEventListener(SFSEvent.CONNECTION, OnConnection);
        _smartFox.AddEventListener(SFSEvent.CONNECTION_LOST, OnConnectionLost);
        _smartFox.AddEventListener(SFSEvent.LOGIN, OnLogin);
        _smartFox.AddEventListener(SFSEvent.LOGIN_ERROR, OnLoginError);
        _smartFox.AddEventListener(SFSEvent.ROOM_JOIN, OnRoomJoin);
        _smartFox.AddEventListener(SFSEvent.LOGOUT, OnLogout);

        _smartFox.AddLogListener(logLevel, OnDebugMessage);
    }
    
    private void FixedUpdate()
    {
        if (_smartFox != null)
        {
            _smartFox.ProcessEvents();
        }
    }
   
    private void OnConnection(BaseEvent evt)
    {
        var success = (bool)evt.Params["success"];
        var error = (string)evt.Params["errorMessage"];

        Debug.Log("On Connection callback got: " + success + " (error : <" + error + ">)");

        if (success)
        {
            SmartFoxConnection.Connection = _smartFox;

            _statusMessage = "Connection succesful!";
        }
        else
        {
            _statusMessage = "Can't connect to server!";
        }
    }
    private void OnConnectionLost(BaseEvent evt)
    {
        // Reset all internal states so we kick back to login screen
        Debug.Log("OnConnectionLost");
        _isJoining = false;

        _statusMessage = "Connection was lost, Reason: " + (string)evt.Params["reason"];
    }

    private void OnLogin(BaseEvent evt)
    {
        Debug.Log("Logged in successfully");

        // We either create the Game Room or join it if it exists already
        if (_smartFox.RoomManager.ContainsRoom(this._gameRoom))
        {
            _smartFox.Send(new JoinRoomRequest(this._gameRoom));

        }
        else
        {
            var settings = new RoomSettings(this._gameRoom);
            settings.MaxUsers = 40;

            _smartFox.Send(new CreateRoomRequest(settings, true));
        }
    }

    private void OnLoginError(BaseEvent evt)
    {
        Debug.Log("Login error: " + (string)evt.Params["errorMessage"]);
    }

    private void OnRoomJoin(BaseEvent evt)
    {
        Debug.Log("Joined room successfully");

        // Room was joined - lets load the game and remove all the listeners from this component
        _smartFox.RemoveAllEventListeners();
        Application.LoadLevel("Game");
    }

    private void OnLogout(BaseEvent evt)
    {
        Debug.Log("OnLogout");
        _isJoining = false;
    }

    private void OnDebugMessage(BaseEvent evt)
    {
        var message = (string)evt.Params["message"];
        Debug.Log("[SFS DEBUG] " + message);
    }

    private void OnGUI()
    {
        if (_smartFox == null) return;
        GUI.skin = sfsSkin;

        // Determine which state we are in and show the GUI accordingly
        if (!_smartFox.IsConnected)
        {
			DrawConnectionGUI();
            return;            
        }

        if (_isJoining)
        {
            DrawMessagePanelGUI("Joining.....");
            return;
        }

        DrawLoginGUI();        
    }

    private void DrawMessagePanelGUI(string message)
    {
        // Lets just quickly set up some GUI layout variables
        float panelWidth = 400;
        float panelHeight = 300;
        float panelPosX = Screen.width / 2 - panelWidth / 2;
        float panelPosY = Screen.height / 2 - panelHeight / 2;

        // Draw the box
        GUILayout.BeginArea(new Rect(panelPosX, panelPosY, panelWidth, panelHeight));
        GUILayout.Box("Message", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.BeginVertical();
        GUILayout.BeginArea(new Rect(20, 25, panelWidth - 40, panelHeight - 60), GUI.skin.customStyles[0]);

        // Center label
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.BeginVertical();
        GUILayout.FlexibleSpace();

        GUILayout.Label(message);

        GUILayout.FlexibleSpace();
        GUILayout.EndVertical();
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(20, panelHeight - 30, panelWidth - 40, 80));
        // Display client status
        GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.label);
        centeredLabelStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.Label("Client Status: " + _statusMessage, centeredLabelStyle);

        GUILayout.EndArea();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }

	private void DrawConnectionGUI()
	{
		// Lets just quickly set up some GUI layout variables
		float panelWidth = 400;
		float panelHeight = 300;
		float panelPosX = Screen.width / 2 - panelWidth / 2;
		float panelPosY = Screen.height / 2 - panelHeight / 2;
		
		// Draw the box
		GUILayout.BeginArea(new Rect(panelPosX, panelPosY, panelWidth, panelHeight));
		GUILayout.Box("Connection", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
		GUILayout.BeginVertical();
		GUILayout.BeginArea(new Rect(20, 25, panelWidth - 40, panelHeight - 60), GUI.skin.customStyles[0]);
		
		// Lets show login box!
		GUILayout.FlexibleSpace();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Server IP: ");
        _serverIp = GUILayout.TextField(_serverIp, 25, GUILayout.MinWidth(200));
		GUILayout.EndHorizontal();
		
		GUILayout.BeginHorizontal();
		GUILayout.Label("Port: ");
        _serverPort = GUILayout.TextField(_serverPort, 25, GUILayout.MinWidth(200));
		GUILayout.EndHorizontal();
		
		GUILayout.Label(_errorMessage);
		
		// Center login button
		GUILayout.BeginHorizontal();
		GUILayout.FlexibleSpace();
		if (GUILayout.Button("Connect") || (Event.current.type == EventType.keyDown && Event.current.character == '\n'))
		{
			Debug.Log("Sending connection request");
		    
		    IPAddress address;
		    if (!IPAddress.TryParse(_serverIp, out address))
		    {
		        _errorMessage = "ip address is not valid";
		        return;
		    }

		    int port;
		    if (!Int32.TryParse(_serverPort, out port))
		    {                
                _errorMessage = "port is not valid";
		        return;
		    }

            _smartFox.Connect(this._serverIp, port);			
		}
		GUILayout.FlexibleSpace();
		GUILayout.EndHorizontal();
		GUILayout.FlexibleSpace();
		
		GUILayout.EndArea();
		
		GUILayout.BeginArea(new Rect(20, panelHeight - 30, panelWidth - 40, 80));
		// Display client status
		GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.label);
		centeredLabelStyle.alignment = TextAnchor.MiddleCenter;
		
		GUILayout.Label("Client Status: " + _statusMessage, centeredLabelStyle);
		
		GUILayout.EndArea();
		GUILayout.EndVertical();
		GUILayout.EndArea();
	}
		
	private void DrawLoginGUI()
	{
		// Lets just quickly set up some GUI layout variables
		float panelWidth = 400;
		float panelHeight = 300;
        float panelPosX = Screen.width / 2 - panelWidth / 2;
        float panelPosY = Screen.height / 2 - panelHeight / 2;

        // Draw the box
        GUILayout.BeginArea(new Rect(panelPosX, panelPosY, panelWidth, panelHeight));
        GUILayout.Box("Login", GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        GUILayout.BeginVertical();
        GUILayout.BeginArea(new Rect(20, 25, panelWidth - 40, panelHeight - 60), GUI.skin.customStyles[0]);

        // Lets show login box!
        GUILayout.FlexibleSpace();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Username: ");
        _username = GUILayout.TextField(_username, 25, GUILayout.MinWidth(200));
        GUILayout.EndHorizontal();

//		GUILayout.BeginHorizontal();
//		GUILayout.Label("Room: ");
//        _gameRoom = GUILayout.TextField(this._gameRoom, 25, GUILayout.MinWidth(200));
//		GUILayout.EndHorizontal();
		
		GUILayout.Label(_errorMessage);

        // Center login button
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Login") || (Event.current.type == EventType.keyDown && Event.current.character == '\n'))
        {
            Debug.Log("Sending login request");
            if (this._username == "root")
            {
                _errorMessage = "username is not valid";
                return;
            }            
            _smartFox.Send(new LoginRequest(_username, "", SmartFoxConnection.Zone));
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        GUILayout.FlexibleSpace();

        GUILayout.EndArea();

        GUILayout.BeginArea(new Rect(20, panelHeight - 30, panelWidth - 40, 80));
        // Display client status
        GUIStyle centeredLabelStyle = new GUIStyle(GUI.skin.label);
        centeredLabelStyle.alignment = TextAnchor.MiddleCenter;

        GUILayout.Label("Client Status: " + _statusMessage, centeredLabelStyle);

        GUILayout.EndArea();
        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}