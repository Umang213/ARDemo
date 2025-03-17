using System.Collections.Generic;
using UnityEngine;

public static class QuickHull
{
	public static List<int> GenerateConvexHull(List<Vector3> points)
	{
		var hull = new HashSet<int>();
		var triangles = new List<int>();

		for (int i = 0; i < points.Count; i++)
		{
			for (int j = 0; j < points.Count; j++)
			{
				for (int k = 0; k < points.Count; k++)
				{
					if (i == j || j == k || k == i) continue;

					Vector3 normal = Vector3.Cross(points[j] - points[i], points[k] - points[i]);
					if (normal == Vector3.zero) continue;

					bool valid = true;

					foreach (var p in points)
					{
						if (p == points[i] || p == points[j] || p == points[k]) continue;

						float side = Vector3.Dot(normal, p - points[i]);
						if (side > 0)
						{
							valid = false;
							break;
						}
					}

					if (valid)
					{
						hull.Add(i);
						hull.Add(j);
						hull.Add(k);

						triangles.Add(i);
						triangles.Add(j);
						triangles.Add(k);
					}
				}
			}
		}

		return triangles;
	}
}
