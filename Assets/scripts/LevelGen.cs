using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelGen : MonoBehaviour {
    /// <summary>
    /// how far ahead of current room do we generate rooms?
    /// </summary>
	public int depthToPreGen = 2; 

	// the rooms that we will build the building out of
	public Room[] roomPrefabs;

	Room currentRoom;
	int nearLayer, farLayer, far2Layer;

	// Use this for initialization
	void Start ()
	{
		nearLayer = LayerMask.NameToLayer ("Default");
		farLayer = LayerMask.NameToLayer ("farRoom");
		far2Layer = LayerMask.NameToLayer ("farRoom2");

		// just place a bunch of hallways?

		// instantiate all the level bits so that we can test their children
		for( int i=0; i<roomPrefabs.Length; i++ ) {
			roomPrefabs[i] = (Room)Instantiate(roomPrefabs[i]);
		}

        // disable all the level bits, because they cause intersection issues
        for ( int i = 0; i < roomPrefabs.Length; i++ ) {
            roomPrefabs[i].gameObject.SetActive( false );
        }

		// add the first room, and its connections
		currentRoom = (Room)Instantiate( roomPrefabs[Random.Range(0, roomPrefabs.Length)] );
        currentRoom.gameObject.SetActive( true );
		EnteredRoom( currentRoom );
	}

	void AddRoomsToDepth( Room startingRoom, int depth ){
        foreach ( var currentRoomConnection in startingRoom.GetConnections() ) {
            Room exitRoom;
            if ( currentRoomConnection.isConnected )
                exitRoom = currentRoomConnection.connectedTo;
            else {
                var nextRoomPrefab = GetRoomToConnectWith( currentRoomConnection.connectionType );
                // instance the next room
                exitRoom = (Room)Instantiate( nextRoomPrefab );
                exitRoom.gameObject.SetActive( true );

                // need to find the connection point
                var cps = exitRoom.GetConnections();
                cps.Shuffle();
                ConnectionPoint newRoomConnectionPoint = null;
                foreach ( var otherConn in cps ) {
                    if ( otherConn.doorType != ConnectionPoint.DoorType.EXIT && 
                        ( currentRoomConnection.connectionType == ConnectionPoint.ConnectionType.ANY || otherConn.connectionType == ConnectionPoint.ConnectionType.ANY || otherConn.connectionType == currentRoomConnection.connectionType ) ) {
                        newRoomConnectionPoint = otherConn;
                        break;
                    }
                }
                if ( newRoomConnectionPoint == null )
                    Debug.LogError( "No valid connections found" );

                // rotate / translate the new room to match the transform of the existing exit
                Transform newRoomConnectionTransform = newRoomConnectionPoint.transform;
                Quaternion OR = currentRoomConnection.transform.rotation;
                currentRoomConnection.transform.Rotate( 0, 180, 0 );

                // need to do this a few times to make sure all the axis line up
                // end with the up axis, as that is the most important one. I can't help but feel like there is a better way to handle this
                exitRoom.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.forward, currentRoomConnection.transform.forward );
                exitRoom.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.right, currentRoomConnection.transform.right );
                exitRoom.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.up, currentRoomConnection.transform.up );

                // unrotate the point
                currentRoomConnection.transform.Rotate( 0, 180, 0 );
                // move the new room such that the transform matches with the last connection point
                exitRoom.transform.position += ( currentRoomConnection.transform.position - newRoomConnectionTransform.position );
                
                // TODO add connector (door or hallway) & position new room properly

                //			newRoomConnectionPoint.renderer.enabled = false;
                // link up the rooms in the room tree

                startingRoom.ConnectRoom( currentRoomConnection, exitRoom );
                exitRoom.ConnectRoom( newRoomConnectionPoint, startingRoom );
            }

			if (depth > 0)
				AddRoomsToDepth( exitRoom, depth - 1 );
		}
	}

	Room GetRoomToConnectWith(ConnectionPoint.ConnectionType connectionType)
	{
        roomPrefabs.Shuffle();
		for(int j = 0; j < roomPrefabs.Length; j++) {
            var bit = roomPrefabs[j];
            if ( connectionType == ConnectionPoint.ConnectionType.ANY )
                return bit;

            var connectors = bit.GetConnections();
			// look in the level bit and see if it has a valid connection point
			foreach( var cp in connectors ) {
				Debug.Log("connects to: "+cp.connectionType);
				if ( cp.connectionType == ConnectionPoint.ConnectionType.ANY ||
                    cp.connectionType == connectionType ){
					return bit;
				}
			}
		}
		return null;
	}

	// Update is called once per frame
	void Update ()
	{
		// see if the player has entered a new room
        RaycastHit hitInfo;
        Vector3 point = Camera.main.transform.position;
        foreach ( var connection in currentRoom.GetConnections() )
		{
			// test if the camera is inside any of room exits
			Vector3 center = connection.trigger.bounds.center;

			// Cast a ray from point to center
			Vector3 direction = center - point;
			Ray ray = new Ray(point, direction);
			bool hit = connection.trigger.Raycast(ray, out hitInfo, direction.magnitude);
			if (!hit)
			{
                if ( !connection.isConnected )
                    Debug.LogError( "Player in connection that isn't connected" );

				EnteredRoom( connection.connectedTo );
				break;
			}
		}
	}

	// traverse the list of rooms, setting their layers based on how far they are from the centered room
	List<Room> visited = new List<Room>();
	void EnteredRoom(Room centeredRoom)
	{
		currentRoom = centeredRoom;
        AddRoomsToDepth( currentRoom, depthToPreGen );

		visited.Clear ();
		SetLayersOnTree (centeredRoom, 0);
	}

	void SetLayersOnTree(Room current, int distance)
	{
        current.gameObject.name = "distance: " + distance;
        current.gameObject.SetActive( true );
        switch ( distance ) {
        case 0:
            SetLayerRecursively( current.gameObject, nearLayer );
            SetColliderState( current.gameObject, true );
            break;
        case 1:
            SetLayerRecursively( current.gameObject, farLayer );
            SetColliderState( current.gameObject, false );
            break;
        default:
            SetLayerRecursively( current.gameObject, far2Layer );
            SetColliderState( current.gameObject, false );
            break;
        //				current.GetGameObject ().SetActive (false);
        //				break;
        }
	
        visited.Add (current);
        var rooms = current.GetConnectedRooms();
        foreach ( Room r in rooms ) {
            if ( !visited.Contains( r ) ) {
                if ( distance <= depthToPreGen )
                    SetLayersOnTree( r, distance + 1 );
                else {
                    current.DisconnectRoom( r );
                    Destroy( r.gameObject );
                }
            }
        }
	}

	public void SetLayerRecursively(GameObject go, int layer)
	{
		go.layer = layer;
		foreach (Transform t in go.transform)
		{
			if(t.gameObject != go)
				SetLayerRecursively (t.gameObject, layer);
		}
	}

	public void SetColliderState(GameObject go, bool state){
		foreach (Collider c in go.GetComponentsInChildren<Collider>())
		{
			c.enabled = state;
		}
	}
}
