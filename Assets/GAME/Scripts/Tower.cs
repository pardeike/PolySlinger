using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Valve.VR;
using Valve.VR.InteractionSystem;
using Random = UnityEngine.Random;

public class Tower : MonoBehaviour
{
	public AudioClip fixate;
	public AudioClip crush;
	public AudioClip over;
	public AudioClip levelup;
	public AudioClip bump;
	public AudioClip suck;

	public AudioSource[] musicPlayer;
	public AudioClip[] musicLoops;
	public float fadeSpeed = 0.01f;

	public GameObject crumbleEffect;
	public Material gameOverSky;
	public bool gameEnded = false;
	public bool canGoToStartScreen = false;
	public GameObject[] gameEndEffects;
	public GameObject qualityMoteEmitter;
	public Texture[] qualityMotes;
	public GameObject result;

	int oldMusic = 1, currentMusic = 0;
	float musicVolume = 0;
	int currentLoop = -1;
	public bool paused = false;

	public int level = 1;
	int score = 0;
	int lifes = 5;
	float startTime;
	int pieceCount = 0;
	readonly int[] qualityCounters = new[] { 0, 0, 0, 0, 0, 0 };
	readonly int[] finalQualityCounterStreaks = new[] { 0, 0, 0, 0, 0, 0 };
	readonly int[] qualityCounterStreaks = new[] { 0, 0, 0, 0, 0, 0 };
	AudioSource ending;
	GameObject teleporting;

	public readonly List<Occupation> lockedPieces = new List<Occupation>();

	public int dimension = 6;
	public int height = 10;
	public bool[][][] cells;

	public float minDuration = 4f;
	public float maxDuration = 60f;
	public int maxLevel = 40;

	static Tower _instance;
	public static Tower Instance
	{
		get
		{
			if (_instance == null)
				_instance = GameObject.Find("Tower").GetComponent<Tower>();
			return _instance;
		}
	}

	void Start()
	{
		musicVolume = Settings.Instance.musicVolume;
		teleporting = GameObject.Find("Teleporting");

		var playAreaController = GameObject.Find("PlayArea").GetComponent<PlayAreaController>();
		playAreaController.ReassignTeleportPlaces(false);
		playAreaController.TeleportToPosition(2, 0, 0); // left of center, furthest row from portal

		var steamVRObjects = GameObject.Find("Player").transform.GetChild(0);
		var extraHeight = Settings.Instance.extraPlayerHeight;
		if (extraHeight != 0)
			steamVRObjects.localPosition = new Vector3(0, extraHeight, 0);

		ending = GetComponent<AudioSource>();

		if (level != 1)
		{
			var overlayController = Player.instance.GetComponentInChildren<OverlayController>();
			overlayController.UpdateLevel(level);
			UpdateResultCanvas();
		}

		startTime = Time.timeSinceLevelLoad;
		PlayMusicLoop();
	}

	public void PauseClicked()
	{
		if (paused)
		{
			Time.timeScale = 1;
			Time.fixedDeltaTime = 1;
			teleporting.SetActive(true);
			AllPieces().ForEach(piece => piece.SendMessage("PauseState", false));
			musicVolume = Settings.Instance.musicVolume;
			paused = false;
			return;
		}

		Time.timeScale = 0;
		Time.fixedDeltaTime = 0;
		teleporting.SetActive(false);
		AllPieces().ForEach(piece => piece.SendMessage("PauseState", true));
		musicVolume = 0;
		paused = true;
	}

	public void QuitClicked()
	{
		Time.timeScale = 1;
		Time.fixedDeltaTime = 1;
		musicVolume = 0;
		SteamVR_LoadLevel.Begin("StartScene", false, 1);
	}

	public bool? this[IntVec3 pos]
	{
		get
		{
			if (pos.x < 0 || pos.x >= dimension) return null;
			if (pos.y < 0 || pos.y >= dimension) return null;
			if (pos.z < 0 || pos.z >= height) return null;
			return cells[pos.x][pos.y][pos.z];
		}
		set
		{
			if (value.HasValue == false) return;
			if (pos.x < 0 || pos.x >= dimension) return;
			if (pos.y < 0 || pos.y >= dimension) return;
			if (pos.z < 0 || pos.z >= height) return;
			cells[pos.x][pos.y][pos.z] = value.Value;
		}
	}

