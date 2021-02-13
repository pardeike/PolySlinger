
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

public class GrabMessage
{
	public BeamHandler beam;
	public Vector3 handStartPos;
	public Quaternion handStartRot;
	public Vector3 handPosition;
	public Quaternion handRotation;
	public Vector3 bodyStart;
	public Vector3 bodyPosition;
	public Vector3 anchorPosition;
	public Vector3 anchorDirection;
	public Vector3 destination;
	public Vector3 objectAnchorPosition;
	public Vector3 objectAnchorNormal;

	public GrabMessage(BeamHandler beamHandler, Vector3 hand, Quaternion startRotation, RaycastHit hit)
	{
		beam = beamHandler;
		handStartPos = hand;
		handStartRot = startRotation;
		handPosition = hand;
		handRotation = startRotation;
		anchorPosition = hit.point;
		anchorDirection = hit.normal;
		destination = Vector3.zero;
	}
}

public class Pointer : MonoBehaviour
{
	public Material material;
	public float thickness = 0.001f;
	public SteamVR_Action_Boolean GrabAction;
	public SteamVR_Action_Boolean FixateAction;
	public SteamVR_Action_Boolean PeekAction;
	public SteamVR_Input_Sources handType;

	public GameObject beamObject;
	BeamHandler beamHandler;
	GameObject beam;
	GameObject holder;
	GameObject pointer;
	OverlayController overlayController;

	GameObject target = null;
	bool grabbing = false;
	Vector3 bodyGrabStartPosition = Vector3.zero;
	GrabMessage grabInfo = null;
	int rayLayer;

	public void Awake()
	{
		beamHandler = beamObject.GetComponent<BeamHandler>();
		beam = beamObject.GetComponentInChildren<ParticleSystem>().gameObject;

		holder = new GameObject("Holder");
		holder.transform.parent = transform;
		holder.transform.localPosition = Vector3.zero;
		holder.transform.localRotation = Quaternion.identity;
		holder.gameObject.SetActive(false);
		beam.SetActive(false);
		beamHandler.volume = 0;

		pointer = GameObject.CreatePrimitive(PrimitiveType.Cube);
		Destroy(pointer.GetComponent<BoxCollider>());
		Destroy(pointer.GetComponent<Rigidbody>());
		pointer.name = "Pointer";
		pointer.transform.parent = holder.transform;
		pointer.transform.localPosition = new Vector3(0f, 0f, 50f);
		pointer.transform.localScale = new Vector3(thickness, thickness, 100f);

		pointer.GetComponent<MeshRenderer>().material = material;
		overlayController = Player.instance.GetComponentInChildren<OverlayController>();

		GrabAction.AddOnStateDownListener(Grab, handType);
		GrabAction.AddOnStateUpListener(Ungrab, handType);
		FixateAction.AddOnStateDownListener(Fixate, handType);
		PeekAction.AddOnStateDownListener(PeekStart, handType);
		PeekAction.AddOnStateUpListener(PeekEnd, handType);

		rayLayer = LayerMask.GetMask("Ray");
	}

	void Start()
	{
		SteamVR_Utils.Event.Listen("update_pointer", UpdatePointer);
		SteamVR_Utils.Event.Listen("rotation_event", RotationEvent);
		transform.Find("Holder").transform.localEulerAngles = new Vector3(Settings.Instance.controllerAngle, 0, 0);
	}

	void UpdatePointer(params object[] args)
	{
		if (holder != null && holder.gameObject != null)
			holder.gameObject.SetActive((bool)args[0]);
	}

	void RotationEvent(params object[] args)
	{
		if (grabInfo == null) return;
		try
		{
			if ((bool)args[0] == false)
				grabInfo.handStartPos = transform.position;
		}
		catch { }
	}

	public void OnEnable()
	{
		holder.gameObject.SetActive(true);
		SteamVR_Utils.Event.Listen("input_focus", OnInputFocus);
	}

