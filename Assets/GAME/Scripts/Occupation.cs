using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Valve.VR.InteractionSystem;

public class Occupation : MonoBehaviour
{
	public bool valid;

	Transform[] children;

	float duration;
	float endTime = -1;
	int connectCount = 9;
	float lastCollisionTime = 0;

	void Awake()
	{
		children = GetComponentsInChildren<MeshRenderer>(true).Skip(1)
			.Select(renderer => renderer.transform).ToArray();
	}

	public void StartCountdown()
	{
		if (endTime == -1)
		{
			duration = PortalController.Instance.CurrentDuration();
			endTime = Time.timeSinceLevelLoad + duration;
		}
	}

	public void GrabContact()
	{
		AudioSource.PlayClipAtPoint(Tower.Instance.suck, transform.position);
		if (connectCount > 0)
			connectCount--;
	}

	public int GetQuality()
	{
		return connectCount switch
		{
			1 => 1,
			2 => 1,
			3 => 2,
			4 => 2,
			5 => 3,
			6 => 3,
			7 => 4,
			8 => 5,
			_ => 0,
		};
	}

	void OnCollisionEnter(Collision collisionInfo)
	{
		var handler = collisionInfo.collider.GetComponent<BeamReceivingHandler>();
		if (handler != null && handler.fixated)
		{
			var force = collisionInfo.relativeVelocity.magnitude;
			var time = Time.timeSinceLevelLoad;
			if (collisionInfo.contactCount > 0 && force > 0.25f && time > lastCollisionTime + 0.25f)
			{
				lastCollisionTime = time;
				AudioSource.PlayClipAtPoint(Tower.Instance.bump, collisionInfo.GetContact(0).point);
			}
		}
	}

	void Update()
	{
		if (Tower.Instance.gameEnded) return;

		if (TimeLeft.Item1 == 0)
		{
			endTime = -1;
			valid = gameObject.GetComponent<BeamReceivingHandler>().LockPosition();
			if (valid == false)
			{
				GetComponent<BeamReceivingHandler>().PeekEnd();
				GameObject.Find("PlayArea").GetComponent<PlayAreaController>().DestroyPiece(gameObject);
				Tower.Instance.PieceDestroyed();
				return;
			}
		}
	}

	public IEnumerable<IntVec3> GetPositions()
	{
		return children.Select(child => child.position.GetIndexPosition());
	}

	public void Fill()
	{
		var tower = Tower.Instance;
		foreach (var xyz in GetPositions())
			tower[xyz] = true;
		endTime = -1;
	}

	public (float, float) TimeLeft
	{
		get
		{
			if (endTime == -1)
				return (-1, -1);
			return (Mathf.Max(0, endTime - Time.timeSinceLevelLoad), duration);
		}
	}

	public void UpdateValidity()
	{
		var tower = Tower.Instance;
		valid = GetPositions().Select(xyz => tower[xyz])
			.All(b => b.HasValue && b.Value == false);
	}

	static readonly Vector3 groundOffset = new Vector3(-0.25f, -0.75f, -0.25f);
	public (Vector3, Quaternion) NearestTransform()
	{
		var p = transform.position + groundOffset;
		var x = Mathf.Round(p.x * 2) / 2;
		var y = Mathf.Round(p.y * 2) / 2;
		var z = Mathf.Round(p.z * 2) / 2;
		var pos = new Vector3(x, y, z) - groundOffset;

		var r = transform.rotation.eulerAngles;
		x = Mathf.Round(r.x / 90) * 90;
		y = Mathf.Round(r.y / 90) * 90;
		z = Mathf.Round(r.z / 90) * 90;
		var rot = Quaternion.Euler(new Vector3(x, y, z));

		return (pos, rot);
	}

	public static GameObject CreateGhost(GameObject original)
	{
		var (pos, rot) = original.GetComponent<Occupation>().NearestTransform();
		var ghost = Instantiate(original, pos, rot);
		ghost.name = "Ghost";
		var occupation = ghost.GetComponent<Occupation>();
		occupation.UpdateValidity();
		ghost.layer = 0;
		ghost.GetComponent<MeshRenderer>().material = Tower.Instance.GhostMaterials[occupation.valid ? 1 : 0];
		ghost.GetComponentsInChildren<BoxCollider>().ForEach(collider => Destroy(collider));
		Destroy(ghost.GetComponent<Rigidbody>());
		var handler = ghost.GetComponent<BeamReceivingHandler>();
		handler.fixated = true;
		return ghost;
	}
}
