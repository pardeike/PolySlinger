using UnityEngine;
using Valve.VR.InteractionSystem;

public class PortalController : MonoBehaviour
{
	public GameObject testing = null;
	public GameObject[] pieces;
	public GameObject singlePiece;
	public int counter = -1;

	PieceTimer timer;
	GameObject piece = null;
	bool hasCountdown = false;
	bool incomingPiece = false;

	void Awake()
	{
		timer = GetComponentInChildren<PieceTimer>();
		SetTimeVisible(false);
	}

	void Start()
	{
		Invoke(nameof(SpawnPiece), 2);

		if (testing.activeSelf)
		{
			_ = StartCoroutine(0.5f.ExecuteAfterTime(() =>
			{
				for (var i = 0; i < testing.transform.childCount; i++)
				{
					var p = testing.transform.GetChild(i);
					var (pos, rot) = p.GetComponent<Occupation>().NearestTransform();
					var brh = p.GetComponent<BeamReceivingHandler>();
					brh.fixated = true;
					p.gameObject.isStatic = true;
					Tower.Instance.PieceLocked(p.GetComponent<Occupation>());
				}

				var playAreaController = GameObject.Find("PlayArea").GetComponent<PlayAreaController>();
				playAreaController.ReassignTeleportPlaces();
				playAreaController.TeleportToPosition(3, 0, 3);
			}));
		}
	}

	static PortalController _instance;
	public static PortalController Instance
	{
		get
		{
			if (_instance == null)
				_instance = GameObject.Find("PiecePortal").GetComponent<PortalController>();
			return _instance;
		}
	}

	public void SetTimeVisible(bool visible)
	{
		timer.gameObject.SetActive(visible);
	}

	public GameObject InstantiateSinglePiece(Vector3 position)
	{
		var p = Instantiate(singlePiece.transform.GetChild(0).gameObject);
		p.name = $"1Shape";
		p.transform.position = position + new Vector3(-0.25f, 0, -0.25f);
		return p;
	}

	GameObject RandomPiece(int overrideType = -1)
	{
		var i = counter == -1 ? Random.Range(0, pieces.Length) : counter++ % pieces.Length;
		if (overrideType != -1) i = overrideType;
		var variant = pieces[i];
		var type = variant.name.Replace("Shape Variant", "");
		var p = Instantiate(variant.transform.GetChild(0).gameObject);
		p.name = $"{type}Shape";
		p.GetComponent<Rigidbody>().isKinematic = true;
		var pos = transform.position;
		switch (type)
		{
			case "3":
				p.transform.position = pos + new Vector3(0, -0.238f, 0.208f);
				p.transform.rotation = Quaternion.Euler(90, 0, -45);
				break;
			case "I":
				p.transform.position = pos + new Vector3(-0.25f, 0, 0);
				p.transform.rotation = Quaternion.Euler(0, 0, 0);
				break;
			case "L":
				p.transform.position = pos + new Vector3(-0.5f, -0.2f, 0);
				p.transform.rotation = Quaternion.Euler(90, 0, 0);
				break;
			case "O":
				p.transform.position = pos + new Vector3(-0.25f, -0.25f, 0);
				p.transform.rotation = Quaternion.Euler(90, 0, 0);
				break;
			case "S":
				p.transform.position = pos + new Vector3(0, -0.25f, 0);
				p.transform.rotation = Quaternion.Euler(90, 0, 0);
				break;
			case "T":
				p.transform.position = pos + new Vector3(0, -0.2f, 0);
				p.transform.rotation = Quaternion.Euler(90, 0, 0);
				break;
			case "Z1":
				p.transform.position = pos + new Vector3(0, -0.2f, 0.25f);
				p.transform.rotation = Quaternion.Euler(0, 45, -90);
				break;
			case "Z2":
				p.transform.position = pos + new Vector3(0, -0.2f, 0.25f);
				p.transform.rotation = Quaternion.Euler(90, 0, 45);
				break;
		};
		return p;
	}

	public void SpawnPiece()
	{
		piece = RandomPiece();
		var n = piece.name.IndexOf("Shape");
		piece.name = piece.name.Substring(0, n + 5);
		incomingPiece = false;
	}

	public void NextPiece(bool spawnNew = false)
	{
		if (spawnNew && PortalEmpty())
		{
			SpawnPiece();
			piece.SendMessage("StartCountdown");
			hasCountdown = true;
		}
		else
		{
			if (piece != null)
			{
				piece.SendMessage("StartCountdown");
				hasCountdown = true;
			}
		}
	}

	public float CurrentDuration()
	{
		return Tower.Instance.GetCurrentDuration();
	}

	bool PortalEmpty()
	{
		if (incomingPiece) return false;
		if (piece == null) return true;
		var delta = piece.transform.position - transform.position;
		return delta.magnitude > 2f;
	}

	void Update()
	{
		if (piece == null) return;
		if (Tower.Instance.gameEnded) return;

		var py = Player.instance.hmdTransform.position.y;
		var dy = (py - transform.position.y) / 20;
		if (Mathf.Abs(dy) > 0.005f)
		{
			transform.Translate(0, dy, 0, Space.World);
			piece.transform.Translate(0, dy, 0, Space.World);
		}

		if (hasCountdown)
		{
			var timeInfo = piece.GetComponent<Occupation>().TimeLeft;
			if (timeInfo.Item1 < 0)
				SetTimeVisible(false);
			else
			{
				SetTimeVisible(true);
				timer.UpdateTime(timeInfo);
			}
			timer.UpdateTime(timeInfo);
		}

		if (PortalEmpty())
		{
			incomingPiece = true;
			if (hasCountdown == false)
			{
				piece.SendMessage("StartCountdown");
				hasCountdown = true;
			}
			SetTimeVisible(false);
			piece = null;
			Invoke(nameof(SpawnPiece), 1);
		}
	}
}
