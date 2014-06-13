using UnityEngine;
using System.Collections;
using System.Collections.Generic;

// for tree traversal of all the rooms in the world
public class Room : MonoBehaviour
{
    public string roomName = "REPLACE";
    public string authorName = "ME";

	List<ConnectionPoint> connections = new List<ConnectionPoint>();
    public Bounds bounds { get; private set; }

	void Awake() {
		// build the list of exits / connection points.
		// there must be an equal number of each
		foreach( var cp in GetComponentsInChildren<ConnectionPoint>() )
		{
            if ( cp.isUsed ) {
                cp.owner = this;
                connections.Add( cp );
            }
		}
        CalcBounds();
        Validate();
	}

    void Validate() {
        if ( connections.Count == 0 )
            Debug.LogError( roomName + " : No connections marked as inUse" );

        int exits = 0;
        int entrances = 0;

        foreach ( var connection in connections ) {
            if ( connection.doorType == ConnectionPoint.DoorType.ENTRANCE )
                entrances++;
            else if ( connection.doorType == ConnectionPoint.DoorType.EXIT )
                exits++;

            if ( Mathf.Abs( connection.connectionX - connection.transform.localPosition.x ) > Mathf.Epsilon ||
                Mathf.Abs( connection.connectionZ - connection.transform.localPosition.z ) > Mathf.Epsilon )
                Debug.LogError( roomName + " : Exit was moved in the x or z dimensions" );
        }
        if ( exits == connections.Count || entrances == connections.Count )
            Debug.LogError( roomName + " :Connections are all marked as entrances or all marked as exits" );
    }

    public ConnectionPoint[] GetConnections() {
        return connections.ToArray();
    }

    public Room[] GetConnectedRooms() {
        var rooms = new List<Room>();
        foreach ( var connector in connections ) {
            if ( connector.isConnected )
                rooms.Add( connector.connectedTo );
        }
        return rooms.ToArray();   
    }

	public ConnectionPoint GetOpenConnection()
	{
        foreach ( var point in connections ) {
            if ( !point.isConnected )
                return point;
        }

		return null;
	}

	public void ConnectRoom( ConnectionPoint cp, Connector connector, Room connectedRoom ){
		if ( cp.isConnected )
            Debug.LogError( "Already room connected at " + cp );

        if ( !connections.Contains( cp ) )
            Debug.LogError( this + " doesn't contain connection " + cp );

        cp.connectedTo = connectedRoom;
        cp.connector = connector;
	}

    public void DisconnectRoom( Room connectedRoom ) {
        foreach ( var connection in connections ) {
            if ( connection.connectedTo == connectedRoom ) {
                connection.connectedTo = null;
                if ( connection.connector ) {
                    Destroy( connection.connector.gameObject );
                    connection.connector = null;
                }
                return;
            }
        }
        Debug.LogWarning( "Couldn't find room " + connectedRoom + " to remove" );
    }

    public void CalcBounds() {
        // since unity bounds are broken?

        bounds = new Bounds( transform.position + Vector3.up * .5f, Vector3.one );
        foreach ( var rend in GetComponentsInChildren<Renderer>() ) {
            Vector3 newMin = new Vector3(
                Mathf.Min( bounds.min.x, rend.bounds.min.x ),
                Mathf.Min( bounds.min.y, rend.bounds.min.y ),
                Mathf.Min( bounds.min.z, rend.bounds.min.z )
                );
            Vector3 newMax = new Vector3(
                Mathf.Max( bounds.max.x, rend.bounds.max.x ),
                Mathf.Max( bounds.max.y, rend.bounds.max.y ),
                Mathf.Max( bounds.max.z, rend.bounds.max.z )
                );
            Vector3 centre = ( newMin + newMax ) * .5f;
            Vector3 size = newMax - newMin;
            bounds = new Bounds( centre, size );
        }
    }
}
