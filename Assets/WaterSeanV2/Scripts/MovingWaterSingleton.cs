using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MovingWaterSingleton : MonoBehaviour {
	
	public static bool playing = true;			//So no external dependencies.
	//public static MovingWaterSingleton self;	//Prevent unnecessary load.
	
	public Vector2 m_sineAmplitudeXY = 	new Vector2( 0.07f, 0.06f );
	public Vector2 m_sinePeriodXY = 	new Vector2( 0.2f, 0.28f );
	public Vector2 m_sineOffsetXY = 	new Vector2( 0f, 0f );
	
	public bool m_useSecondSineWave = true;
	
	public Vector2 m_sineAmplitudeXY2 = 	new Vector2( -0.1f, -0.07f );
	public Vector2 m_sinePeriodXY2 = 	new Vector2( 0.22f, 0.24f );
	public Vector2 m_sineOffsetXY2 = 	new Vector2( 0f, 0f );
	
	public bool m_ChangeTransparency = false;
	
	public float m_TransparencyAmplitude = 0.074f;
	public float m_TransparencyPeriod = 1f;
	public float m_TransparencyOffset = 0.6f;
	
	public Material m_SelfMaterial;
	
	private Color cachedColor;
	
	// Use this for initialization
	void Start () {
		
		//if ( self == null  ){
			//self = this;
		if ( true ){
			
			if ( m_SelfMaterial == null ){
				Debug.LogError("MovingWaterSingleton: Error, please supply a material");
				playing = false;
				Debug.Break();
			}else{
				cachedColor = m_SelfMaterial.color;
			}
			
		}
		//else{
		//	Debug.LogError("MovingWaterSingleton: Error, duplicate instance!");
		//	playing = false;
		//	Debug.Break();
		//}
		
	}
	
	void Update(){
		
		if ( playing ){
		
			float xPos = ( Mathf.Sin( Time.time * m_sinePeriodXY.x ) * m_sineAmplitudeXY.x ) + m_sineOffsetXY.x;
			float yPos = ( Mathf.Cos( Time.time * m_sinePeriodXY.y ) * m_sineAmplitudeXY.y ) + m_sineOffsetXY.y;
			
			if ( m_useSecondSineWave ){
				
				xPos += ( Mathf.Sin( Time.time * m_sinePeriodXY2.x ) * m_sineAmplitudeXY2.x ) + m_sineOffsetXY2.x;
				yPos += ( Mathf.Cos( Time.time * m_sinePeriodXY2.y ) * m_sineAmplitudeXY2.y ) + m_sineOffsetXY2.y;
				
			}
			
			m_SelfMaterial.mainTextureOffset = new Vector2( xPos, yPos );
			
			if ( m_ChangeTransparency ){
				
				float trans = ( Mathf.Sin( Time.time * m_TransparencyPeriod ) * m_TransparencyAmplitude) + m_TransparencyOffset;
				//m_SelfMaterial.color = new Color( m_SelfMaterial.color.r, m_SelfMaterial.color.g, m_SelfMaterial.color.b, trans );
				
				cachedColor.a = trans;
				m_SelfMaterial.color = cachedColor;
				
			}
			
		}
		
	}
	
}
