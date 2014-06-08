using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// for tree traversal of all the rooms in the world
public class Room
{
	GameObject root;
	Dictionary<BoxCollider, Room> exitRooms = new Dictionary<BoxCollider, Room>();
	List<BoxCollider> exits = new List<BoxCollider>();
	List<ConnectionPoint> connections = new List<ConnectionPoint>();

	public Room(GameObject _root)
	{
		root = _root;
		// build the list of exits / connection points.
		// there must be an equal number of each
		foreach(ConnectionPoint cp in root.GetComponentsInChildren<ConnectionPoint>())
		{
			connections.Add(cp);
			exits.Add (cp.GetComponent<BoxCollider> ());
		}
	}
	public Dictionary<BoxCollider, Room>.KeyCollection GetExits()
	{
		return exitRooms.Keys;
	}
	public Dictionary<BoxCollider, Room>.ValueCollection GetRooms()
	{
		return exitRooms.Values;
	}

	public Room GetRoomForExit(BoxCollider bc)
	{
		return exitRooms [bc];
	}
	public ConnectionPoint GetOpenConnection()
	{
		for (int i = 0; i < exits.Count; i++)
		{
			if (!exitRooms.ContainsKey (exits [i]))
			{
				Debug.Log (i);
				return connections [i];
			}
		}
		return null;
	}
	public void ConnectRoom(ConnectionPoint cp, Room connectedRoom){
		for (int i = 0; i < connections.Count; i++)
		{
			if (connections [i] == cp)
			{
				exitRooms [exits [i]] = connectedRoom;
				return;
			}
		}
		Debug.Log ("couldn't find connection");
	}
	public GameObject GetGameObject(){
		return root;
	}
}
