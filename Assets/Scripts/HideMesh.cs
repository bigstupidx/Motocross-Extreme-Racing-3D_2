using UnityEngine;
using System.Collections;

/// <summary>
/// Destroys test items regardless of debug mode
/// </summary>

public class HideMesh : MonoBehaviour {
	
	void Start () {
		if (this.light ) this.light.enabled = false;
		if (this.renderer) this.renderer.enabled = false;
		GameObject.DestroyImmediate(this.renderer);
		GameObject.DestroyImmediate(this);
	}

}