	public void Log(int minZ, int maxZ)
	{
		for (var y = dimension - 1; y >= 0; y--)
		{
			var line = "";
			for (var z = minZ; z <= maxZ; z++)
			{
				for (var x = 0; x < dimension; x++)
					line += (cells[x][y][z] ? "#" : "-"); ;
				line += " ";
			}
			FileLog.Log(line);
		}
	}

	Tower()
	{
		Clear();
	}

	public void PlayMusicLoop()
	{
		var n = Mathf.FloorToInt(musicLoops.Length * (level - 1) / maxLevel);
		if (currentLoop != n)
		{
			currentLoop = n;
			oldMusic = currentMusic;
			currentMusic = 1 - currentMusic;
			musicPlayer[currentMusic].volume = 0;
			musicPlayer[currentMusic].clip = musicLoops[currentLoop];
			musicPlayer[currentMusic].Play();
		}
	}

	public float GetCurrentDuration()
	{
		return maxDuration - (maxDuration - minDuration) * (level - 1) / (maxLevel - 1);
	}

	public IEnumerator Collapse()
	{
		var playAreaController = GameObject.Find("PlayArea").GetComponent<PlayAreaController>();

		var loop = true;
		var placesUpdated = false;
		var combo = 0;
		while (loop)
		{
			loop = false;
			for (var z = 0; z < height; z++)
			{
				if (IsFull(z))
				{
					ShiftDown(z);
					RemoveRowAndSplitPieces(z);
					playAreaController.ReassignTeleportPlaces();
					placesUpdated = true;

					yield return new WaitForSeconds(1);

					combo++;

					score += new[] { 2000, 4000, 8000, 15000, 30000, 50000, 100000 }[combo - 1];
					var overlayController = Player.instance.GetComponentInChildren<OverlayController>();
					overlayController.UpdateScore(score);
					if (lifes < 5) lifes++;
					overlayController.UpdateLifes(lifes);

					loop = true;
					break;
				}
			}
		}
		if (combo > 0)
			LevelUp();

		if (placesUpdated == false)
			playAreaController.ReassignTeleportPlaces();

		yield return new WaitForSeconds(1);
		PortalController.Instance.NextPiece();
	}

	public void PieceLocked(Occupation occupation)
	{
		pieceCount++;

		var quality = occupation.GetQuality();
		qualityCounters[quality]++;
		for (var i = 0; i < qualityCounterStreaks.Length; i++)
		{
			if (i == quality)
			{
				qualityCounterStreaks[i]++;
				finalQualityCounterStreaks[i] = Math.Max(finalQualityCounterStreaks[i], qualityCounterStreaks[i]);
			}
			else
				qualityCounterStreaks[i] = 0;
		}

		var center = occupation.gameObject.GetComponent<Rigidbody>().centerOfMass + occupation.transform.position;
		var mote = Instantiate(qualityMoteEmitter, center + new Vector3(0, 0.5f, 0), Quaternion.identity);
		var particleRenderer = mote.GetComponent<ParticleSystemRenderer>();
		particleRenderer.material.mainTexture = qualityMotes[quality];
		mote.GetComponent<ParticleSystem>().Play();

		UpdateScore(quality, occupation.TimeLeft);

		occupation.Fill();
		lockedPieces.Add(occupation);
		AudioSource.PlayClipAtPoint(fixate, occupation.transform.position);
		_ = StartCoroutine(nameof(Collapse));
	}

	public void PieceDestroyed()
	{
		lifes--;
		var overlayController = Player.instance.GetComponentInChildren<OverlayController>();
		overlayController.UpdateLifes(lifes);
		UpdateResultCanvas();
		if (lifes <= 0)
		{
			gameEnded = true;
			AudioSource.PlayClipAtPoint(over, Player.instance.hmdTransform.position);
			Invoke(nameof(ShowGameOver), 1.5f);
			return;
		}

		PortalController.Instance.SetTimeVisible(false);
		_ = StartCoroutine(1f.ExecuteAfterTime(() =>
		{
			PortalController.Instance.NextPiece(true);
		}));
	}

	Text GetText(string name)
	{
		return result.GetComponentsInChildren<Text>().First(t => t.name == name);
	}

