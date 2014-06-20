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
    public Connector[] hallwayPrefabs;

    public GameObject player;

    public static Room currentRoom { get; private set; }
	int nearLayer, farLayer, far2Layer;
    bool enteringRoom;

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
		var newRoom = (Room)Instantiate( roomPrefabs[Random.Range(0, roomPrefabs.Length)] );
        newRoom.gameObject.SetActive( true );
		EnteredRoom( newRoom );
	}

	void AddRoomsToDepth( Room startingRoom, int depth ){
        foreach ( var currentRoomConnection in startingRoom.GetConnections() ) {
            Room exitRoom;
            if ( currentRoomConnection.isConnected )
                exitRoom = currentRoomConnection.connectedTo;
            else {
                // instance the next room
                exitRoom = (Room)Instantiate( GetRoom() );
                exitRoom.gameObject.SetActive( true );

                // need to find the connection point
                var cps = exitRoom.GetConnections();
                Doorway newRoomConnectionPoint = cps[Random.Range( 0, cps.Length )];
                
                // rotate / translate the new room and connector to match the transform of the existing exit
                Transform newRoomConnectionTransform = newRoomConnectionPoint.transform;
                currentRoomConnection.transform.Rotate( 0, 180, 0 );

                // need to do this a few times to make sure all the axis line up
                // end with the up axis, as that is the most important one. I can't help but feel like there is a better way to handle this
                exitRoom.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.forward, currentRoomConnection.transform.forward );
                exitRoom.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.right, currentRoomConnection.transform.right );
                exitRoom.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.up, currentRoomConnection.transform.up );
                // move the new room such that the transform matches with the last connection point
                exitRoom.transform.position += ( currentRoomConnection.transform.position - newRoomConnectionTransform.position );

                // get a connector, sometimes
                Connector connector = null;
                if ( Random.value < .5f ) {
                    connector = (Connector)Instantiate( GetConnector() );
                    bool startEnd1 = Random.value < .5f;
                    Transform connectorEndTransform = startEnd1 ? connector.end1 : connector.end2;
                    Transform connectorOtherEndTransform = startEnd1 ? connector.end2 : connector.end1;
                    connector.transform.rotation *= Quaternion.FromToRotation( connectorEndTransform.forward, currentRoomConnection.transform.forward );
                    connector.transform.rotation *= Quaternion.FromToRotation( connectorEndTransform.right, currentRoomConnection.transform.right );
                    connector.transform.rotation *= Quaternion.FromToRotation( connectorEndTransform.up, currentRoomConnection.transform.up );

                    connector.transform.position += ( currentRoomConnection.transform.position - connectorEndTransform.position );
                    exitRoom.transform.position += ( connectorOtherEndTransform.position - newRoomConnectionTransform.position );
                }
                    
                exitRoom.CalcBounds();

                // unrotate the point
                currentRoomConnection.transform.Rotate( 0, 180, 0 );
                //			newRoomConnectionPoint.renderer.enabled = false;
                // link up the rooms in the room tree

                startingRoom.ConnectRoom( currentRoomConnection, connector, exitRoom );
                exitRoom.ConnectRoom( newRoomConnectionPoint, connector, startingRoom );
            }

			if (depth > 0)
				AddRoomsToDepth( exitRoom, depth - 1 );
		}
	}

	Room GetRoom()
	{
        return roomPrefabs[Random.Range( 0, roomPrefabs.Length )];
	}

    Connector GetConnector() {
        return hallwayPrefabs[Random.Range( 0, hallwayPrefabs.Length )];
    }

	// Update is called once per frame
	void Update ()
	{
		// see if the player has entered a new room
        Vector3 point = Camera.main.transform.position;
        if ( !currentRoom.bounds.Contains( point ) ) {
            Debug.Log( "Player leaving current room" );
            Debug.Log( currentRoom.bounds );
            Debug.Log( point );
            foreach ( var connection in currentRoom.GetConnections() ) {
                Debug.Log( connection.connectedTo.bounds );
                if ( connection.connectedTo.bounds.Contains( point ) ) {
                    Debug.Log( "Player now in " + connection.connectedTo );
                    EnteredRoom( connection.connectedTo );
                }
            }
        }
	}

	// traverse the list of rooms, setting their layers based on how far they are from the centered room
	List<Room> visited = new List<Room>();
	void EnteredRoom(Room centeredRoom)
	{
        enteringRoom = true;
        if ( currentRoom )
            currentRoom.SendMessage( "RoomLeft", player, SendMessageOptions.DontRequireReceiver );
		currentRoom = centeredRoom;
        AddRoomsToDepth( currentRoom, depthToPreGen );

		visited.Clear ();
		SetLayersOnTree (centeredRoom, 0);
        currentRoom.SendMessage( "RoomEntered", player, SendMessageOptions.DontRequireReceiver );
	}

	void SetLayersOnTree(Room current, int distance)
	{
        visited.Add( current );
        var unvisitedConnections = new List<Doorway>();
        foreach ( var connection in current.GetConnections() ) {
            if ( !visited.Contains( connection.connectedTo ) )
                unvisitedConnections.Add( connection );
        }

        current.gameObject.name = "distance: " + distance;
        current.gameObject.SetActive( true );

        int layer;
        if ( distance == 0 )
            layer = nearLayer;
        else if ( distance == 1 )
            layer = farLayer;
        else
            layer = far2Layer;

        SetLayerRecursively( current.gameObject, layer );
        SetColliderState( current.gameObject, distance < 2 );
        SetLightState( current.gameObject, distance == 0 );
        foreach ( var connection in unvisitedConnections ) {
            if ( connection.isConnected ) {

                if ( distance <= depthToPreGen ) {
                    if ( connection.connector ) {
                        SetLayerRecursively( connection.connector.gameObject, layer );
                        SetColliderState( connection.connector.gameObject, distance < 2 );
                        SetLightState( connection.connector.gameObject, distance == 0 );
                    }
                    SetLayersOnTree( connection.connectedTo, distance + 1 );
                }
                else {
                    var roomObj = connection.connectedTo.gameObject;
                    current.DisconnectRoom( connection.connectedTo );
                    Destroy( roomObj );
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

    void SetLightState( GameObject go, bool state ) {
        foreach ( var l in go.GetComponentsInChildren<Light>() )
            l.enabled = state;
    }
}
