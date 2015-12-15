// i6 AdMob_Manager.cs [Updated 9th July 2015]
// Attach this script to a persistent Game Object
// Contact sean@i6.com for help and information

/* Change Log:
 * [Contact me or check our developers document if you need information on past changes!]
 *
 * 26th June 2015
 * - Script re-written from scratch check script release notes for feature list
 * 
 * 29th June 2015
 * - Added function to get largest ad size for available height and width
 * - Added pre-interstitial wait screen support
 * 
 * 30th June 2015
 * - Calling ShowBanner or ShowInterstitial now Loads a new banner or interstitial if one isn't already loaded
 *
 * 6th July 2015
 * - Fixed a StackOverflow exception being thrown when loading interstitials when it was already loading one causing ShowInterstitial to be called on loop until the interstitial loaded (crashing the app)
 * 
 * 9th July 2015
 * - Fixed a StackOverflow exception being thrown when loading a banner when it was already loading one causing ShowBanner to be called on loop (crashing the app)
 */

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ActionStateChecks
{
	public bool State { get; set; }
	public string LogMessage { get; private set; }
	
	public ActionStateChecks(bool inState, string inLogMessage)
	{
		State = inState;
		LogMessage = inLogMessage;
	}
}

public class ActionStateItems
{
	public List<ActionStateChecks> items = new List<ActionStateChecks>();
	
	public void UpdateItems(List<bool> inItems)
	{
		if(items.Count != inItems.Count)
			Debug.Log ("Invalid inItem count!");
		
		for(int i=0;i < items.Count;i++)
			items[i].State = inItems[i];
	}
}

public class AdMob_Manager : MonoBehaviour
{
	public static AdMob_Manager Instance;
	public static bool AdMobAndroidReady = false;
	
	// Your interstitial and banner IDs, you will be given unique IDs per project (Set these in the inspector!)
	public string interstitialID = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";
	public string bannerID = "ca-app-pub-xxxxxxxxxxxxxxxx/xxxxxxxxxx";
	
	// Is admob enabled? Will any banners or interstitials be triggered?
	public bool EnableAdMob = true;
	
	// Should we be in test mode for testing ads?
	public bool EnableTestMode = false;
	
	// Should an interstitial be LOADED (Not shown) when this script starts?
	public bool IntLoadOnStart = false;
	
	// Shown an interstitial be LOADED (Not shown) as soon as a previous interstitial is closed (always keeping an interstitial ready in memory)
	public bool IntAutoReload = false;
	
	// Should a banner be LOADED (Not shown) when this script starts?
	public bool BannerLoadOnStart = false;
	
	// Banner type which will be loaded on start
	public AdmobAd.BannerAdType BannerLoadOnStartType;
	
	// Position of banner which will be loaded on start
	public AdmobAd.AdLayout BannerLoadOnStartPos;
	
	// Information about the interstitial state
	public bool IntIsReady { get; private set; }
	public bool IntIsLoading { get; private set; }
	public bool IntIsVisible { get; private set; }
	public bool IntWantedVisible { get; private set; }
	
	// Information about the banner state
	public bool BannerIsReady { get; private set; }
	public bool BannerIsLoading { get; private set; }
	public bool BannerIsVisible { get; private set; }
	public bool BannerWantedVisible { get; private set; }
	
	// Cache the type of the current banner in memory so we can process calls to LoadBanner again to change certain things without needing to actually request another banner
	private AdmobAd.BannerAdType BannerInMemoryType;
	private AdmobAd.AdLayout BannerInMemoryLayout;
	
	// If we're hiding a banner due to an overlay (popup box or backscreen) then we want to remember the ad state when that is closed
	public bool BannerPrevState { get; private set; }
	
	// Sometimes we like to overlay our overlays but still want to remember our original banner state
	public int BannerOverlayDepth { get; private set; }
	
	public Dictionary<string, ActionStateItems> ActionState = new Dictionary<string, ActionStateItems>();
	
	// Should debug messages be logged?
	public bool DebugLogging = false;
	
	public bool InterstitialWaitScreenEnabled = true;
	
	// Screen to show before an interstitial pops
	public GameObject InterstitialWaitScreen;
	
	// Time to wait before displaying interstitial after InterstitialWaitScreen has appeared
	public float InterstitialWaitTime = 1f;
	
