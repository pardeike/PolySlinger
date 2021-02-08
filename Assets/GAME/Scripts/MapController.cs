using UnityEngine;
//using System.Linq;
//using Valve.VR.InteractionSystem;

// for debugging the play area as a map attached to the right hand
//
public class MapController : MonoBehaviour
{
	/*
	readonly int maxH = 4;

	public static void Refresh()
	{
		Player.instance.rightHand.GetComponentInChildren<MapController>().RefreshLocally();
	}

	void RefreshLocally()
	{
		foreach (Transform child in transform)
			Destroy(child.gameObject);

		var tower = Tower.Instance;
		var dim = tower.dimension;
		var height = tower.height;
		if (maxH < height) height = maxH;
		var size = new Vector3(0.02f, 0.02f, 0.02f);

		for (var x = 0; x < dim; x++)
			for (var y = 0; y < dim; y++)
				for (var z = 0; z < height; z++)
				{
					var pos = new IntVec3 { x = x, y = y, z = z };
					var vec = pos.GetWorldPosition();
					var filled = tower.cells[x][y][z];

					GameObject piece = null;
					foreach (var occupation in tower.lockedPieces.ToArray())
					{
						foreach (var wpos in occupation.GetPositions().Select(pos => pos.GetWorldPosition()))
						{
							if (wpos.x == vec.x && wpos.y == vec.y && wpos.z == vec.z)
							{
								piece = occupation.gameObject;
								break;
							}
						}
						if (piece != null) break;
					}

					var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					sphere.transform.parent = transform;
					sphere.transform.localPosition = pos.GetWorldPosition() / 10f;
					sphere.transform.localScale = piece == null ? size / 4 : size;
					var c = piece == null ? Color.white : piece.GetComponent<MeshRenderer>().material.color;
					c.a = filled ? 1 : 0.5f;
					sphere.GetComponent<MeshRenderer>().material.color = c;
				}
	}
	*/
}
