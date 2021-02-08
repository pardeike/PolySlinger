using System;
using System.Globalization;

public class Settings : SettingsController<Settings>
{
	public static Settings Instance => BaseInstance;

	//

	[SettingsInterval(0, 1)]
	public float musicVolume = 0.8f;

	[SettingsInterval(-0.5f, 1)]
	public float extraPlayerHeight = 0;

	[SettingsInterval(0.5f, 2f)]
	public float controllerSensitivity = 1;

	[SettingsInterval(-30, 30)]
	public float controllerAngle = 0;

	[SettingsInterval(0.7f, 2f)]
	public float overlayScale = 1;

	public string DisplayString(int idx)
	{
		var playerHeight = extraPlayerHeight * 100;
		var playerHeightFormat = "F0";
		var playerHeightUnit = " cm";
		if (RegionInfo.CurrentRegion.IsMetric == false)
		{
			playerHeight *= 0.393701f;
			playerHeightFormat = "F1";
			playerHeightUnit = " \"";
		}

		return idx switch
		{
			0 => musicVolume.ToString("P0", CultureInfo.InvariantCulture),
			1 => playerHeight.ToString(playerHeightFormat, CultureInfo.InvariantCulture) + playerHeightUnit,
			2 => controllerSensitivity.ToString("P0", CultureInfo.InvariantCulture),
			3 => controllerAngle.ToString("F0", CultureInfo.InvariantCulture) + "°",
			4 => overlayScale < 0.75f ? "—" : overlayScale.ToString("P0", CultureInfo.InvariantCulture),
			_ => "",
		};
	}
}

[AttributeUsage(AttributeTargets.Field)]
public class SettingsIntervalAttribute : Attribute
{
	public float min;
	public float max;

	public SettingsIntervalAttribute(float min, float max)
	{
		this.min = min;
		this.max = max;
	}
}
