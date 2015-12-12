// JarLoader.cs [Updated 11th June 2015]
// Attach this script to a persistent Game Object or use it from the plugins folder

/* Change Log:
 * 7th June 2015 (Sean)
 * - Initial file creation
 * 
 * 8th June 2015 (Jonni)
 * - Optional search for package manager
 * - Auto-instantiation
 * - Compiler directives
 * - GetPackageList returns a string
 * 
 * 8th June 2015 (Sean)
 * - Overall cleanup
 * - Fixed ActivityContext and JavaClass being re-set each time GetInstance() was called
 * - Static Instance replaced with a ScriptReady bool
 * - Debug.Log replaced with a DebugLog function allowing LoggingEnable to be toggled to set debug outputs
 * 
 * 11th June 2015 (Sean)
 * - Added the GetDensity function which interacts with the Java GetDensity function, returning an accurate screen DPI
 * - If the GetDensity request fails then it will fallback to Screen.dpi and a GoogleAnalytics error is logged
*/

using UnityEngine;
using System.Collections;

public class JarLoader : MonoBehaviour {
	
	private static bool LoggingEnabled = false; // Toggle debug outputs
	
	private static bool ScriptReady = false; // True once there is a gameobject in the scene with JarLoader.cs which has awaken
	
	#if UNITY_ANDROID && !UNITY_EDITOR
	private static AndroidJavaObject ActivityContext;
	private static AndroidJavaClass JavaClass;
	#endif
	
	void Awake()
	{
		if(!ScriptReady){
			ScriptReady = true;
		} else {
			// This is a duplicate copy of JarLoader, destroy this!
			Destroy (gameObject);
			DebugLog("Destroyed duplicate JarLoader.cs!");
		}
	}
	
	private static void DebugLog(string Message)
	{
		if(LoggingEnabled)
			Debug.Log(Message);
	}
	
	private static void GetInstance()
	{
		DebugLog("Running GetInstance..");
		
		if(!ScriptReady){
			// JarLoader.cs wasn't attached to a persistant gameobject but a function was called!
			
			// Create a new gameobject for the JarLoader script
			GameObject JarLoaderObj = new GameObject("JarLoaderObj");
			
			// Attach the JarLoader script to the new gameobject and use this script for JarLoader.Instance references
			JarLoaderObj.AddComponent<JarLoader>();
			
			ScriptReady = true;
			
			DebugLog("JarLoader.cs was created");
		}
		
		#if UNITY_ANDROID && !UNITY_EDITOR
		if(ActivityContext == null || JavaClass == null){
			if(AndroidJNI.AttachCurrentThread() >= 0){
				ActivityContext = new AndroidJavaClass("com.unity3d.player.UnityPlayer").GetStatic<AndroidJavaObject>("currentActivity");
				JavaClass = new AndroidJavaClass("com.i6.PackageLister.PackageLister");
			} else {
				DebugLog("Failed to attach current thread to Java (Dalvik) VM");
			}
		}
		#elif UNITY_EDITOR
		DebugLog("JarLoader.cs will not attach current thread in the editor!");
		#elif !UNITY_ANDROID
		DebugLog("JarLoader.cs will not attach current thread on non-android devices!");
		#endif
		
	}
	
	public static int GetDensity()
	{
		// Make sure we have an instance and the ActivityContext + JavaClass is ready
		GetInstance();
		
		DebugLog("Getting display density..");
		
		#if UNITY_ANDROID && !UNITY_EDITOR
		if(JavaClass != null && ActivityContext != null){
			return JavaClass.CallStatic<int>("GetDensity", ActivityContext);
		} else {
			DebugLog("Failed to get display density!");
			GoogleAnalytics.Instance.LogError("Java DPI failure! Falling back to unreliable Screen.dpi!", false);
		}
		#elif UNITY_EDITOR 
		DebugLog ("JarLoader.cs will not get display desity from jar in the editor!");
		#elif !UNITY_ANDROID
		DebugLog("JarLoader.cs will not get display desity on non-android devices!");
		#endif
		
		// Nothing has been returned yet so just return Screen.dpi instead (Note that this will return 0 if it fails)
		return Mathf.RoundToInt(Screen.dpi);
	}
	
	public static string GetPackageList(string searchString = default(string))
	{
		// Make sure we have an instance and the ActivityContext + JavaClass is ready
		GetInstance();
		
		DebugLog("Getting package list..");
		
		#if UNITY_ANDROID && !UNITY_EDITOR
		string PackageList = string.Empty;
		
		if(JavaClass != null && ActivityContext != null){
			DebugLog ("About to get package list, here we go!");
			// Get the list of installed packages on the device
			PackageList = JavaClass.CallStatic<string>("GetPackageList", ActivityContext, searchString);
		} else {
			DebugLog("The Java class or ActivityContext wasn't ready when getting package list!");
		}
		
		DebugLog("Getting package list..");
		
		if (!string.IsNullOrEmpty (PackageList)) {
			DebugLog("Output: " + PackageList);
		} else {
			DebugLog("Output was null or empty!");
		}
		return PackageList;
		#elif UNITY_EDITOR
		DebugLog("JarLoader.cs will not GetPackageList in the editor!");
		#elif !UNITY_ANDROID
		DebugLog("JarLoader.cs will not GetPackageList on non-android devices!");
		#endif
		
		return string.Empty;
	}
	
	public static void DisplayToastMessage(string inString)
	{
		// Make sure we have an instance and the ActivityContext + JavaClass is ready
		GetInstance();
		
		#if UNITY_ANDROID && !UNITY_EDITOR
		if(JavaClass != null){
			// Get the list of installed packages on the device
			JavaClass.CallStatic("DisplayToast", ActivityContext, inString , 5);
			
			DebugLog("Toast has been popped!");
		} else {
			DebugLog("The Java class wasn't ready when displaying a toast!");
		}
		#else
		DebugLog("JarLoader.cs - Toast: " + inString);
		#endif
	}
	
	public static void CancelToastMessage()
	{
		// Make sure we have an instance and the ActivityContext + JavaClass is ready
		GetInstance();
		
		#if UNITY_ANDROID && !UNITY_EDITOR
		if(JavaClass != null){
			// Get the list of installed packages on the device
			JavaClass.CallStatic("ForceEndToast");
			
			DebugLog("Toast has been cancelled!");
		} else {
			DebugLog("The Java class wasn't ready when cancelling a toast!");
		}
		#else
		DebugLog("JarLoader.cs - Cancelling Toast");
		#endif
	}
	
}