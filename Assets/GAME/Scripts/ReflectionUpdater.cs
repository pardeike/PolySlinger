using UnityEngine;

public class ReflectionUpdater : MonoBehaviour
{
	public Camera vrCamera;
	ReflectionProbe probe;

	void Awake()
	{
		probe = GetComponent<ReflectionProbe>();
	}

	void Update()
	{
		var p = vrCamera.transform.position;
		probe.transform.position = new Vector3(p.x, -p.y, p.z);
		_ = probe.RenderProbe();
	}
}
