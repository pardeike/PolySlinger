using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class PlayAreaController : MonoBehaviour
{
	public GameObject explosion;
	public AudioClip boom;
	public GameObject place;

	readonly List<PlaceTeleport> teleports = new List<PlaceTeleport>();
	IntVec3 lastPlayerTeleportLocation = new IntVec3 { x = -1, y = -1, z = -1 };

	public void SetLastPlayerTeleportLocation(int x, int y, int z)
	{
		lastPlayerTeleportLocation = new IntVec3 { x = x, y = y, z = z };
	}

	public void TeleportToPosition(int x, int y, int h)
	{
		if (lastPlayerTeleportLocation.x == x && lastPlayerTeleportLocation.y == y && lastPlayerTeleportLocation.z == h)
			return;
		SetLastPlayerTeleportLocation(x, y, h);

		var player = Player.instance;

		var position = new Vector3(-1.25f + x * 0.5f, 0.51f + h * 0.5f, -1.25f + y * 0.5f);
		var teleport = teleports.FirstOrDefault(tp => (tp.transform.position - position).magnitude < 0.1f);
		if (teleport != null)
		{
			var playerFeetOffset = player.trackingOriginTransform.position - player.feetPositionGuess;
			player.trackingOriginTransform.position = teleport.transform.position + playerFeetOffset;

			if (player.leftHand.currentAttachedObjectInfo.HasValue)
				player.leftHand.ResetAttachedTransform(player.leftHand.currentAttachedObjectInfo.Value);
			if (player.rightHand.currentAttachedObjectInfo.HasValue)
				player.rightHand.ResetAttachedTransform(player.rightHand.currentAttachedObjectInfo.Value);

			var x2 = Mathf.RoundToInt((position.x + 1.25f) / 0.5f);
			var y2 = Mathf.RoundToInt((position.z + 1.25f) / 0.5f);
		}
	}

	public void AdjustPlayerPosition()
	{
		var x = lastPlayerTeleportLocation.x;
		var y = lastPlayerTeleportLocation.y;

		var tower = Tower.Instance;
		var h = tower.height;
		while (h >= 0)
		{
			var full = h == 0 || tower.cells[x][y][h - 1];
			if (full) break;
			h--;
		}
		TeleportToPosition(x, y, h);
	}

	public void DestroyPiece(GameObject piece)
	{
		var pos = piece.transform.position;
		var ex = Instantiate(explosion, pos, Quaternion.identity);
		ex.transform.localScale = Vector3.one * 0.35f;
		AudioSource.PlayClipAtPoint(boom, pos);
		Destroy(piece);
	}

	public void ReassignTeleportPlaces(bool adjustPlayer = true)
	{
		var tower = Tower.Instance;
		var current = new HashSet<IntVec3>(tower.GetStandingAreas());

		teleports.Clear();

		var places = transform.Find("Places").gameObject;
		var n = places.transform.childCount;
		for (var i = 0; i < n; i++)
		{
			var place = places.transform.GetChild(i).gameObject;
			var teleport = place.GetComponent<PlaceTeleport>();
			if (current.Contains(teleport.Pos) == false)
				Destroy(place);
			else
			{
				teleports.Add(teleport);
				_ = current.Remove(teleport.Pos);
			}
		}
		foreach (var pos in current)
		{
			var newPlace = Instantiate(place, places.transform);
			newPlace.name = $"{pos.x + 1}.{pos.y + 1}.{pos.z + 1}";
			var teleport = newPlace.GetComponent<PlaceTeleport>();
			teleport.Assign(pos);
			teleports.Add(teleport);
		}

		// refresh all teleport markers in <Valve.VR.InteractionSystem.Teleport>
		// because they are cached
		var tpObject = GameObject.Find("Teleporting").GetComponent<Teleport>();
		var tpMarkers = tpObject.GetType().GetField("teleportMarkers", BindingFlags.NonPublic | BindingFlags.Instance);
		tpMarkers.SetValue(tpObject, teleports.ToArray());

		if (adjustPlayer)
			AdjustPlayerPosition();

		// MapController.Refresh();
	}
}