	// An Instance to the AdmobAd Instance (AdmobAd.Instance())
	private AdmobAd AdIns;
	
	void Awake()
	{
		if(Instance){
			DebugLog("You have duplicate AdMob_Manager.cs scripts in your scene! Admob might not work as expected!");
			Destroy (gameObject);
			return;
		}
		
		Instance = this;
		DontDestroyOnLoad(this);
		
		if(EnableAdMob){
			AdmobAdAgent.RetainGameObject(ref AdMobAndroidReady, gameObject, null);
			AdIns = AdmobAd.Instance();
		}
	}
	
	void OnEnable()
	{
		// Register the interstitial ad events
		AdmobAdAgent.OnReceiveAdInterstitial += OnReceiveInterstitial;
		AdmobAdAgent.OnFailedToReceiveAdInterstitial += OnFailedReceiveInterstitial;
		AdmobAdAgent.OnPresentScreenInterstitial += OnInterstitialVisible;
		AdmobAdAgent.OnDismissScreenInterstitial += OnInterstitialHidden;
		AdmobAdAgent.OnLeaveApplicationInterstitial += OnInterstitialClick;	
		
		// Register the banner ad events
		AdmobAdAgent.OnReceiveAd += OnReceiveBanner;
		AdmobAdAgent.OnFailedToReceiveAd += OnFailReceiveBanner;
		AdmobAdAgent.OnAdShown += OnBannerVisible;
		AdmobAdAgent.OnAdHidden += OnBannerHidden;
		AdmobAdAgent.OnLeaveApplication += OnBannerClick;
	}
	
	void OnDisable()
	{
		// Unregister the interstitial ad events
		AdmobAdAgent.OnReceiveAdInterstitial -= OnReceiveInterstitial;
		AdmobAdAgent.OnFailedToReceiveAdInterstitial -= OnFailedReceiveInterstitial;
		AdmobAdAgent.OnPresentScreenInterstitial -= OnInterstitialVisible;
		AdmobAdAgent.OnDismissScreenInterstitial -= OnInterstitialHidden;
		AdmobAdAgent.OnLeaveApplicationInterstitial -= OnInterstitialClick;	
		
		// Unregister the banner ad events
		AdmobAdAgent.OnReceiveAd -= OnReceiveBanner;
		AdmobAdAgent.OnFailedToReceiveAd -= OnFailReceiveBanner;
		AdmobAdAgent.OnAdShown -= OnBannerVisible;
		AdmobAdAgent.OnAdHidden -= OnBannerHidden;
		AdmobAdAgent.OnLeaveApplication -= OnBannerClick;
	}
	
	private void DebugLog(string Message, bool IgnoreDebugSetting = false)
	{
		if(!DebugLogging && !IgnoreDebugSetting)
			return;
		
		// Prepend AdMobManager to the debug output to make it easier to filter in logcat
		Debug.Log ("AdMobManager " + Message);
	}
	
	void Start()
	{
		if(!EnableAdMob){
			DebugLog("AdMob is NOT enabled! No adverts will be triggered!", true);
			return;
		}
		
		ActionState.Add("LoadInterstitial", new ActionStateItems());
		ActionState["LoadInterstitial"].items.Add(new ActionStateChecks(IntIsLoading, "Already loading!"));	// Don't load interstitial if one is already loading
		ActionState["LoadInterstitial"].items.Add(new ActionStateChecks(IntIsReady, "Already ready!"));		// Don't load interstitial if one is already loaded
		ActionState["LoadInterstitial"].items.Add(new ActionStateChecks(IntIsVisible, "Already visible!"));	// Don't load interstitial if one is already visible
		
		ActionState.Add("LoadBanner", new ActionStateItems());
		ActionState["LoadBanner"].items.Add(new ActionStateChecks(BannerIsLoading, "Already loading!"));	// Don't load banner if one is already loading
		ActionState["LoadBanner"].items.Add(new ActionStateChecks(BannerIsReady, "Already ready!"));		// Don't load banner if one is already loaded
		ActionState["LoadBanner"].items.Add(new ActionStateChecks(BannerIsVisible, "Already visible!"));	// Don't load banner if one is already visible
		
		ActionState.Add("RepositionBanner", new ActionStateItems());
		ActionState["RepositionBanner"].items.Add(new ActionStateChecks(BannerIsReady, "Needs to be visible!"));	// Don't reposition banner if it's not yet ready
		
		ActionState.Add("ShowInterstitial", new ActionStateItems());
		ActionState["ShowInterstitial"].items.Add(new ActionStateChecks(IntIsVisible, "Already visible!"));	// Don't show interstitial if one is already visible
		
		ActionState.Add("ShowBanner", new ActionStateItems());
		ActionState["ShowBanner"].items.Add(new ActionStateChecks(BannerIsVisible, "Already visible!")); 	// Don't show banner if one is already visible
		
		// Set the banner ID and interstitial ID for this app
		AdIns.Init(bannerID, interstitialID, EnableTestMode);
		
		if(EnableTestMode)
			DebugLog("This build has admob set to debug mode! Remember to disable before release!", true);
		
		// Load an interstitial if the option is selected
		if(IntLoadOnStart)
			LoadInterstitial(false);
		
		// Load a banner if the option is selected
		if(BannerLoadOnStart)
			LoadBanner(BannerLoadOnStartType, BannerLoadOnStartPos, false, AdmobAd.TagForChildrenDirectedTreatment.Unset);
	}
	
