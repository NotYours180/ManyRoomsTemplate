/* Exporter.cs
 * Copyright Eddie Cameron 2014
 * ----------------------------
 *
 */

using UnityEngine;
using UnityEditor;
using System.Collections;
using System.Collections.Generic;

public class Exporter : EditorWindow {
    Room roomPrefab;
    bool roomValid = true;
    Connector connectorObj;

    // Add menu named "My Window" to the Window menu
    [MenuItem( "ManyHouses/Export" )]
    static void Init() {
        // Get existing open window or if none, make a new one:
        var window = (Exporter)EditorWindow.GetWindow<Exporter>();
    }

    void OnGUI() {
        GUILayout.Label( "Room Exporter", EditorStyles.boldLabel );
        GUILayout.Label( "Export your room or connector prefab" );

        roomPrefab = (Room)EditorGUILayout.ObjectField( "Room", roomPrefab, typeof( Room ), false );
        if ( !roomValid ) {
            EditorGUILayout.LabelField( "Room didn't validate, see console" );
        }

        if ( roomPrefab != null ) {
            if ( GUILayout.Button( "Validate & export room" ) ) {
                var roomObj = (Room)PrefabUtility.InstantiatePrefab( roomPrefab );
                roomValid = roomObj.Validate();
                //DestroyImmediate( roomObj.gameObject );

                if ( roomValid ) {
                    var roomPaths = new List<string>();
                    roomPaths.Add( AssetDatabase.GetAssetPath( roomPrefab ) );
                    foreach ( var script in roomPrefab.GetComponents<MonoBehaviour>() ) {
                        if ( script.GetType() != typeof( Room ) ) {
                            roomPaths.Add( AssetDatabase.GetAssetPath( MonoScript.FromMonoBehaviour( script ) ) );
                        }
                    }

                    AssetDatabase.ExportPackage( roomPaths.ToArray(), roomPrefab.roomName + ".unitypackage", UnityEditor.ExportPackageOptions.IncludeDependencies );
                    Debug.Log( "Package exported to the project folder!" );
                }
            }
        }
    }
}