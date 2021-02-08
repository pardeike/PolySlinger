using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class SliderRange : MonoBehaviour
{
	public Texture icon;
	public Color color;
	public float min;
	public float max;
	public float current;
	public Func<float, string> toString;

	RawImage iconImage;
	Transform knob;
	string oldValue = "";
	Text valueText;
	Action<float> callback;
	const float minX = -0.1f;
	const float width = 0.3f;

	public void Prepare(float min, float max, float current, Func<float, string> toString, Action<float> callback)
	{
		if (knob == null)
			Initialize();

		this.min = min;
		this.max = max;
		this.current = current;
		this.toString = toString;
		this.callback = callback;
		oldValue = toString(current);
		valueText.text = oldValue;

		UpdateKnob();
	}

	float KnobValue()
	{
		var n = Mathf.Max(-0.1f, Mathf.Min(minX + width, knob.localPosition.x));
		return min + (max - min) * (n - minX) / width;
	}

	void UpdateKnob()
	{
		var n = Mathf.Max(min, Mathf.Min(max, current));
		var x = minX + width * (n - min) / (max - min);
		knob.localPosition = new Vector3(x, 0, -0.001f);
	}

	void Initialize()
	{
		if (iconImage == null)
			iconImage = GetComponentsInChildren<RawImage>().FirstOrDefault(cmp => cmp.name == "icon");
		if (iconImage != null)
		{
			iconImage.texture = icon;
			iconImage.color = color;
		}

		if (knob == null)
			knob = transform.Find("Knob");

		if (valueText == null)
			valueText = transform.GetComponentsInChildren<Text>().First();
	}

	public void Update()
	{
		Initialize();
		current = KnobValue();
		callback(current);
		var newValue = toString(current);
		if (newValue != oldValue)
		{
			oldValue = newValue;
			valueText.text = newValue;
		}

		if (knob.localPosition.x < minX)
			knob.localPosition = new Vector3(minX, 0, -0.001f);
		if (knob.localPosition.x > minX + width)
			knob.localPosition = new Vector3(minX + width, 0, -0.001f);
	}
}
