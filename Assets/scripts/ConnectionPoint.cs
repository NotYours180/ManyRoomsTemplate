using UnityEngine;
using System.Collections;

public class ConnectionPoint : MonoBehaviour {
	public enum ConnectionType
	{
		HALLWAY,
		DOOR
	}
	public ConnectionType t;
	public int connectionCount = 0;
}
