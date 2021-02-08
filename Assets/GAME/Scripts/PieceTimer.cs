using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PieceTimer : MonoBehaviour
{
	public Color[] colors; // need to be 3
	public AudioClip warning;
	public AudioClip attention;
	public AudioClip peep;
	public bool planarToCamera = true;

	Text textObject;
	RectTransform sliderRect;
	float sliderWidth;
	Image image;
	string lastValue;
	readonly float step = 1f / 3f;
	float nextLimit;
	int counter = 0;
	float peepLevel = 4f;
	float peepDelta = 0.5f;
	readonly float peepDeltaShrink = 0.032f;

	void Awake()
	{
		textObject = GetComponentInChildren<Text>();
		textObject.text = "";
		image = transform.GetComponentsInChildren<Image>().First(img => img.gameObject.name == "slider");
		sliderRect = image.gameObject.GetComponent<RectTransform>();
		sliderWidth = sliderRect.rect.width;
		nextLimit = 2 * step;
	}

	public void UpdateTime((float, float) info)
	{
		if (sliderRect == null) return;
		if (image == null) return;
		if (textObject == null) return;

		var f = Mathf.Clamp01(info.Item1 / info.Item2);
		if (f <= nextLimit)
		{
			counter++;
			switch (counter)
			{
				case 1: Play(warning); break;
				case 2: Play(attention); break;
			}
			nextLimit -= step;
		}

		if (info.Item1 != -1 && info.Item1 <= peepLevel)
		{
			peepLevel -= peepDelta;
			peepDelta -= peepDeltaShrink;
			Play(peep);
		}

		var value = info.Item1.ToString("0.0");
		sliderRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, sliderWidth * f);
		var c = Mathf.FloorToInt(f * 3);
		image.color = colors[c == 3 ? 2 : c];
		if (lastValue != value)
		{
			textObject.text = value;
			lastValue = value;
		}
	}

	void Play(AudioClip clip)
	{
		AudioSource.PlayClipAtPoint(clip, transform.position);
	}

	void Update()
	{
		if (planarToCamera)
			transform.rotation = Camera.main.transform.rotation;
	}
}
