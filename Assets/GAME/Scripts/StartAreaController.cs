using Assets.GAME.Scripts;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class StartAreaController : MonoBehaviour
{
	public bool fadeIn = true;

	public AudioClip sound;
	public AudioClip[] sparks;

	public GameObject information;
	public GameObject infoButtons;
	public GameObject settingsButtons;
	public GameObject settingsSlider;
	public Texture[] infoPanels;
	int infoPanelIndex = 0;
	int settingsIndex = 0;
	public Texture[] settingsIcons;

	bool needsPlay = true;
	Transform logo;

	Transform buttonsPanel;
	Transform[] buttons;
	GameObject caption;

	Transform[] pieces;
	Light[] logoLights;
	float[] maxIntensity;
	readonly int[] lightOrder = new[] { 1, 3, 0, 2, 4 };
	readonly bool[] lightsOn = new[] { false, false, false, false, false };

	Light centerLight;
	float centerLightIntensity;

	int logoInCounter = 0;
	float worldFadeIn = 0;

	void Awake()
	{
		if (GlobalState.hasSeenStartScene)
			fadeIn = false;
		GlobalState.hasSeenStartScene = true;

		// mirror and prepare pieces for rotation
		var upper = transform.GetChild(1);
		var lower = Instantiate(upper);
		lower.transform.localScale = new Vector3(1, -1, 1);
		pieces = Children(upper).Union(Children(lower)).ToArray();

		// logo lights
		logo = GameObject.Find("PolySlingerLogo").transform;
		logoLights = logo.GetComponentsInChildren<Light>().ToArray();
		maxIntensity = logoLights.Select(light => light.intensity).ToArray();
		if (fadeIn) logoLights.ForEach(light => light.intensity = 0);

		// center light
		centerLight = GameObject.Find("CenterLight").GetComponent<Light>();
		centerLightIntensity = centerLight.intensity;
		if (fadeIn) centerLight.intensity = 0;

		// buttons
		buttonsPanel = GameObject.Find("Buttons").transform;
		buttons = Children(buttonsPanel).Where(child => child.GetComponent<PushButtton>() != null).ToArray();
		if (fadeIn) buttons.ForEach(button => button.localScale = Vector3.zero);

		// caption
		caption = logo.GetComponentInChildren<Canvas>().gameObject;
		if (fadeIn) caption.SetActive(false);

		if (fadeIn)
		{
			RenderSettings.skybox.SetFloat("_Exposure", 0);
			RenderSettings.reflectionIntensity = 0;
			RenderSettings.ambientIntensity = 0;
		}
		else
		{
			logoInCounter = int.MaxValue;
			worldFadeIn = 1;
		}
	}

	IEnumerable<Transform> Children(Transform parent)
	{
		for (int i = 0; i < parent.childCount; i++)
			yield return parent.GetChild(i);
	}

	void ShowCaption()
	{
		caption.SetActive(true);
	}

	// Update is called once per frame
	void Update()
	{
		for (int i = 0; i < pieces.Length; i++)
		{
			var m = i < pieces.Length / 2 ? 1 : -1;
			var q = Quaternion.Euler(0, m * (i % 2 == 0 ? 0.2f : -0.2f), 0);
			if (i == 5 || i == 12) q = Quaternion.Euler(0, 0, 0.2f);
			pieces[i].localRotation *= q;
		}

		var pos = buttonsPanel.transform.position;
		var py = Player.instance.hmdTransform.position.y;
		buttonsPanel.transform.position = new Vector3(pos.x, py - 0.1f - py / 7, pos.z);

		var interval = 160f;
		var intervalOffset = interval / 3;
		var delay = 350f;
		var max = delay + (logoLights.Length - 1) * intervalOffset + interval;
		if (++logoInCounter <= max)
		{
			for (var i = 0; i < logoLights.Length; i++)
			{
				var j = lightOrder[i];
				var start = delay + i * intervalOffset;
				var end = start + interval;
				if (logoInCounter > start && logoInCounter <= end)
				{
					if (needsPlay)
					{
						needsPlay = false;
						AudioSource.PlayClipAtPoint(sound, Player.instance.transform.position);
						if (fadeIn)
							Invoke(nameof(ShowCaption), 3f);
					}

					var x = (logoInCounter - start) / interval;
					var f = 10 * (Mathf.Abs(Mathf.Sin(11 * Mathf.Sqrt(x))) - 0.9f);
					if (f > 0.6f)
					{
						if (lightsOn[i] == false)
						{
							lightsOn[i] = true;
							AudioSource.PlayClipAtPoint(sparks[Random.Range(0, sparks.Length)], logo.position);
						}
					}
					else
						lightsOn[i] = false;
					logoLights[j].intensity = maxIntensity[j] * f;
				}
			}
			if (logoInCounter < 600)
				return;
		}

		if (logoInCounter == int.MaxValue)
			return;

		worldFadeIn = Mathf.Min(1, worldFadeIn + 0.005f);
		if (worldFadeIn <= 1)
		{
			RenderSettings.skybox.SetFloat("_Exposure", worldFadeIn);
			centerLight.intensity = Mathf.Lerp(0, centerLightIntensity, worldFadeIn);
			RenderSettings.reflectionIntensity = worldFadeIn;
			RenderSettings.ambientIntensity = worldFadeIn;

			if (worldFadeIn >= 0.95)
			{
				var f = (worldFadeIn - 0.95f) * 20;
				buttons.ForEach(button => button.localScale = new Vector3(0.2f, 0.2f, 0.01f) * f);
			}
		}
	}

	public void StartButtonPressed()
	{
		SteamVR_LoadLevel.Begin("MainScene", false, 1);
	}

	public void InfoButtonPressed()
	{
		information.SetActive(true);
		infoButtons.SetActive(true);
	}

	public void InfoPrevPressed()
	{
		infoPanelIndex = (infoPanels.Length + infoPanelIndex - 1) % infoPanels.Length;
		information.transform.GetChild(1).GetComponent<RawImage>().texture = infoPanels[infoPanelIndex];
	}

	public void InfoNextPressed()
	{
		infoPanelIndex = (infoPanels.Length + infoPanelIndex + 1) % infoPanels.Length;
		information.transform.GetChild(1).GetComponent<RawImage>().texture = infoPanels[infoPanelIndex];
	}

	public void InfoClosePressed()
	{
		information.SetActive(false);
		infoButtons.SetActive(false);
	}

	void UpdateSlider()
	{
		var slider = settingsSlider.GetComponent<SliderRange>();
		var (min, max) = Settings.Instance.GetFloatRange(settingsIndex);
		var current = Settings.Instance.GetFloatValue(settingsIndex);
		slider.icon = settingsIcons[settingsIndex];
		slider.Prepare(
			min,
			max,
			current,
			(value) => Settings.Instance.DisplayString(settingsIndex),
			(value) => Settings.Instance.SetFloatValue(settingsIndex, value)
		);
	}

	public void SettingsButtonPressed()
	{
		settingsButtons.SetActive(true);
		UpdateSlider();
	}

	public void SettingsPrevPressed()
	{
		settingsIndex = (Settings.Instance.Count + settingsIndex - 1) % Settings.Instance.Count;
		UpdateSlider();
	}

	public void SettingsNextPressed()
	{
		settingsIndex = (Settings.Instance.Count + settingsIndex + 1) % Settings.Instance.Count;
		UpdateSlider();
	}

	public void SettingsClosePressed()
	{
		settingsButtons.SetActive(false);
	}

	public void QuitButtonPressed()
	{
#if UNITY_EDITOR
		StartCoroutine(2f.ExecuteAfterTime(() =>
		{
			UnityEditor.EditorApplication.isPlaying = false;
		}));
#else
         Application.Quit();
#endif
	}
}