	static string lastTimeStr = "";
	void UpdateResultCanvas(bool timeOnly = false)
	{
		var delta = Mathf.FloorToInt(Time.timeSinceLevelLoad - startTime);
		var deltaSecs = delta % 60;
		var deltaMins = delta / 60;
		var timeStr = $"{deltaMins}:{(deltaSecs < 10 ? "0" + deltaSecs : "" + deltaSecs)}";
		if (lastTimeStr != timeStr)
		{
			lastTimeStr = timeStr;
			GetText("time").text = timeStr;
		}

		if (timeOnly)
			return;

		GetText("score").text = "" + score;
		GetText("level").text = "" + level;
		GetText("count").text = "" + pieceCount;
		for (var i = 1; i <= 5; i++) // ignore worst category (n=0)
		{
			GetText($"q{i}-count").text = "" + qualityCounters[i];
			GetText($"q{i}-streak").text = "" + finalQualityCounterStreaks[i];
		}
	}

	IEnumerable<GameObject> AllPieces()
	{
		return FindObjectsOfType<GameObject>()
			.Where(obj => obj.GetComponent<Occupation>() != null);
	}

	void ShowGameOver()
	{
		ending.Play();

		Destroy(GameObject.Find("Surroundings"));
		Destroy(GameObject.Find("PiecePortal"));

		AllPieces()
			.Select(obj => obj.GetComponent<BeamReceivingHandler>())
			.ForEach(brh =>
			{
				if (brh.fixated == false)
					Destroy(brh.gameObject);
				else
					brh.gameObject.isStatic = true;
			});

		result.transform.Find("Display").Find("Pause").gameObject.SetActive(false);
		result.transform.Find("Display").Find("Quit").gameObject.SetActive(false);

		var b1 = GameObject.Find("BeamLeft");
		if (b1 != null) Destroy(b1);

		var b2 = GameObject.Find("BeamRight");
		if (b2 != null) Destroy(b2);

		var overlayController = Player.instance.GetComponentInChildren<OverlayController>();
		Destroy(overlayController.gameObject);

		RenderSettings.skybox = gameOverSky;
		RenderSettings.ambientIntensity = 0;
		RenderSettings.fog = false;

		FireWork();
		Invoke(nameof(AllowExitToMainScreen), 4f);
	}

	void AllowExitToMainScreen()
	{
		canGoToStartScreen = true;
	}

	public void ExitToMainScreen()
	{
		IEnumerator fadeOutAndExit()
		{
			while (ending.volume > 0.01f)
			{
				ending.volume -= Time.deltaTime / 2;
				yield return null;
			}
			ending.volume = 0;
			ending.Stop();
			SteamVR_LoadLevel.Begin("StartScene", false, 1);
		}
		SteamVR_Fade.Start(Color.black, 2);
		_ = StartCoroutine(fadeOutAndExit());
	}

	void UpdateScore(int quality, (float, float) info)
	{
		var f = info.Item1 == -1 ? 0 : Mathf.Clamp01(info.Item1 / info.Item2);
		var maxItemScore = quality < 5 ? 100 : 400;
		score += Mathf.FloorToInt(quality * maxItemScore * f);
		var overlayController = Player.instance.GetComponentInChildren<OverlayController>();
		overlayController.UpdateScore(score);

		UpdateResultCanvas();
	}

	void LevelUp()
	{
		if (level < maxLevel)
		{
			AudioSource.PlayClipAtPoint(levelup, Player.instance.hmdTransform.position);
			level++;
			var overlayController = Player.instance.GetComponentInChildren<OverlayController>();
			overlayController.UpdateLevel(level);

			UpdateResultCanvas();
			PlayMusicLoop();
		}
	}

	static bool Near(float a, float b) => Mathf.Abs(a - b) < 0.05f;
	static bool NotNear(float a, float b) => Mathf.Abs(a - b) >= 0.05f;

	void RemoveRowAndSplitPieces(int z)
	{
		var yVec = new IntVec3 { x = 0, y = 0, z = z }.GetWorldPosition().y;
		foreach (var occupation in lockedPieces.ToArray())
		{
			var worldPositions = occupation.GetPositions().Select(pos => pos.GetWorldPosition()).ToList();
			var inside = worldPositions.Where(vec => Near(vec.y, yVec)).ToList();
			var outside = worldPositions.Where(vec => NotNear(vec.y, yVec)).ToList();
			if (inside.Count == 0)
				continue;
			var material = occupation.gameObject.GetComponent<MeshRenderer>().material;
			_ = lockedPieces.Remove(occupation);
			Destroy(occupation.gameObject);
			outside.ForEach(vec =>
			{
				var piece = PortalController.Instance.InstantiateSinglePiece(vec);
				piece.GetComponent<MeshRenderer>().material = material;
				lockedPieces.Add(piece.GetComponent<Occupation>());
			});
		}
		foreach (var occupation in lockedPieces.ToArray())
		{
			var worldPositions = occupation.GetPositions().Select(pos => pos.GetWorldPosition()).ToList();
			if (worldPositions.Any(vec => vec.y > yVec))
				occupation.transform.position += new Vector3(0, -0.5f, 0);
		}
		AudioSource.PlayClipAtPoint(crush, Player.instance.hmdTransform.position);
		var ex = Instantiate(crumbleEffect, new Vector3(0, yVec, 0), Quaternion.identity);
		ex.transform.localScale = new Vector3(6, 8, 6);
	}

