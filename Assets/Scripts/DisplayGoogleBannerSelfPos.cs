using UnityEngine;
using System.Collections;

public class DisplayGoogleBannerSelfPos : MonoBehaviour {
	
	private UIWidget SelfWidget;
	private float ScrHeight;
	private float ScrDPI;
	
	private Vector2 StoredAlignmentAnchors = Vector2.zero;
	
	void Awake()
	{
		// Get the widget attached to this gameobject
		SelfWidget = GetComponent<UIWidget>();
		
		// Get the height of the screen resolution
		ScrHeight = (float)Screen.height;
		
		// Get the screen dots per inch
		ScrDPI = JarLoader.GetDensity();
		
		StoredAlignmentAnchors = new Vector2(SelfWidget.leftAnchor.absolute, SelfWidget.topAnchor.absolute);
	}
	
	// We don't except anyone to have a device which changes resolution mid-game so just setup the ad size OnEnable()
	void OnEnable()
	{
		// If the screen DPI can't be calculated then we can't display an ad due to it not being possible to dermine the scale of everything else on the screen
		if(ScrDPI <= 0){
			GoogleAnalytics.Instance.LogError("DPI checks failed! Falling back to default!", false);
			ScrDPI = 160f;
		}
		
		// Update the anchors already set before taking this script into account (Bug fix)
		SelfWidget.UpdateAnchors();
		
		// Now if we don't do this then for no apparent reason it will double the right and top anchors (Bug fix)
		SelfWidget.leftAnchor.absolute = Mathf.RoundToInt(StoredAlignmentAnchors.x);
		SelfWidget.topAnchor.absolute = Mathf.RoundToInt(StoredAlignmentAnchors.y);
		
		// We have to update the anchors again or everything breaks (Bug fix)
		SelfWidget.UpdateAnchors();
		
		// Convert 300 and 250 to literal pixel values for the current resolution
		#if UNITY_EDITOR
		SelfWidget.width = 300;
		SelfWidget.height = 250;
		#else
		SelfWidget.width = Mathf.RoundToInt(((300 * SelfWidget.root.pixelSizeAdjustment) * (ScrDPI / 160f)) * SelfWidget.anchorCamera.orthographicSize);
		SelfWidget.height = Mathf.RoundToInt(((250 * SelfWidget.root.pixelSizeAdjustment) * (ScrDPI / 160f)) * SelfWidget.anchorCamera.orthographicSize);
		#endif
		
		// If we don't wait a frame before calling CalculateAbsoluteWidgetBounds then it will lie and give us the wrong values
		StartCoroutine(CalculateAdPosition());
	}
	
	private IEnumerator CalculateAdPosition()
	{
		// The UI with this might have just been instantiated so we should wait for a bit to be sure (or we'll be reading invalid values)
		yield return new WaitForFixedUpdate();
		yield return new WaitForEndOfFrame();
		
		// Get the absolute bounds of the widget
		Bounds SelfBounds = NGUIMath.CalculateAbsoluteWidgetBounds(SelfWidget.transform);
		
		// Get the literal pixel position of the left and top bounds of the widget
		Vector3 ScreenPoint = SelfWidget.anchorCamera.WorldToScreenPoint(new Vector3(SelfBounds.min.x, SelfBounds.max.y, 0f));
		Vector3 RemainingScreenPoint = SelfWidget.anchorCamera.WorldToScreenPoint(SelfBounds.min);
		
		// Calculate the literal pixel xPos based from the right edge
		int xPos = Mathf.RoundToInt(ScreenPoint.x);
		
		// Calculate the literal pixel yPos based from the top edge
		int yPos = Mathf.RoundToInt(ScrHeight - ScreenPoint.y);
		
		/*if(RemainingScreenPoint.x < (100f * SelfWidget.root.pixelSizeAdjustment) * (ScrDPI / 160f)){
			SelfWidget.width = 300;
			SelfWidget.height = 300;
			
			GoogleAnalytics.Instance.LogError("Not enough space for ad and buttons, no ad displayed!", false);
			return false;
		}*/
		// Note: On some devices with .5 DPI the ad size is actually 299x249 so remember to leave a few pixels for error (As if your ad goes off the screen by even 1 pixel no ad will be shown at all!)
		AdMob_Manager.Instance.RepositionBanner(xPos, yPos);
		AdMob_Manager.Instance.ShowBanner();
	}
}