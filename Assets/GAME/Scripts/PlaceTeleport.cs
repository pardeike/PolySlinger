using UnityEngine;
using Valve.VR.InteractionSystem;

public class PlaceTeleport : TeleportMarkerBase
{
	public bool playerSpawnPoint = false; // override base

	GameObject marker;
	GameObject arrow;
	float arrowY;
	Player player;
	Vector3 lookAtPosition = Vector3.zero;
	int x, y, z;

	public override bool showReticle => false;

	public IntVec3 Pos => new IntVec3 { x = x, y = y, z = z };

	void Start()
	{
		player = Player.instance;

		GetRelevantComponents();
		UpdateVisuals();
		gameObject.SetActive(false);
	}

	void Update()
	{
		if (Tower.Instance.gameEnded) return;

		if (Application.isPlaying && arrow != null)
		{
			lookAtPosition.x = player.hmdTransform.position.x;
			lookAtPosition.y = arrowY;
			lookAtPosition.z = player.hmdTransform.position.z;
			arrow.transform.LookAt(lookAtPosition);
		}
	}

	public void Assign(IntVec3 pos)
	{
		x = pos.x;
		y = pos.y;
		z = pos.z;

		var tower = Tower.Instance;
		var start = -(tower.dimension / 2) / 2 - 0.25f;
		var step = 0.5f;
		var ground = transform.position.y;
		transform.position = new Vector3(start + step * x, ground + step * z, start + step * y);
	}

	public override bool ShouldActivate(Vector3 playerPosition) => true;

	public override bool ShouldMovePlayer()
	{
		GameObject.Find("PlayArea").GetComponent<PlayAreaController>().SetLastPlayerTeleportLocation(x, y, z);
		return true;
	}

	public override void Highlight(bool highlight)
	{
		if (arrow != null)
			arrow.SetActive(highlight);
	}

	public override void UpdateVisuals()
	{
		if (marker != null)
			marker.SetActive(markerActive);
	}

	public void GetRelevantComponents()
	{
		marker = transform.Find("Marker").gameObject;
		arrow = transform.Find("Arrow").gameObject;
		arrowY = arrow.transform.position.y;
	}

	public void ReleaseRelevantComponents()
	{
		marker = null;
		arrow = null;
	}

	public void UpdateVisualsInEditor()
	{
		if (Application.isPlaying)
			return;

		GetRelevantComponents();
		if (arrow != null)
			arrow.SetActive(true);
		ReleaseRelevantComponents();
	}

	public override void SetAlpha(float tintAlpha, float alphaPercent)
	{
	}
}
