//================================================================
// Road Creator - (C) 2015 - Jonathan Cel
//
// Build: 25_09_2015
//
// Internal i6 use only - for fuck's sake it's not
// ready so please don't go sharing this yet.
//
// J. I like paste.
//
//
//
// This class more or less handles the "using UnityEditor;" stuff
// so it doesn't have to be compiled into the main .dll.
//
//

using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
	using UnityEditor;
#endif

[ExecuteInEditMode]
public class RoadCreator : MonoBehaviour {
	public void UnwrapMesh( Mesh inMesh ){
#if UNITY_EDITOR
		Unwrapping.GenerateSecondaryUVSet( inMesh );
#endif	
	}
}
