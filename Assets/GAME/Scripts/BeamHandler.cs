using BezierSolution;
using UnityEngine;

public class BeamHandler : MonoBehaviour
{
	BezierSpline bezier;
	public ParticleSystem particles;
	AudioSource beamSource;
	public float volume = 0;

	void Start()
	{
		bezier = GetComponent<BezierSpline>();
		bezier[0].gameObject.transform.position = new Vector3(0, -1000, 0);
		bezier[1].gameObject.transform.position = new Vector3(0, -1000, 0);

		beamSource = GetComponentInChildren<AudioSource>();
		beamSource.volume = volume;
	}

	public void SetPoints(Vector3 start, Quaternion startRot, Vector3 end, Vector3 endHandle, Vector3 force)
	{
		if (bezier == null) return;

		particles.transform.position = start;
		bezier[0].gameObject.transform.position = start;
		bezier[0].precedingControlPointLocalPosition = Vector3.zero;
		bezier[0].followingControlPointLocalPosition = startRot * Vector3.forward / 3;
		bezier[1].gameObject.transform.position = end;
		bezier[1].precedingControlPointLocalPosition = endHandle / 3;
		bezier[1].followingControlPointLocalPosition = Vector3.zero;

		beamSource.pitch = 0.4f + volume / 3 + force.magnitude / 6;
	}

	public void RemovePoints()
	{
		if (bezier == null) return;

		bezier[0].gameObject.transform.position = new Vector3(0, -1000, 0);
		bezier[1].gameObject.transform.position = new Vector3(0, -1000, 0);
	}

	public void Update()
	{
		var f = volume > beamSource.volume ? 24 : 8;
		beamSource.volume = (f * beamSource.volume + volume) / (f + 1);
	}
}
