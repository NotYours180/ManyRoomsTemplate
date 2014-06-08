using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LevelGen : MonoBehaviour {

    /// <summary>
    /// how far ahead of current room do we generate rooms?
    /// </summary>
	public int depthToPreGen = 2; 

	// the rooms that we will build the building out of
	public GameObject[] levelBits;

	List<GameObject> openConnections;
	List<GameObject> rooms;
	GameObject nullObj;
	Room currentRoom;
	int nearLayer, farLayer, far2Layer;
	// Use this for initialization
	void Start ()
	{
		nearLayer = LayerMask.NameToLayer ("Default");
		farLayer = LayerMask.NameToLayer ("farRoom");
		far2Layer = LayerMask.NameToLayer ("farRoom2");

		nullObj = new GameObject("null");
		// just place a bunch of hallways?
		openConnections = new List<GameObject>();
		rooms = new List<GameObject>();

		// instantiate all the level bits so that we can test their children
		for(int i=0;i<levelBits.Length;i++){
			levelBits[i] = (GameObject)Instantiate(levelBits[i]);
		}

		// add the first hallway, and its connections
		GameObject firstRoom = (GameObject)Instantiate(levelBits[Random.Range(0, levelBits.Length)]);
		rooms.Add(firstRoom);
		currentRoom = new Room (firstRoom);
		EnteredRoom (currentRoom);

		// disable all the level bits, because they cause intersection issues
		for(int i=0;i<levelBits.Length;i++){
			levelBits [i].SetActive (false);
		}

	}
	void AddRoomsToDepth (Room startingRoom, int depth){
        for ( int i = 0; i < startingRoom.connections.Count; i++ ) {
            Room exitRoom;
            if ( startingRoom.IsConnectionOpen( i ) ) {
                ConnectionPoint currentRoomConnection = startingRoom.connections[i];
                //			currentRoomConnection.renderer.enabled = false;
                GameObject nextRoomPrefab = GetRoomWithConnector( currentRoomConnection.t );
                // instance the next room
                GameObject newRoomGO = (GameObject)Instantiate( nextRoomPrefab );
                newRoomGO.SetActive( true );
                exitRoom = new Room( newRoomGO );
                // need to find the connection point again
                ConnectionPoint[] cps = newRoomGO.GetComponentsInChildren<ConnectionPoint>();
                cps = TensionExtensions.Shuffle( cps );
                ConnectionPoint newRoomConnectionPoint = cps[0];
                for ( int j = 0; j < cps.Length; j++ ) {
                    newRoomConnectionPoint = cps[j];
                    if ( newRoomConnectionPoint.t == currentRoomConnection.t ) {
                        break;
                    }
                }

                // rotate / translate the new room to match the transform of the existing exit
                Transform newRoomConnectionTransform = newRoomConnectionPoint.transform;
                newRoomGO.SetActive( true );
                Quaternion OR = currentRoomConnection.transform.rotation;
                currentRoomConnection.transform.Rotate( 0, 180, 0 );

                // need to do this a few times to make sure all the axis line up
                // end with the up axis, as that is the most important one. I can't help but feel like there is a better way to handle this
                newRoomGO.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.forward, currentRoomConnection.transform.forward );
                newRoomGO.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.right, currentRoomConnection.transform.right );
                newRoomGO.transform.rotation *= Quaternion.FromToRotation( newRoomConnectionTransform.up, currentRoomConnection.transform.up );

                // unrotate the point
                currentRoomConnection.transform.Rotate( 0, 180, 0 );
                // move the new room such that the transform matches with the last connection point
                newRoomGO.transform.position += ( currentRoomConnection.transform.position - newRoomConnectionTransform.position );
                // just debug output, making sure that things aren't connected more than once			
                currentRoomConnection.GetComponent<ConnectionPoint>().connectionCount++;
                newRoomConnectionPoint.connectionCount++;
                //			newRoomConnectionPoint.renderer.enabled = false;
                // link up the rooms in the room tree

                startingRoom.ConnectRoom( currentRoomConnection, exitRoom );
                exitRoom.ConnectRoom( newRoomConnectionPoint, startingRoom );
            }
            else
                exitRoom = startingRoom.GetRoomForExit( i );

			if (depth > 0)
			{
				AddRoomsToDepth (exitRoom, depth - 1);
			}
		}
	}
	GameObject GetRoomWithConnector(ConnectionPoint.ConnectionType connectionType)
	{
		levelBits = TensionExtensions.Shuffle(levelBits);
		for(int j = 0; j<levelBits.Length;j++){
            var bit = levelBits[j];
            bit.SetActive( true );
			// look in the level bit and see if it has a valid connection point
			foreach(ConnectionPoint cp in bit.GetComponentsInChildren<ConnectionPoint>()){
				Debug.Log("connects to: "+cp.t);
				if(cp.t == connectionType){
                    bit.SetActive( false );
					return bit;
				}
			}
            bit.SetActive( false );
		}
		return nullObj;
	}
	// Update is called once per frame
	void Update ()
	{
		// see if the player has entered a new room
		foreach (BoxCollider collider in currentRoom.GetExits())
		{
			// test if the camera is inside any of room exits
			RaycastHit hitInfo;
			Vector3 point = Camera.main.transform.position;
			Vector3 center = collider.bounds.center;

			// Cast a ray from point to center
			Vector3 direction = center - point;
			Ray ray = new Ray(point, direction);
			bool hit = collider.Raycast(ray, out hitInfo, direction.magnitude);
			if (!hit)
			{
				EnteredRoom (currentRoom.GetRoomForExit (collider));
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
        current.GetGameObject().name = "distance: " + distance;
        current.GetGameObject().SetActive( true );
        switch ( distance ) {
        case 0:
            SetLayerRecursively( current.GetGameObject(), nearLayer );
            SetColliderState( current.GetGameObject(), true );
            break;
        case 1:
            SetLayerRecursively( current.GetGameObject(), farLayer );
            SetColliderState( current.GetGameObject(), false );
            break;
        default:
            SetLayerRecursively( current.GetGameObject(), far2Layer );
            SetColliderState( current.GetGameObject(), false );
            break;
        //				current.GetGameObject ().SetActive (false);
        //				break;
        }
	
        visited.Add (current);
        var rooms = new Room[current.GetRooms().Count];
        current.GetRooms().CopyTo( rooms, 0 ); // get around deleting during foreach
        foreach ( Room r in rooms ) {
            if ( !visited.Contains( r ) ) {
                if ( distance <= depthToPreGen )
                    SetLayersOnTree( r, distance + 1 );
                else
                    current.RemoveRoom( r );
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
