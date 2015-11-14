using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CustomEditor(typeof(RoadBit))]
public class RoadBitEditor : Editor {
	
	//=========================== UI Menu Stuff

	[MenuItem("RoadCreator/ExportRoads")]
	public static void ExportRoads(){
		foreach( GameObject go in Selection.gameObjects ){
			exportSub( go.transform );
		}
	}
	
	public static void exportSub( Transform inTransform ){
		
		if ( inTransform == null ) return;
		
		MeshFilter mf = inTransform.GetComponent<MeshFilter>();
		if ( mf != null ){
			Mesh m = mf.sharedMesh;
			if ( m != null ){
				if ( m.name.IndexOf("road_") == 0 && m.name.IndexOf(".") > 0 ){
					
					string GUID = AssetDatabase.CreateFolder( "Assets", "pack_roads" );
					string newFolderPath = AssetDatabase.GUIDToAssetPath( GUID );
					if ( AssetDatabase.Contains( m ) ){
						Debug.LogWarning("Asset Database already contains " + m.name );
					}else{
						AssetDatabase.CreateAsset( m, "Assets/pack_roads/" + m.name + ".asset" );
						AssetDatabase.SaveAssets();
					}
					
				}
			}
		}
		
		foreach( Transform t in inTransform ){
			exportSub(t);
		}
		
	}
	
	//========================== Inspector / Scene stuff
	
	public Vector3 lastPos = Vector3.zero;
	public Quaternion lastRot = Quaternion.identity;
	
	public static RoadBit targetedObject = null;
	
	public override void OnInspectorGUI(){
		
		if ( GUILayout.Button("TARGET THIS OBJECT") ){
			targetedObject = (RoadBit)target;
			validateUpdate = true;
		}
		
		if ( targetedObject != null ){
			GUILayout.Label("Got targ obj: " + targetedObject );
			
			RoadBit targ = (RoadBit)target;
			
			if ( targetedObject !=  targ ){
				
				if ( GUILayout.Button("Add targeted to end...") ){
					targ.nextRoad = targetedObject;
					targetedObject.lastRoad = targ;
					targetedObject.updateAll();
					validateUpdate = true;
					targetedObject = null;
				}
				
				if ( GUILayout.Button("Add targeted to start...") ){
					targetedObject.nextRoad = targ;
					targ.lastRoad = targetedObject;
					targetedObject.updateAll();
					validateUpdate = true;
					targetedObject = null;
				}
				
			}
			
		}
		
		if ( GUILayout.Button("Lightmap Everything") ){
			RoadBit[] allRoads = GameObject.FindObjectsOfType<RoadBit>();
			foreach ( RoadBit r in allRoads ){
				r.updateLightmap();
			}
		}
		
		bool willUpdate = !Application.isPlaying && !((RoadBit)target).DISABLE_UPDATES;
		
		if ( willUpdate ){
			
			RoadBit targ = (RoadBit)target;
			
			if ( GUILayout.Button("Flip End Angle") ){
				targ.endRotation = Quaternion.Euler( Quaternion.identity * Vector3.up * -90 );
				validateUpdate = true;
			}
			if ( GUILayout.Button("Reset End Angle") ){
				targ.endRotation = Quaternion.Euler( Quaternion.identity * Vector3.up );
				validateUpdate = true;
			}
			
			if ( GUILayout.Button("Flip Start Angle") ){
				targ.startRotation = Quaternion.Euler( Quaternion.identity * Vector3.up * -90 );
				validateUpdate = true;
			}
			if ( GUILayout.Button("Reset Start Angle") ){
				targ.startRotation = Quaternion.Euler( Quaternion.identity * Vector3.up );
				validateUpdate = true;
			}
			
		}
		
		DrawDefaultInspector();
		
		if ( willUpdate ){
			
			RoadBit targ = (RoadBit)target;
				
			if ( GUILayout.Button("Filthy") ){
				validateUpdate = true; //update next frame
			}
			
			if ( GUILayout.Button("Flatten All") ){
				targ.startRotation = Quaternion.Euler( Quaternion.identity * Vector3.up * 90 );
				targ.endRotation = Quaternion.Euler( Quaternion.identity * Vector3.up * 90 );
				targ.endPos.Scale( new Vector3( 1, 0, 1 ) );
				validateUpdate = true; //update next frame
				//then apply flatten middle
				targ.handle1Pos.Scale( new Vector3( 1, 0, 1 ) );
				targ.handle2Pos.Scale( new Vector3( 1, 0, 1 ) );
				targ.bankStart = 0;
				targ.bankEnd = 0;
				validateUpdate = true; //update next frame
				
			}
			
			if ( GUILayout.Button("Flatten Middle") ){
				targ.handle1Pos.Scale( new Vector3( 1, 0, 1 ) );
				targ.handle2Pos.Scale( new Vector3( 1, 0, 1 ) );
				targ.bankStart = 0;
				targ.bankEnd = 0;
				validateUpdate = true; //update next frame
			}
			
			if ( GUILayout.Button("Straighten") ){
				float endDistance = targ.endPos.magnitude;
				//targ.endPos = targ.transform.forward * endDistance;
				targ.worldEndPosInverse = targ.transform.position + ( targ.transform.forward * endDistance);
				//targ.endPos.Scale(  new Vector3( 1, 0, 1 ) );
				targ.handle1Pos = (targ.endPos) * 0.33333f;
				targ.handle2Pos = (targ.endPos) * 0.66666f;
				//validateUpdate = true;
			}
			
			if ( GUILayout.Button("Update IDs") ){
				targ.updateIDs();
			}
			
			if ( GUILayout.Button("Disconnect") ){
				if ( targ.nextRoad ){
					targ.nextRoad.lastRoad = null;
					targ.nextRoad = null;
				}
				if ( targ.lastRoad ){
					targ.lastRoad.nextRoad = null;
					targ.lastRoad = null;
				}
			}
			
			if ( GUILayout.Button("Add Section") ){
				RoadBit newBit = (RoadBit)GameObject.Instantiate( targ );
				newBit.lastMesh = null; //force it to make a new mesh
				newBit.lastRoad = targ;
				targ.nextRoad = newBit;
				newBit.updateStart();
				newBit.gameObject.layer = targ.gameObject.layer;
				newBit.gameObject.tag = targ.gameObject.tag;
				newBit.transform.parent = targ.transform.parent;
				Selection.activeGameObject = newBit.gameObject;
				//float outAngle = targ.endRotation.eulerAngles.y;
				//Debug.Log("OO " + outAngle );
			}
			
			if ( GUI.changed ){
				targ.updateAll();
			}
			
			
		}
		
	}
	