	public void OnDisable()
	{
		holder.gameObject.SetActive(false);
		SteamVR_Utils.Event.Remove("input_focus", OnInputFocus);
	}

	void OnInputFocus(params object[] args)
	{
		var hasFocus = (bool)args[0];
		holder.gameObject.SetActive(hasFocus && enabled);
	}

	Vector3 BodyPosition()
	{
		return Player.instance.feetPositionGuess;
	}

	public void Grab(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
	{
		var tower = Tower.Instance;
		if (tower.gameEnded && tower.canGoToStartScreen)
		{
			tower.ExitToMainScreen();
			return;
		}

		if (target == null) return;
		if (grabInfo == null) return;
		target.SendMessage("GrabStart", grabInfo, SendMessageOptions.DontRequireReceiver);
		grabInfo.beam.SetPoints(Vector3.zero, Quaternion.identity, Vector3.zero, Vector3.zero, Vector3.zero);
		SetGrabStatus(true);
		overlayController.SetTrackedPiece(target.GetComponent<Occupation>());
		bodyGrabStartPosition = BodyPosition();
	}

	public void Ungrab(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
	{
		if (target == null) return;
		target.SendMessage("GrabEnd", null, SendMessageOptions.DontRequireReceiver);
		SetGrabStatus(false);
		overlayController.SetTrackedPiece(null);
		bodyGrabStartPosition = Vector3.zero;
		target = null;
	}

	public void Fixate(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
	{
		if (target == null) return;
		if (Tower.Instance.paused) return;
		target.SendMessage("LockPosition", true, SendMessageOptions.DontRequireReceiver);
	}

	public void PeekStart(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
	{
		if (target == null) return;
		if (Tower.Instance.paused) return;
		target.SendMessage("PeekStart", null, SendMessageOptions.DontRequireReceiver);
	}

	public void PeekEnd(SteamVR_Action_Boolean action, SteamVR_Input_Sources source)
	{
		if (target == null) return;
		if (Tower.Instance.paused) return;
		target.SendMessage("PeekEnd", null, SendMessageOptions.DontRequireReceiver);
	}

	void SetGrabStatus(bool grabbing)
	{
		holder.gameObject.SetActive(grabbing == false);
		beam.SetActive(grabbing);
		beamHandler.volume = grabbing ? 1 : 0;
		this.grabbing = grabbing;
	}

	void Update()
	{
		var tower = Tower.Instance;
		if (tower.gameEnded) return;

		var pos = transform.position;
		var rot = pointer.transform.rotation;

		if (grabbing)
		{
			if (target == null || tower.paused)
			{
				SetGrabStatus(false);
				return;
			}

			grabInfo.handPosition = pos;
			grabInfo.handRotation = rot;
			grabInfo.bodyStart = bodyGrabStartPosition;
			grabInfo.bodyPosition = BodyPosition();
			target.SendMessage("GrabMoved", grabInfo, SendMessageOptions.DontRequireReceiver);
			return;
		}

		var raycast = new Ray(pos, pos + 100f * (rot * Vector3.forward));
		if (Physics.Raycast(raycast, out var hitInfo, 100, rayLayer))
		{
			grabInfo = new GrabMessage(beamHandler, pos, rot, hitInfo);

			var obj = hitInfo.transform.gameObject;
			if (obj != target)
			{
				if (target != null)
					target.SendMessage("BeamExit", null, SendMessageOptions.DontRequireReceiver);

				var brh = obj.GetComponent<BeamReceivingHandler>();
				var inUIElement = brh != null && brh.uiElement != null;
				var inPiece = tower.paused == false && obj.GetComponent<Occupation>() != null;
				if (inUIElement || inPiece)
				{
					obj.SendMessage("BeamEnter", null, SendMessageOptions.DontRequireReceiver);
					target = obj;
				}
			}
		}
		else
		{
			if (target != null)
			{
				target.SendMessage("BeamExit", null, SendMessageOptions.DontRequireReceiver);
				target = null;
			}
		}
	}
}