	private bool CanPerformAction(string ActionName, List<ActionStateChecks> ActionChecks)
	{
		// Iterate through the checks in the current item
		for(int i=0;i < ActionChecks.Count;i++)
		{
			// Return false and log the event if any of the check states are false
			if(!ActionChecks[i].State){
				//GoogleAnalytics.Instance.LogEvent("AdMob", ActionName + " failed", ActionChecks[i].LogMessage);
				DebugLog(ActionName + " failed - " + ActionChecks[i].LogMessage);
				return false;
			}
		}
		
		return true;
	}
	
	/// <summary>
	/// Loads an interstitial advert into memory.
	/// </summary>
	/// <param name="DisplayImmediately">If set to <c>true</c> display the interstitial immediately when it has finished loading.</param>
	public void LoadInterstitial(bool DisplayImmediately = false)
	{
		if(!EnableAdMob)
			return;
		
		// Get the name of the current method
		string MethodName = "LoadInterstitial";
		
		DebugLog(MethodName + " called");
		
		// Update the state items (Values used to determine if the action in this method should be ran)
		ActionState[MethodName].UpdateItems(new List<bool>(){ !IntIsLoading, !IntIsReady, !IntIsVisible });
		
		// Check if we can perform the action for the current method
		if(CanPerformAction(MethodName, ActionState[MethodName].items)){
			// Mark the interstitial as loading
			IntIsLoading = true;
			
			// If we want to display the interstitial as soon as it's loaded then mark the wanted visible variable as true
			IntWantedVisible = DisplayImmediately;
			
			// Load an interstitial ad marking it as hidden, this script will handle showing the interstitial
			AdIns.LoadInterstitialAd(true);
		} else {
			if(DisplayImmediately)
				ShowInterstitial();
		}
	}
	
	private void LoadInterstitial(bool DisplayImmediately, bool ForcedInternalCall)
	{
		if(ForcedInternalCall){
			LoadInterstitial(false);
			IntWantedVisible = DisplayImmediately;
		} else {
			LoadInterstitial(DisplayImmediately);
		}
	}
	
	/// <summary>
	/// Shows an interstitial if one is loaded in memory.
	/// </summary>
	public void ShowInterstitial()
	{
		if(!EnableAdMob)
			return;
		
		// Get the name of the current method
		string MethodName = "ShowInterstitial";
		
		DebugLog(MethodName + " called");
		
		// Update the state items (Values used to determine if the action in this method should be ran)
		ActionState[MethodName].UpdateItems(new List<bool>(){ !IntIsVisible });
		
		// Check if we can perform the action for the current method
		if(CanPerformAction(MethodName, ActionState[MethodName].items)){
			if(IntIsReady){
				if(InterstitialWaitScreenEnabled){
					// We're ready to show the interstitial but first a message from our sponsors err I mean a black screen wait wait text on it
					if(InterstitialWaitScreen != null){
						// Enable the wait screen
						InterstitialWaitScreen.SetActive(true);
						
						StartCoroutine(ShowInterstitialAfterDelay());
					} else {
						DebugLog("Wait screen was enabled but no gameobject was set! Interstitial will not be delayed..", true);
						
						// Show the interstitial
						AdIns.ShowInterstitialAd();
					}
				} else {
					// Show the interstitial
					AdIns.ShowInterstitialAd();
				}
			} else {
				LoadInterstitial(true, true);
			}
		}
	}
	
