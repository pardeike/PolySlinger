using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class BlobController : MonoBehaviour
{
	readonly bool REGENERATE = false;

	const int count = 9;
	const float scale = 0.25f;

	void Start()
	{
		if (REGENERATE == false) return;

		var template = transform.GetChild(0);
		var layerTemplate = template.gameObject.layer;
		var rigidBodyTemplate = template.GetComponent<Rigidbody>();
		var springJointTemplate = template.GetComponent<SpringJoint>();
		var templateColliderRadius = template.GetComponent<SphereCollider>().radius;

		foreach (Transform child in transform)
			Destroy(child.gameObject);

		var colliders = new List<SphereCollider>();

		var gr = (Mathf.Sqrt(5f) + 1f) / 2f;
		var ga = (2f - gr) * (2f * Mathf.PI);
		for (var i = 1; i <= count; i++)
		{
			var lat = Mathf.Asin(-1f + 2f * i / (count + 1));
			var lon = ga * i;

			var x = Mathf.Cos(lon) * Mathf.Cos(lat) * scale;
			var y = Mathf.Sin(lon) * Mathf.Cos(lat) * scale;
			var z = Mathf.Sin(lat) * scale;

			var ball = new GameObject("mb" + i);
			var collider = ball.AddComponent<SphereCollider>();
			collider.radius = templateColliderRadius;
			colliders.Add(collider);
			ball.transform.SetParent(transform);
			ball.transform.localPosition = new Vector3(x, y, z);
			ball.transform.localScale = Vector3.one;
			_ = AttachSpring(ball, layerTemplate, rigidBodyTemplate, springJointTemplate);
		}

		var mcBlob = GetComponent<MCBlob>();
		var f_BlobObjectsLocations = typeof(MCBlob).GetField("BlobObjectsLocations", BindingFlags.NonPublic | BindingFlags.Instance);
		f_BlobObjectsLocations.SetValue(mcBlob, colliders.ToArray());
	}

	SpringJoint AttachSpring(GameObject obj, int layer, Rigidbody rigidBody, SpringJoint spring)
	{
		obj.layer = layer;
		var rb = CopyComponent(rigidBody, obj);
		var newSpring = CopyComponent(spring, obj);
		newSpring.autoConfigureConnectedAnchor = false;
		newSpring.connectedAnchor = obj.transform.localPosition;
		return newSpring;
	}

	T CopyComponent<T>(T original, GameObject destination) where T : Component
	{
		var type = original.GetType();
		var dst = destination.GetComponent(type) as T;
		if (!dst) dst = destination.AddComponent(type) as T;
		var fields = type.GetFields();
		foreach (var field in fields)
		{
			if (field.IsStatic) continue;
			if (field.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length > 0) continue;
			field.SetValue(dst, field.GetValue(original));
		}
		var props = type.GetProperties();
		foreach (var prop in props)
		{
			if (!prop.CanWrite || !prop.CanWrite || prop.Name == "name") continue;
			if (prop.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length > 0) continue;
			prop.SetValue(dst, prop.GetValue(original, null), null);
		}
		return dst;
	}
}
