using UnityEngine;
using System.Collections;

public class AdjustAtMinDistance : MonoBehaviour {
	
	public UIWidget TargetWidget;
	
	public float MarginDistance; // This widget will not get closer than this value from the target
	
	public enum Sides { Left, Right, Top, Bottom }
	public Sides SelfSideToAdjust; // Which side is the target approaching from?
	
	public enum RunOn { Awake, Start, Enable, Update }
	public RunOn SelectedRunOn; // When should we run UpdateDistance() (Update useful for editor testing but on a device you shouldn't expect the resolution or ad size to change
	
	private UIWidget SelfWidget;
	
	private Transform InitialAnchorTransform;
	private float InitialAnchorRelative;
	private float InitialAnchorAbsolute;
	
	private int InitialWidth;
	private int InitialHeight;
	
	private UIRect.AnchorPoint SelectedAnchor;
	
	void Awake()
	{
		SelfWidget = GetComponent<UIWidget>();
		
		switch(SelfSideToAdjust)
		{
		case Sides.Left: SelectedAnchor = SelfWidget.leftAnchor; break;
		case Sides.Right: SelectedAnchor = SelfWidget.rightAnchor; break;
		case Sides.Top: SelectedAnchor = SelfWidget.topAnchor; break;
		case Sides.Bottom: SelectedAnchor = SelfWidget.bottomAnchor; break;
		}
		
		InitialAnchorTransform = SelectedAnchor.target;
		InitialAnchorRelative = SelectedAnchor.relative;
		InitialAnchorAbsolute = SelectedAnchor.absolute;
		
		InitialWidth = SelfWidget.width;
		InitialHeight = SelfWidget.height;
		
		if(SelectedRunOn != RunOn.Awake) return;
		
		UpdateDistance();
	}
	
	void Start()
	{
		if(SelectedRunOn != RunOn.Start) return;
		
		UpdateDistance();
	}
	
	void OnEnable()
	{
		if(SelectedRunOn != RunOn.Enable) return;
		
		UpdateDistance();
	}
	
	void Update()
	{
		if(SelectedRunOn != RunOn.Update) return;
		
		UpdateDistance();
	}
	
	private void UpdateDistance()
	{
		// Compare the selected side of SetWidget with the anchor point of the target
		if(SelfWidget.width > InitialWidth || SelfWidget.height > InitialHeight){
			SelectedAnchor.Set(InitialAnchorTransform, InitialAnchorRelative, InitialAnchorAbsolute);
			
			SelfWidget.ResetAnchors(); // Without this changing the target transform won't affect anything, causing unexpected results
			SelfWidget.UpdateAnchors();
		} else {
			float DistanceToTarget = 0f;
			float AbsolutePosition = 0f;
			int PivotAdjustment = 0;
			bool ShouldAdjustAnchor = false;
			
			switch(SelfSideToAdjust)
			{
			case Sides.Left: 
				DistanceToTarget = SelfWidget.GetSides(TargetWidget.cachedTransform)[0].x; 
				AbsolutePosition = 1f;
				PivotAdjustment = Mathf.RoundToInt(Mathf.InverseLerp(1f, 0f, TargetWidget.pivotOffset.x) * TargetWidget.width);
				ShouldAdjustAnchor = (DistanceToTarget <= MarginDistance + PivotAdjustment);
				break;
				
			case Sides.Right: 
				DistanceToTarget = SelfWidget.GetSides(TargetWidget.cachedTransform)[2].x; 
				AbsolutePosition = 0f; 
				PivotAdjustment = Mathf.RoundToInt(TargetWidget.pivotOffset.x * -TargetWidget.width);
				ShouldAdjustAnchor = (DistanceToTarget >= -MarginDistance + PivotAdjustment);
				break;
				
			case Sides.Top: 
				DistanceToTarget = SelfWidget.GetSides(TargetWidget.cachedTransform)[1].y; 
				AbsolutePosition = 0f; 
				PivotAdjustment = Mathf.RoundToInt(TargetWidget.pivotOffset.y * -TargetWidget.height);
				ShouldAdjustAnchor = (DistanceToTarget >= -MarginDistance + PivotAdjustment);
				break;
				
			case Sides.Bottom: 
				DistanceToTarget = SelfWidget.GetSides(TargetWidget.cachedTransform)[3].y; 
				AbsolutePosition = 1f; 
				PivotAdjustment = Mathf.RoundToInt(Mathf.InverseLerp(1f, 0f, TargetWidget.pivotOffset.y) * TargetWidget.height);
				ShouldAdjustAnchor = (DistanceToTarget <= MarginDistance + PivotAdjustment);
				break;
			}
			
			if(ShouldAdjustAnchor){
				SelectedAnchor.Set(TargetWidget.cachedTransform, AbsolutePosition, AbsolutePosition == 1f ? MarginDistance : -MarginDistance);
				
				SelfWidget.ResetAnchors(); // Without this changing the target transform won't affect anything, causing unexpected results
				SelfWidget.UpdateAnchors();
			}
		}
	}
}