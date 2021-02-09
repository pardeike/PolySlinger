using System;
using System.Globalization;
using UnityEngine;

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
		string playerHeight()
		{
			var sign = Math.Sign(extraPlayerHeight);
			if (RegionInfo.CurrentRegion.IsMetric)
			{
				var cm = Mathf.RoundToInt(extraPlayerHeight * 100);
				var prefix1 = cm == 0 ? "" : (sign == 1 ? "+" : "-");
				return prefix1 + cm + " cm";
			}

			var h = Mathf.Abs(extraPlayerHeight);
			var totalInches = Mathf.FloorToInt(h * 39.3701f);
			var feet = Mathf.FloorToInt(totalInches / 12f);
			var inches = totalInches - feet * 12;
			var prefix2 = feet + inches == 0 ? "" : (sign == 1 ? "+" : "-");
			if (feet == 0)
				return $"{prefix2}{inches}\"";
			return $"{prefix2}{feet}' {inches}\"";
		}

		return idx switch
		{
			0 => musicVolume.ToString("P0", CultureInfo.InvariantCulture),
			1 => playerHeight(),
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