	public void DupeCleanup(RoadBit targ){
		//targ.nextRoad = null;
		//targ.lastRoad = null;
		if ( targ.nextRoad != null ){
			if ( targ.nextRoad.lastRoad != targ ){
				if ( targ.nextRoad.lastRoad != null ){
					Debug.Log( "removing duped road reference (next road) " + this );
					//this is likely the one we've been duped from
					//so we should tell it to update since its mesh 
					//has just been destroyed by instantiate
					targ.nextRoad.lastRoad.updateAll( false ); 
				}
				targ.nextRoad = null;
			}
		}
		if ( targ.lastRoad != null ){
			if ( targ.lastRoad.nextRoad != targ ){
				
				if ( targ.lastRoad.nextRoad != null ){
					Debug.Log( "removing duped road reference (prev road) " + this );
					//this is likely the one we've been duped from
					//so we should tell it to update since its mesh 
					//has just been destroyed by instantiate
					targ.lastRoad.nextRoad.updateAll( false );
				}
				targ.lastRoad = null;
			}
		}
	}
	
	public void OnSceneGUI(){
		
		
		//DrawDefaultInspector();
		
		if ( !Application.isPlaying && !((RoadBit)target).DISABLE_UPDATES ){
			
			RoadBit targ = (RoadBit)target;
			
			DupeCleanup(targ);
			
			//Vector3 handleStart = UnityEditor.Handles.PositionHandle( targ.startPos, Quaternion.identity );
			
			//UnityEditor.Handles.RotationHandle( targ.startRotationInverse, targ.startPos  + Vector3.up * 6 );
			
			Quaternion rotStart = UnityEditor.Handles.RotationHandle( targ.startRotation, targ.startPos + Vector3.up * 2 );
			Quaternion rotEnd = UnityEditor.Handles.RotationHandle( targ.endRotation, targ.worldEndPosInverse );
			//Vector3 posEnd = UnityEditor.Handles.PositionHandle( targ.worldEndPos, targ.endRotation );
			//Vector3 handle1 = UnityEditor.Handles.PositionHandle( targ.worldHandle1Pos, Quaternion.identity );
			//Vector3 handle2 = UnityEditor.Handles.PositionHandle( targ.worldHandle2Pos, Quaternion.identity );
			
			Vector3 posEnd = UnityEditor.Handles.PositionHandle( targ.worldEndPosInverse, targ.endRotation );
			Vector3 handle1 = UnityEditor.Handles.PositionHandle( targ.worldHandle1PosInverse, Quaternion.identity );
			Vector3 handle2 = UnityEditor.Handles.PositionHandle( targ.worldHandle2PosInverse, Quaternion.identity );
			
			bool isFilthy = false || validateUpdate; //isDirty
			validateUpdate = false;
			
			//if ( !posEnd.Equals( targ.worldEndPos ) ){
			if ( !posEnd.Equals( targ.worldEndPosInverse ) ){
				//Debug.Log("moved end");
				targ.worldEndPosInverse = posEnd;
				isFilthy = true;
			}
			
			
			//if ( !handle1.Equals( targ.worldHandle1Pos ) ){
			if ( !handle1.Equals( targ.worldHandle1PosInverse ) ){
				//Debug.Log("moved handle 1");
				//targ.worldHandle1Pos = handle1;
				targ.worldHandle1PosInverse = handle1;
				isFilthy = true;
			}
			
			//if ( !handle2.Equals( targ.worldHandle2Pos ) ){
			if ( !handle2.Equals( targ.worldHandle2PosInverse ) ){
				//Debug.Log("moved handle 2");
				//targ.worldHandle2Pos = handle2;
				targ.worldHandle2PosInverse = handle2;
				isFilthy = true;
			}
			
			
			if ( !rotEnd.Equals( targ.endRotation )  || !rotStart.Equals( targ.startRotation) ){
				//Debug.Log("rotated handle");
				targ.endRotation = rotEnd;
				targ.startRotation = rotStart;
				isFilthy = true;
			}
			
			if ( !lastPos.Equals( targ.startPos ) || !lastRot.Equals( targ.transform.rotation ) ){
				isFilthy = true;
			}
			
			if ( GUI.changed ){
				//Debug.Log("guichange");
			}
			
			if ( isFilthy ){
				targ.updateAll();
			}
			
			drawtheLines( targ );
			
			lastPos = targ.startPos;
			lastRot = targ.transform.rotation;
			
		}
		
		
		
	}
	