	private IEnumerator ShowInterstitialAfterDelay()
	{
		yield return new WaitForSeconds(InterstitialWaitTime);
		
		// Show the interstitial
		AdIns.ShowInterstitialAd();
		
		// Give a little bit of time for the interstitial to actually show
		yield return new WaitForSeconds(0.3f);
		
		// Disable the wait screen
		InterstitialWaitScreen.SetActive(false);
	}
	
	/// <summary>
	/// Cancels an interstitial from loading, useful if you wanted to show an interstitial on menu x but it didn't load in time, 
	/// you might want to cancel the interstitial from showing once the player enters the main game for example.
	/// </summary>
	public void CancelInterstitial()
	{
		if(!EnableAdMob)
			return;
		
		// Mark the interstitial as not wanted to show
		IntWantedVisible = false;
		
		DebugLog("Cancelling interstitial!");
	}
	
	// public void DestroyInterstitial() // Not supported by NeatPlug!
	
	/// <summary>
	/// Loads a banner advert into memory.
	/// </summary>
	/// <param name="AdType">Admob banner ad type.</param>
	/// <param name="AdLayout">Admob ad position.</param>
	/// <param name="DisplayImmediately">If set to <c>true</c> display the banner immediately when it has finished loading.</param>
	/// <param name="AdChildDirected">Ad child directed.</param>
	public void LoadBanner(AdmobAd.BannerAdType AdType, AdmobAd.AdLayout AdLayout, bool DisplayImmediately = false, AdmobAd.TagForChildrenDirectedTreatment AdChildDirected = AdmobAd.TagForChildrenDirectedTreatment.Unset)
	{
		if(!EnableAdMob)
			return;
		
		// Get the name of the current method
		string MethodName = "LoadBanner";
		
		DebugLog(MethodName + " called");
		
		// Update the state items (Values used to determine if the action in this method should be ran)
		ActionState[MethodName].UpdateItems(new List<bool>(){ !BannerIsLoading, !BannerIsReady, !BannerIsVisible });
		
		// Check if we can perform the action for the current method
		if(CanPerformAction(MethodName, ActionState[MethodName].items)){
			// Mark the banner as loading
			BannerIsLoading = true;
			
			// If we want to display the banner as soon as it's loaded then mark the wanted visible variable as true
			BannerWantedVisible = DisplayImmediately;
			
			// Load a banner ad marking it as hidden, this script will handle showing the banner
			AdIns.LoadBannerAd(AdType, AdLayout, Vector2.zero, true, new Dictionary<string, string>(), AdChildDirected);
			
			BannerInMemoryType = AdType;
			BannerInMemoryLayout = AdLayout;
		} else {
			// We already have a banner loading, ready or visible.. if we tried to load the same banner type maybe reposition or simply ShowBanner would be better here
			if(AdType == BannerInMemoryType){
				if(AdLayout != BannerInMemoryLayout)
					RepositionBanner(0, 0, AdLayout);
				
				if(DisplayImmediately)
					ShowBanner();
			}
		}
	}
	
	private void LoadBanner(bool DisplayImmediately, bool ForcedInternalCall)
	{
		if(ForcedInternalCall){
			LoadBanner(BannerInMemoryType, BannerInMemoryLayout, false);
			BannerWantedVisible = DisplayImmediately;
		} else {
			LoadBanner(BannerInMemoryType, BannerInMemoryLayout, DisplayImmediately);
		}
	}
	
	/// <summary>
	/// Repositions the banner.
	/// </summary>
	/// <param name="xPos">X position.</param>
	/// <param name="yPos">Y position.</param>
	/// <param name="AdLayout">Admob ad position.</param>
	public void RepositionBanner(int xPos, int yPos, AdmobAd.AdLayout AdLayout = AdmobAd.AdLayout.Top_Left)
	{
		if(!EnableAdMob)
			return;
		
		// Get the name of the current method
		string MethodName = "RepositionBanner";
		
		DebugLog(MethodName + " called");
		
		// Update the state items (Values used to determine if the action in this method should be ran)
		ActionState[MethodName].UpdateItems(new List<bool>(){ BannerIsReady });
		
		// Check if we can perform the action for the current method
		if(CanPerformAction(MethodName, ActionState[MethodName].items)){
			// Reposition the banner
			AdIns.RepositionBannerAd(AdLayout, xPos, yPos);
		}
	}
	
