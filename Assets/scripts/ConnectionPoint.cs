using UnityEngine;
using System.Collections;

public class ConnectionPoint : MonoBehaviour 
{
    [Tooltip( "If you care whether a door or hallway is connected here")]
	public ConnectionType connectionType;

    [Tooltip( "If you care whether the player enters or exits the room here (connection points can't be all one type)")]
    public DoorType doorType;

    [Tooltip( "Is this connection point used by the room? Need at least one" )]
    public bool isUsed = true;

    [Header( "Don't Change Me! Used for verification" )]
    public float connectionX, connectionZ;

    [HideInInspector]
    public Room owner;

    [HideInInspector]
    public Room connectedTo;

    [HideInInspector]
    public Connector connector;

    public bool isConnected { get { return connectedTo; } }

    public enum ConnectionType {
        ANY,
        HALLWAY,
        DOOR
    }

    public enum DoorType {
        ANY,
        ENTRANCE,
        EXIT
    }
}