	private bool validateUpdate = false;
	public void OnValidate(){
		Debug.Log("onvalidate");
		validateUpdate = true;
	}
	
	public void drawtheLines( RoadBit targ ){
		
		if ( targ == null ){
			Debug.LogError("RoadBitEditor::drawTheLines() - targ is null");
			return;
		}
		
		if ( !targ.showWireframe ){
			return;
		}
		
		//Remember to convert to world space. It's local internally.
		for ( int i = 0 ; i < targ.spineSegs.Count - 1; i++ ){
			Handles.color = Color.green;
			Vector3 startPoint = targ.spineSegs[i] + targ.startPos;
			Vector3 endPoint =  targ.spineSegs[i+1] + targ.startPos;
			UnityEditor.Handles.DrawLine( startPoint, endPoint );
		}
		
		for ( int line = 0 ; line < targ.leftSegs.Count; line++ ){
			Handles.color = Color.red;
			for ( int i = 0 ; i < targ.leftSegs[line].Count - 1; i++ ){
				Vector3 startPoint = targ.leftSegs[line][i] + targ.startPos;
				Vector3 endPoint =  targ.leftSegs[line][i+1] + targ.startPos;
				UnityEditor.Handles.DrawLine( startPoint, endPoint );
				Handles.color = Color.white;
				
			}
		}
		
		for ( int line = 0 ; line < targ.rightSegs.Count; line++ ){
			Handles.color = Color.red;
			for ( int i = 0 ; i < targ.rightSegs[line].Count - 1; i++ ){
				Vector3 startPoint = targ.rightSegs[line][i] + targ.startPos;
				Vector3 endPoint =  targ.rightSegs[line][i+1] + targ.startPos;
				UnityEditor.Handles.DrawLine( startPoint, endPoint );
				Handles.color = Color.white;
				
			}
		}
		
		//Border sections
		
		for ( int line = 0 ; line < targ.leftBorderSegs.Count; line++ ){
			Handles.color = Color.magenta;
			for ( int i = 0 ; i < targ.leftBorderSegs[line].Count - 1; i++ ){
				Vector3 startPoint = targ.leftBorderSegs[line][i] + targ.startPos + Vector3.up;
				Vector3 endPoint =  targ.leftBorderSegs[line][i+1] + targ.startPos + Vector3.up;
				UnityEditor.Handles.DrawLine( startPoint, endPoint );
				Handles.color = Color.magenta;
			}
		}
		
		for ( int line = 0 ; line < targ.rightBorderSegs.Count; line++ ){
			Handles.color = Color.magenta;
			for ( int i = 0 ; i < targ.rightBorderSegs[line].Count - 1; i++ ){
				Vector3 startPoint = targ.rightBorderSegs[line][i] + targ.startPos + Vector3.up;
				Vector3 endPoint =  targ.rightBorderSegs[line][i+1] + targ.startPos + Vector3.up;
				UnityEditor.Handles.DrawLine( startPoint, endPoint );
				Handles.color = Color.magenta;
			}
		}
		
		//Ground sections
		
		for ( int line = 0 ; line < targ.leftGroundSegs.Count; line++ ){
			Handles.color = Color.blue;
			for ( int i = 0 ; i < targ.leftGroundSegs[line].Count - 1; i++ ){
				Vector3 startPoint = targ.leftGroundSegs[line][i] + targ.startPos;
				Vector3 endPoint =  targ.leftGroundSegs[line][i+1] + targ.startPos;
				UnityEditor.Handles.DrawLine( startPoint, endPoint );
				Handles.color = Color.blue;
			}
		}
		
		for ( int line = 0 ; line < targ.rightGroundSegs.Count; line++ ){
			Handles.color = Color.blue;
			for ( int i = 0 ; i < targ.rightGroundSegs[line].Count - 1; i++ ){
				Vector3 startPoint = targ.rightGroundSegs[line][i] + targ.startPos;
				Vector3 endPoint =  targ.rightGroundSegs[line][i+1] + targ.startPos;
				UnityEditor.Handles.DrawLine( startPoint, endPoint );
				Handles.color = Color.blue;
			}
		}
		
		Handles.color = Color.white;
		//remember this is row/column
		for ( int line = 0 ; line < targ.strips.Count ; line++ ){
			
			for ( int i = 0 ; i < targ.strips[line].Count; i++ ){
				UnityEditor.Handles.DrawWireDisc( targ.strips[line][i] + targ.startPos, Vector3.up, 1f );
			}
			
		}
		
		
		for ( int line = 0 ; line < targ.tunnelStrips.Count; line++ ){
			Handles.color = Color.blue;
			for ( int i = 0 ; i < targ.tunnelStrips[line].Count - 1; i++ ){
				Vector3 startPoint = targ.tunnelStrips[line][i] + targ.startPos;
				Vector3 endPoint =  targ.tunnelStrips[line][i+1] + targ.startPos;
				UnityEditor.Handles.DrawLine( startPoint, endPoint );
				startPoint = targ.tunnelStripsOuter[line][i] + targ.startPos;
				endPoint =  targ.tunnelStripsOuter[line][i+1] + targ.startPos;
				UnityEditor.Handles.DrawLine( startPoint, endPoint );
				Handles.color = Color.blue;
			}
		}
		
		
	}
	
	
	
}