	/// <summary>
	/// Shows a banner advert if one is loaded in memory.
	/// </summary>
	public void ShowBanner()
	{
		if(!EnableAdMob)
			return;
		
		// Get the name of the current method
		string MethodName = "ShowBanner";
		
		DebugLog(MethodName + " called");
		
		// Check if we're calling ShowBanner because we're returning from an overlay screen which hid the banner
		if(BannerOverlayDepth > 0){
			// Decrease the overlay depth by 1
			BannerOverlayDepth--;
			
			// If the overlay depth is still above 0 then there must still be some overlays open
			if(BannerOverlayDepth > 0)
				return;
			
			// There isn't any more overlaying menus open, return to the previous banner ad state
			BannerWantedVisible = BannerPrevState;
			
			DebugLog ("Banner wanted set to prev state: " + BannerPrevState);
		} else {
			BannerWantedVisible = true;
		}
		
		if(!BannerWantedVisible)
			return;
		
		// Update the state items (Values used to determine if the action in this method should be ran)
		ActionState[MethodName].UpdateItems(new List<bool>(){ !BannerIsVisible });
		
		// Check if we can perform the action for the current method
		if(CanPerformAction(MethodName, ActionState[MethodName].items)){
			if(BannerIsReady){
				// Show the banner
				AdIns.ShowBannerAd();
			} else {
				LoadBanner(true, true);
			}
		}
	}
	
	/// <summary>
	/// Hides a banner advert, will also cancel a banner advert from showing if one is loaded.
	/// </summary>
	/// <param name="IsOverlay">Set to <c>true</c> if you want to hide the banner while opening an overlaying screen (such as the backscreen) and want to revert the banner ad status later.</param>
	public void HideBanner(bool IsOverlay = false)
	{
		if(!EnableAdMob)
			return;
		
		// If this is an overlaying screen (e.g backscreen) then we'll want to return to the previous banner state when we close it
		if(IsOverlay){
			BannerOverlayDepth++;
			
			if(BannerOverlayDepth == 1)
				BannerPrevState = BannerWantedVisible;
		}
		
		DebugLog("Hiding banner!");
		
		// Mark wanted visible as false so if the banner ad hasn't loaded yet it'll make sure it isn't shown when loaded
		BannerWantedVisible = false;
		BannerIsVisible = false;
		
		// Hide the banner advert from view (This does not unload it from memory)
		AdIns.HideBannerAd();
	}
	
	/// <summary>
	/// Remove the banner from memory. (Required if you want to load a new banner ad type)
	/// </summary>
	public void DestroyBanner()
	{
		if(!EnableAdMob)
			return;
		
		// Mark the banner properties as false as the banner is now destroyed
		BannerWantedVisible = false;
		BannerIsLoading = false;
		BannerIsReady = false;
		BannerIsVisible = false;
		BannerInMemoryType = default(AdmobAd.BannerAdType);
		BannerInMemoryLayout = default(AdmobAd.AdLayout);
		
		DebugLog("Destroying banner!");
		
		// Unload the banner from memory
		AdIns.DestroyBannerAd();
	}
	
