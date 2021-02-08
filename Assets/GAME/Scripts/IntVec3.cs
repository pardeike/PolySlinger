using UnityEngine;

public struct IntVec3
{
	public int x;
	public int y;
	public int z;

	public static IntVec3 operator +(IntVec3 a, IntVec3 b) => new IntVec3 { x = a.x + b.x, y = a.y + b.y, z = a.z + b.z };
	public static IntVec3 operator -(IntVec3 a, IntVec3 b) => new IntVec3 { x = a.x - b.x, y = a.y - b.y, z = a.z - b.z };

	public IntVec3 Apply(Quaternion rot)
	{
		var p = rot * new Vector3(x, y, z);
		var x2 = Mathf.FloorToInt(p.x + 0.1f);
		var z2 = Mathf.FloorToInt(p.y + 0.1f);
		var y2 = Mathf.FloorToInt(p.z + 0.1f);
		return new IntVec3 { x = x2, y = y2, z = z2 };
	}

	public Vector3 Vector3
	{
		get
		{
			return new Vector3(x, y, z);
		}
	}
}
