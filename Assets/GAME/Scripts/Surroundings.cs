using UnityEngine;

[ExecuteAlways]
public class Surroundings : MonoBehaviour
{
	public Material skyBox;
	public Light sun;
	public Color shadowColor;
	public float intensityMultiplier;
	public bool fog;
	public Color fogColor;
	public FogMode fogMode;
	public float fogStartDistance;
	public float fogEndDistance;

	void Start()
	{
		RenderSettings.skybox = skyBox;
		RenderSettings.sun = sun;
		RenderSettings.ambientLight = shadowColor;
		RenderSettings.ambientIntensity = intensityMultiplier;
		RenderSettings.fog = fog;
		RenderSettings.fogColor = fogColor;
		RenderSettings.fogMode = fogMode;
		RenderSettings.fogStartDistance = fogStartDistance;
		RenderSettings.fogEndDistance = fogEndDistance;
	}

	void OnGUI()
	{
		Start();
	}
}
