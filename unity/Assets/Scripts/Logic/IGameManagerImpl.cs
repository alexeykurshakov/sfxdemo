using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Sfs2X.Entities;
using UnityEngine;
using System.Collections;

public interface IGameManagerImpl 
{
    PlayerController LocalPlayer { get; set; }

    Dictionary<User, PlayerController> RemotePlayers { get; }

    Dictionary<int, ObjectController> ObjectControllers { get; }	
}