	public void IterateOverGrid(Action<int, int> action)
	{
		for (var x = 0; x < dimension; x++)
			for (var y = 0; y < dimension; y++)
				action(x, y);
	}

	public void Clear()
	{
		cells = new bool[dimension][][];
		for (var x = 0; x < dimension; x++)
		{
			cells[x] = new bool[dimension][];
			for (var y = 0; y < dimension; y++)
				cells[x][y] = new bool[height];
		}
	}

	public void ShiftDown(int row)
	{
		if (row < 0 || row >= height) return;
		for (var z = row; z < height - 1; z++)
			for (var x = 0; x < dimension; x++)
				for (var y = 0; y < dimension; y++)
					cells[x][y][z] = cells[x][y][z + 1];
		for (var x = 0; x < dimension; x++)
			for (var y = 0; y < dimension; y++)
				cells[x][y][height - 1] = false;
	}

	public bool IsFull(int z)
	{
		if (z < 0 || z >= height) return false;
		for (var x = 0; x < dimension; x++)
			for (var y = 0; y < dimension; y++)
				if (cells[x][y][z] == false)
					return false;
		return true;
	}

	public IEnumerable<IntVec3> GetStandingAreas()
	{
		for (var x = 0; x < dimension; x++)
			for (var y = 0; y < dimension; y++)
				for (var z = 0; z <= height; z++)
					if ((z + 0 == height || cells[x][y][z + 0] == false) && (z == 0 || cells[x][y][z - 1] == true))
						if (z + 1 >= height || cells[x][y][z + 1] == false)
							if (z + 2 >= height || cells[x][y][z + 2] == false)
								if (z + 3 >= height || cells[x][y][z + 3] == false)
									yield return new IntVec3 { x = x, y = y, z = z };
	}

	static Material[] ghostMaterials = null;
	public Material[] GhostMaterials
	{
		get
		{
			if (ghostMaterials == null)
				ghostMaterials = GameObject.Find("Peek").GetComponent<MeshRenderer>().materials;
			return ghostMaterials;
		}
	}

	void FireWork()
	{
		var angle = Random.Range(0, 360);
		var radius = Random.Range(8f, 30f);
		var pos = new Vector3(Mathf.Cos(angle) * radius, 1, Mathf.Sin(angle) * radius);
		_ = Instantiate(gameEndEffects[Random.Range(0, gameEndEffects.Length)], pos, Quaternion.identity);
		if (ending.isPlaying)
			Invoke(nameof(FireWork), Random.Range(0.1f, 1.5f));
	}

	void Update()
	{
		var py = Player.instance.hmdTransform.position.y;
		var dy = (py - result.transform.position.y) / 20;
		if (Mathf.Abs(dy) > 0.005f)
			result.transform.Translate(0, dy, 0, Space.World);

		float vol;

		if (gameEnded)
		{
			vol = musicPlayer[oldMusic].volume;
			if (vol > 0)
				musicPlayer[oldMusic].volume = vol - fadeSpeed;

			vol = musicPlayer[currentMusic].volume;
			if (vol > 0)
				musicPlayer[currentMusic].volume = vol - fadeSpeed;

			return;
		}

		UpdateResultCanvas(true);

		vol = musicPlayer[oldMusic].volume;
		if (vol > 0)
			musicPlayer[oldMusic].volume = vol - fadeSpeed;

		vol = musicPlayer[currentMusic].volume;
		if (vol < musicVolume)
			musicPlayer[currentMusic].volume = vol + fadeSpeed;
		else if (vol > musicVolume)
			musicPlayer[currentMusic].volume = vol - fadeSpeed;
	}

	~Tower()
	{
		Dispose(false);
	}

	public void Dispose()
	{
		Dispose(true);
		System.GC.SuppressFinalize(this);
	}

	private void Dispose(bool _)
	{
		_instance = null;
	}
}
