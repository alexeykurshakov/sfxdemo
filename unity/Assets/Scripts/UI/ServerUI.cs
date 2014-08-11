using UnityEngine;

public class ServerUI : MonoBehaviour
{       
    private void OnGUI()
    {
		var currentColor = GUI.color;

		GUI.color = Color.blue;
		GUILayout.Label(string.Format ("Players: {0}", GameManager.Instance.OtherPlayers.Count));      

		GUI.color = currentColor;
    }
}