using UnityEngine;

public class TeleportStateObserver : MonoBehaviour
{
	void OnEnable()
	{
		var name = gameObject.name;
		if (name == "OnActivate")
			SteamVR_Utils.Event.Send("update_pointer", false);
		HandleRotate(name, true);
	}

	void OnDisable()
	{
		var name = gameObject.name;
		if (name == "OnDeactivate")
			SteamVR_Utils.Event.Send("update_pointer", true);
		HandleRotate(name, false);
	}

	void HandleRotate(string name, bool state)
	{
		if (name == "Left" || name == "Right")
			SteamVR_Utils.Event.Send("rotation_event", state);
	}
}