	// Input how many pixels of space are free and the largest ad for this space will be calculated taking DPI into account
	public int GetLargestAdForSpace(int AvailableWidth = int.MaxValue, int AvailableHeight = int.MaxValue)
	{
		// Make sure this project has the required scripts and plugins!
		float dpi = JarLoader.GetDensity();
		float ScrWidth = (float)Screen.width;
		float ScrHeight = (float)Screen.height;
		
		// Some devices can report weird DPI values so we add padding just to make sure the ad doesn't go off screen as admob won't display any banner in those outcomes!
		float ReqPadding = 2f;
		
		// Default back to 160 DPI if the DPI was invalid
		if(dpi <= 0f)
			dpi = 160f;
		
		List<float> AdWidths = new List<float>(){ 728f, 468f, 320f };
		List<float> AdHeights = new List<float>(){ 90f, 60f, 50f };
		
		int BannerOutput = -1;
		
		foreach(float AdWidth in AdWidths)
		{
			if(AvailableWidth >= (AdWidth + ReqPadding) * (dpi / 160f)){
				foreach(float AdHeight in AdHeights)
				{
					if(AvailableHeight >= (AdHeight + ReqPadding) * (dpi / 160f)){
						if(AdWidth == AdWidths[0] && AdHeight == AdHeights[0]){
							BannerOutput = 0; //AdmobAd.BannerAdType.Tablets_IAB_LeaderBoard_728x90
						} else if(AdWidth == AdWidths[1] && AdHeight == AdHeights[1]) {
							BannerOutput = 1; //AdmobAd.BannerAdType.Tablets_IAB_Banner_468x60
						} else if(AdWidth == AdWidths[2] && AdHeight == AdHeights[2]) {
							BannerOutput = 2; //AdmobAd.BannerAdType.Universal_Banner_320x50
						} else if(AdWidth >= AdWidths[0]) {
							BannerOutput = 3; //AdmobAd.BannerAdType.Universal_SmartBanner
						}
						
						break;
					}
				}
				break;
			}
		}
		
		DebugLog("Not enough space to display any advert! Wanted to display an ad in the space of, W: " + AvailableWidth + " H: " + AvailableHeight);
		GoogleAnalytics.Instance.LogEvent("AdMob", "Not enough space for ad", "W: " + AvailableWidth + " H: " + AvailableHeight);
		
		return BannerOutput;
	}
	
	
	// Everything past this point is ad listener events //
	
	private void OnReceiveBanner()
	{
		BannerIsReady = true;
		BannerIsLoading = false;
		
		DebugLog ("New banner loaded!");
		
		if(BannerWantedVisible){
			ShowBanner();
		} else {
			HideBanner();
		}
	}
	
	private void OnFailReceiveBanner(string ErrMsg)
	{
		BannerIsReady = false;
		
		DebugLog("Banner failed to load - Error: " + ErrMsg);
		GoogleAnalytics.Instance.LogEvent("AdMob", "Banner Load Failure", "Error: " + ErrMsg);
		
		StartCoroutine(RetryBannerLoad());
	}
	
	private IEnumerator RetryBannerLoad()
	{
		yield return new WaitForSeconds(5f);
		
		DebugLog("Retrying banner load!");
		
		if(!BannerIsLoading){
			BannerInMemoryType = default(AdmobAd.BannerAdType);
			BannerInMemoryLayout = default(AdmobAd.AdLayout);
			return false;
		}
		
		LoadBanner(BannerInMemoryType, BannerInMemoryLayout, false, AdmobAd.TagForChildrenDirectedTreatment.Unset);
	}
	
	private void OnBannerVisible()
	{
		BannerIsVisible = true;
		
		if(!BannerWantedVisible)
			HideBanner();
	}
	
	private void OnBannerHidden()
	{
		BannerIsVisible = false;
		
		if(BannerWantedVisible)
			ShowBanner();
	}
	
	private void OnBannerClick()
	{
		GoogleAnalytics.Instance.LogEvent("AdMob", "Banner Clicked", "CLIENT_ID");
	}
	
	private void OnReceiveInterstitial()
	{
		IntIsReady = true;
		IntIsLoading = false;
		
		if(IntWantedVisible)
			ShowInterstitial();
	}
	
	private void OnFailedReceiveInterstitial(string ErrMsg)
	{
		IntIsReady = false;
		IntIsLoading = false;
		
		DebugLog("Interstitial failed to load - Error: " + ErrMsg);
		GoogleAnalytics.Instance.LogEvent("AdMob", "Interstitial Load Failure", "Error: " + ErrMsg);
	}
	
	private void OnInterstitialVisible()
	{
		IntIsReady = false;
		IntIsVisible = true;
		
		// The interstitial auto hides ads but lets call the usual functions so the variables know what's happening
		HideBanner(true);
	}
	
	private void OnInterstitialHidden()
	{
		IntIsVisible = false;
		
		if(IntAutoReload)
			LoadInterstitial(false);
		
		// The interstitial auto hid any banners, so we need to re-enable them
		ShowBanner();
	}
	
	private void OnInterstitialClick()
	{
		GoogleAnalytics.Instance.LogEvent("AdMob", "Interstitial Clicked", "CLIENT_ID");
	}
	
}