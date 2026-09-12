using System;
using System.Collections.Generic;
using UnityEngine;

namespace HouseAccess.World;

/// <summary>Geometry shared by route following and its progress watchdog.</summary>
internal static class RouteProgress
{
	public static bool Reached(Vector3 position, Vector3 corner, float radius, float height)
	{
		float x = position.x - corner.x, z = position.z - corner.z;
		return x * x + z * z <= radius * radius && Math.Abs(position.y - corner.y) <= height;
	}

	public static float Remaining(Vector3 position, IList<Vector3> corners, int index)
	{
		float distance = 0f;
		for (int i = index; i < corners.Count; i++)
		{
			distance += Vector3.Distance(position, corners[i]);
			position = corners[i];
		}
		return distance;
	}

	public static Vector3 HorizontalStep(Vector3 position, Vector3 waypoint, float maximum)
	{
		Vector3 direction = waypoint - position;
		direction.y = 0f;
		float distance = direction.magnitude;
		return distance > 0f ? direction * (Math.Min(distance, Math.Max(0f, maximum)) / distance) : Vector3.zero;
	}
}
