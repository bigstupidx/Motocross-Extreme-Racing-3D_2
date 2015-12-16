using UnityEngine;
using System.Collections;

public class RefuelingButton : MonoBehaviour {
	
	public UILabel FuelingLabel;
	public UISprite FuelingSprite;
	
	public Color LabelWaitColor;
	public Color LabelReadyColor;
	
	public float TotalReadyTime = 5f;
	private float RemainingReadyTime = 5f;
	private int CurWaitDots = 0;
	
	private float TimeSinceLastLabelUpdate = 0f;
	
	void OnClick()
	{
		if(RemainingReadyTime <= 0f){
			Time.timeScale = 1f;
			AdMob_Manager.Instance.HideBanner(false);
			AdMob_Manager.Instance.LoadInterstitial(true);

			Game.instance.reloadFuel();	//FuelManager.Instance.OutOfFuelScreen.SetActive(false);
		}
	}
	
	void OnEnable()
	{
		RemainingReadyTime = TotalReadyTime;
		CurWaitDots = 0;
		TimeSinceLastLabelUpdate = 0f;
		
		FuelingLabel.text = "Refueling";
		FuelingLabel.color = LabelWaitColor;
		
		FuelingSprite.spriteName = "Gas Station-100(1)";
	}
	
	void Update()
	{
		RemainingReadyTime -= RealTime.deltaTime;
		TimeSinceLastLabelUpdate += RealTime.deltaTime;
		
		if(RemainingReadyTime > 0f){
			if(TimeSinceLastLabelUpdate > 0.2f){
				TimeSinceLastLabelUpdate = 0f;
				
				string WaitDots = "";
				CurWaitDots = (CurWaitDots + 1 <= 3 ? CurWaitDots + 1 : 0);
				
				for(int i=0;i < CurWaitDots;i++)
					WaitDots += ".";
				
				FuelingLabel.text = "Refueling" + WaitDots;
				
				if(FuelingSprite.spriteName != "Gas Station-100(1)")
					FuelingSprite.spriteName = "Gas Station-100(1)";
				
				if(FuelingLabel.color != LabelWaitColor)
					FuelingLabel.color = LabelWaitColor;
			}
		} else {
			if(FuelingLabel.text != "Tap to continue!")
				FuelingLabel.text = "Tap to continue!";
			
			if(FuelingSprite.spriteName != "Motorcycle-100(1)")
				FuelingSprite.spriteName = "Motorcycle-100(1)";
			
			if(FuelingLabel.color != LabelReadyColor)
				FuelingLabel.color = LabelReadyColor;
		}
	}
}