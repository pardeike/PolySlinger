using UnityEngine;
using UnityEngine.UI;
using Valve.VR.InteractionSystem;

public class BeamReceivingHandler : MonoBehaviour
{
	static readonly float mass = 0.005f;
	static readonly float drag = 10;

	static readonly float factorMultiplier = 2.5f;
	static readonly float factorLowerBound = 0.75f;
	static readonly float factorUpperBound = 4.5f;

	Material outline;
	Material originalMaterial;
	MeshRenderer meshRenderer;

	bool grabbed = false;
	public bool fixated = false;
	public RawImage uiElement = null;

	Rigidbody rigidBody;
	GameObject peekObject = null;

	void Awake()
	{
		if (uiElement != null)
			return;

		meshRenderer = gameObject.GetComponent<MeshRenderer>();
		originalMaterial = meshRenderer.material;
		if (meshRenderer.materials.Length > 1)
		{
			outline = meshRenderer.materials[1];
			outline.color = Color.gray;
		}
		meshRenderer.materials = new Material[] { originalMaterial };
	}

	void Start()
	{
		if (uiElement != null)
			return;

		rigidBody = GetComponent<Rigidbody>();
		rigidBody.mass = mass;
		rigidBody.drag = drag;
		rigidBody.angularDrag = drag;
		rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
	}

	public void PauseState(bool paused)
	{
		if (fixated == false)
		{
			if (paused)
			{
				PeekEnd();
				BeamExit();
			}
		}
		else
			GetComponentsInChildren<MeshRenderer>().ForEach(comp => comp.enabled = paused == false);
	}

	public void BeamEnter()
	{
		if (uiElement != null)
		{
			var color = uiElement.color;
			color.a = 1f;
			uiElement.color = color;
			return;
		}

		if (grabbed || fixated) return;
		meshRenderer.materials = new Material[] { originalMaterial, outline };
	}

	public void GrabStart(GrabMessage msg)
	{
		if (uiElement != null)
		{
			Tower.Instance.SendMessage(uiElement.gameObject.name + "Clicked");
			return;
		}

		if (fixated) return;
		rigidBody.isKinematic = false;

		msg.beam.SetPoints(msg.handPosition, msg.handRotation, msg.anchorPosition, msg.anchorDirection, Vector3.zero);
		msg.objectAnchorPosition = transform.InverseTransformPoint(msg.anchorPosition);
		msg.objectAnchorNormal = transform.InverseTransformPoint(msg.anchorDirection);

		if (outline != null) outline.color = Color.white;
		grabbed = true;

		rigidBody.constraints = RigidbodyConstraints.None;

		SendMessage("GrabContact");
		SendMessage("StartCountdown");
	}

	public void GrabMoved(GrabMessage msg)
	{
		if (fixated) return;

		var anchorGlobal = transform.TransformPoint(msg.objectAnchorPosition);
		var anchorNormalGlobal = transform.TransformPoint(msg.objectAnchorNormal);

		var rotateDelta = msg.handRotation * Quaternion.Inverse(msg.handStartRot);
		var handDelta = msg.handPosition - msg.handStartPos;
		var magnitude = (msg.handPosition - anchorGlobal).magnitude;
		var factor = Mathf.Clamp(magnitude * factorMultiplier, factorLowerBound, factorUpperBound) * Settings.Instance.controllerSensitivity;
		var anchorDestination = RotatePointAroundPivot(msg.anchorPosition, msg.handPosition, rotateDelta) + factor * handDelta;
		var force = anchorDestination - anchorGlobal;
		rigidBody.AddForceAtPosition(force, anchorGlobal);

		msg.beam.SetPoints(msg.handPosition, msg.handRotation, anchorGlobal, anchorNormalGlobal, force);
	}

	public bool LockPosition(bool checkRigidBodySleep = true)
	{
		if (fixated) return false;
		if (checkRigidBodySleep && rigidBody.IsSleeping() == false) return false;

		var ghost = Occupation.CreateGhost(gameObject);
		var isValid = ghost.GetComponent<Occupation>().valid;
		var newPosition = ghost.transform.position;
		var newRotation = ghost.transform.rotation;
		Destroy(ghost);
		if (isValid == false) return false;

		if (peekObject != null)
			Destroy(peekObject);

		transform.position = newPosition;
		transform.rotation = newRotation;

		rigidBody.isKinematic = true;
		meshRenderer.materials = new Material[] { originalMaterial };
		transform.GetChild(0).gameObject.SetActive(true);
		fixated = true;

		Tower.Instance.PieceLocked(GetComponent<Occupation>());
		return true;
	}

	public void PeekStart()
	{
		if (fixated) return;

		if (peekObject != null)
			Destroy(peekObject);
		peekObject = Occupation.CreateGhost(gameObject);
	}

	public void PeekEnd()
	{
		if (fixated) return;

		if (peekObject != null)
			Destroy(peekObject);
		peekObject = null;
	}

	public void GrabEnd()
	{
		if (fixated) return;

		meshRenderer.materials = new Material[] { originalMaterial };
		if (outline != null) outline.color = Color.gray;
		grabbed = false;
	}

	public void BeamExit()
	{
		if (uiElement != null)
		{
			var color = uiElement.color;
			color.a = 0.1f;
			uiElement.color = color;
			return;
		}

		if (grabbed || fixated) return;
		meshRenderer.materials = new Material[] { originalMaterial };
	}

	static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
	{
		return rotation * (point - pivot) + pivot;
	}
}
