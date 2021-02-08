using System;
using System.Collections;
using UnityEngine;

public static class IntVec3Extensions
{
	public static string UppercaseFirst(this string str) => char.ToUpper(str[0]) + str.Substring(1);

	public static GameObject CreateDebugSphere(this IntVec3 vec, GameObject obj)
	{
		var pos = obj.transform.position;
		var debug = GameObject.Find("debug");
		var newDebug = UnityEngine.Object.Instantiate(GameObject.Find("debug"), debug.transform);
		newDebug.transform.localScale = new Vector3(1, 1, 1);
		vec.UpdateDebug(obj, newDebug);
		return newDebug;
	}

	public static Vector3 GetWorldPosition(this IntVec3 vec)
	{
		var tower = Tower.Instance;
		var a = -tower.dimension / 2 / 2;
		var start = new Vector3(a, 0.75f, a);
		return start + new Vector3(vec.x * 0.5f, vec.z * 0.5f, vec.y * 0.5f);
	}

	public static void UpdateDebug(this IntVec3 vec, GameObject obj, GameObject debug)
	{
		debug.transform.position = obj.transform.position + (obj.transform.rotation * vec.Vector3) * 0.5f;
	}
}

public static class FloatExtensions
{
	public static float Lerp(this float value, float inFrom, float inTo, float outFrom, float outTo)
	{
		return Mathf.Lerp(outFrom, outTo, Mathf.InverseLerp(inFrom, inTo, value));
	}

	public static IEnumerator ExecuteAfterTime(this float time, Action task)
	{
		yield return new WaitForSeconds(time);
		task();
	}
}

public static class Vector3Extensions
{
	public static IntVec3 GetIndexPosition(this Vector3 position)
	{
		var tower = Tower.Instance;
		var x = Mathf.FloorToInt((position.x - 0.25f) * 2f + 0.05f) + tower.dimension / 2;
		var y = Mathf.FloorToInt((position.z - 0.25f) * 2f + 0.05f) + tower.dimension / 2;
		var z = Mathf.FloorToInt((position.y - 0.75f) * 2f + 0.05f);
		return new IntVec3 { x = x, y = y, z = z };
	}
}
