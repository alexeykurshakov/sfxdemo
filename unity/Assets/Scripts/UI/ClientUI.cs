using UnityEngine;

public class ClientUI : MonoBehaviour
{
    private PlayerController _localPlayer;

    public void ChangePlayerMaterial(int numMaterial)
    {
        if (_localPlayer == null)
            return;

        _localPlayer.ChangePlayerMaterial(numMaterial);     
    }

    public void ChangePlayerModel(int numModel)
    {
        if (_localPlayer == null)
            return;

        _localPlayer.ChangePlayerModel(numModel);
        _localPlayer = null;
    }

    private void OnGUI()
    {
        if (_localPlayer == null)
        {
            _localPlayer = GameManager.Instance.GetPlayerController(SmartFoxConnection.Connection.MySelf);
        }

        // We basically just draw some buttons to change color and model of our player
        GUILayout.BeginArea(new Rect(0, 0, 150, 400));
        GUILayout.BeginVertical();

        GUILayout.Label("Select your model");

        if (GUILayout.Button("Cube"))
        {
            ChangePlayerModel(0);
        }

        if (GUILayout.Button("Sphere"))
        {
            ChangePlayerModel(1);
        }

        if (GUILayout.Button("Capsule"))
        {
            ChangePlayerModel(2);
        }

        GUILayout.Label("Select your color");

        if (GUILayout.Button("Blue"))
        {
            ChangePlayerMaterial(0);
        }

        if (GUILayout.Button("Green"))
        {
            ChangePlayerMaterial(1);
        }

        if (GUILayout.Button("Red"))
        {
            ChangePlayerMaterial(2);
        }

        if (GUILayout.Button("Yellow"))
        {
            ChangePlayerMaterial(3);
        }

        GUILayout.EndVertical();
        GUILayout.EndArea();
    }
}