using UnityEngine;
using System.Collections;

public class WaterCheker : MonoBehaviour {

	float curTime = 0f;
	bool isEnter = false;
	void OnTriggerEnter(Collider other) {
		if(other.name.Equals("Water"))
		{
			isEnter = true;
			curTime = 0f;
			//Game.instance.OnTouchZone();
		}
	}

	void Update()
	{
		if(isEnter)
		{
			curTime += Time.deltaTime;
			if (curTime >= 5f) {
				isEnter = false;
				curTime = 0f;
				Game.instance.OnTouchZone ();
			}
		}
	}

	void OnTriggerExit(Collider other) {
		if (other.name.Equals ("Water")) {
			isEnter = false;
		}
	}
}
