using System;
using System.Collections.Generic;
using System.Linq;
using iPAHeartBeat.Core.Dependency;
using UnityEngine;
using UnityEngine.UI;

public class MeshCreatorAR : MonoBehaviour
{

    [Header("Room Creation")]
    public RoomCreation roomCreation;
    [SerializeField] private Material wallMaterial;
    [SerializeField] private Material ceilingMaterial;
    [SerializeField] private Material floorMaterial;
    [SerializeField] private Slider slider;
    private GameObject roomMeshObject;
    private GameObject floorMeshObject;
    private GameObject ceilingMeshObject;
    private float minHeight = 0.1f;
    private float maxHeight = 3.0f;
    private float currentHeight = 1.5f;
    internal List<Vector3> floorPoints = new List<Vector3>();
    void Awake()
    {
        DependencyResolver.Register<MeshCreatorAR>(this);
    }

    internal void CreateMesh()
    {
        if (floorPoints.Count < 3) return;

        // Convert to local space
        for (int i = 0; i < floorPoints.Count; i++)
            floorPoints[i] = transform.InverseTransformPoint(floorPoints[i]);

        if (IsSelfIntersecting(floorPoints))
        {
            Debug.LogError("Self-intersecting polygon detected!");
            return;
        }

        Mesh wallMesh = new Mesh();
        Mesh floorMesh = new Mesh();
        Mesh ceilingMesh = new Mesh();

        List<Vector3> wallVertices = new List<Vector3>();
        List<int> wallTriangles = new List<int>();

        List<Vector3> floorVertices = new List<Vector3>();
        List<int> floorTriangles = new List<int>();

        List<Vector3> ceilingVertices = new List<Vector3>();
        List<int> ceilingTriangles = new List<int>();

        // Generate floor & ceiling
        Vector3[] floorArray = floorPoints.ToArray();
        int[] floorIndices = TriangulateFloor(floorArray);
        int[] ceilingIndices = floorIndices.Reverse().ToArray(); // Flip ceiling

        floorVertices.AddRange(floorArray);
        ceilingVertices.AddRange(floorArray.Select(p => p + Vector3.up * currentHeight));

        floorTriangles.AddRange(floorIndices);
        ceilingTriangles.AddRange(ceilingIndices);

        // Generate Walls
        for (int i = 0; i < floorPoints.Count; i++)
        {
            Vector3 basePoint = floorPoints[i];
            Vector3 topPoint = basePoint + Vector3.up * currentHeight;

            wallVertices.Add(basePoint);
            wallVertices.Add(topPoint);
        }

        // Create Wall Triangles
        for (int i = 0; i < floorPoints.Count - 1; i++)
        {
            int bl = i * 2;
            int tl = i * 2 + 1;
            int br = (i + 1) * 2;
            int tr = (i + 1) * 2 + 1;

            wallTriangles.Add(bl); wallTriangles.Add(br); wallTriangles.Add(tl);
            wallTriangles.Add(tl); wallTriangles.Add(br); wallTriangles.Add(tr);
        }

        // Close the last wall
        int lastBl = (floorPoints.Count - 1) * 2;
        int lastTl = (floorPoints.Count - 1) * 2 + 1;
        int firstBl = 0;
        int firstTl = 1;

        wallTriangles.Add(lastBl); wallTriangles.Add(firstBl); wallTriangles.Add(lastTl);
        wallTriangles.Add(lastTl); wallTriangles.Add(firstBl); wallTriangles.Add(firstTl);

        // Assign Mesh Data
        wallMesh.vertices = wallVertices.ToArray();
        wallMesh.triangles = wallTriangles.ToArray();
        wallMesh.RecalculateNormals();

        floorMesh.vertices = floorVertices.ToArray();
        floorMesh.triangles = floorTriangles.ToArray();
        floorMesh.RecalculateNormals();

        ceilingMesh.vertices = ceilingVertices.ToArray();
        ceilingMesh.triangles = ceilingTriangles.ToArray();
        ceilingMesh.RecalculateNormals();

        ApplyMeshToGameObject(ref roomMeshObject, "Walls", wallMesh, wallMaterial);
        ApplyMeshToGameObject(ref floorMeshObject, "Floor", floorMesh, floorMaterial);
        ApplyMeshToGameObject(ref ceilingMeshObject, "Ceiling", ceilingMesh, ceilingMaterial);

        AdjustHeight(currentHeight);
        slider.onValueChanged.AddListener(AdjustHeight);
    }





    int[] TriangulateFloor(Vector3[] points)
    {
        List<int> indices = new List<int>();
        Vector2[] points2D = points.Select(p => new Vector2(p.x, p.z)).ToArray();

        // Use Ear Clipping Triangulation for concave shapes
        List<int> triangulatedIndices = EarClipping.Triangulate(points2D);

        // Ensure correct winding order
        if (!IsCounterClockwise(points2D))
            triangulatedIndices.Reverse();

        return triangulatedIndices.ToArray();
    }
    bool IsCounterClockwise(Vector2[] points)
    {
        float sum = 0;
        for (int i = 0; i < points.Length; i++)
        {
            Vector2 p1 = points[i];
            Vector2 p2 = points[(i + 1) % points.Length];
            sum += (p2.x - p1.x) * (p2.y + p1.y);
        }
        return sum > 0; // Positive sum means counterclockwise
    }
    bool IsSelfIntersecting(List<Vector3> polygon)
    {
        if (polygon.Count < 4) return false; // Need at least 4 points for intersection

        for (int i = 0; i < polygon.Count - 1; i++)
        {
            for (int j = i + 2; j < polygon.Count - 1; j++)
            {
                if (j == i || j == i + 1) continue; // Ignore consecutive lines

                if (LinesIntersect(polygon[i], polygon[i + 1], polygon[j], polygon[(j + 1) % polygon.Count]))
                {
                    Debug.LogError($"Self-intersection detected between {i} and {j}.");
                    return true;
                }
            }
        }
        return false;
    }

    bool LinesIntersect(Vector3 a1, Vector3 a2, Vector3 b1, Vector3 b2)
    {
        if (a1 == b1 || a1 == b2 || a2 == b1 || a2 == b2)
            return false; // Shared endpoints are not considered intersections.

        float denominator = (a2.x - a1.x) * (b2.z - b1.z) - (a2.z - a1.z) * (b2.x - b1.x);
        if (Mathf.Abs(denominator) < Mathf.Epsilon)
            return false; // Parallel or collinear lines.

        float ua = ((b2.x - b1.x) * (a1.z - b1.z) - (b2.z - b1.z) * (a1.x - b1.x)) / denominator;
        float ub = ((a2.x - a1.x) * (a1.z - b1.z) - (a2.z - a1.z) * (a1.x - b1.x)) / denominator;

        return (ua > 0f && ua < 1f && ub > 0f && ub < 1f); // Ensure intersection happens **within** segments.
    }
    void ApplyMeshToGameObject(ref GameObject obj, string name, Mesh mesh, Material mat)
    {
        if (obj != null) Destroy(obj);
        obj = new GameObject(name);
        obj.AddComponent<MeshFilter>().mesh = mesh;
        obj.AddComponent<MeshRenderer>().material = mat;
    }

    void AdjustHeight(float newHeight)
    {
        if (roomMeshObject == null || ceilingMeshObject == null) return;

        newHeight = Mathf.Clamp(newHeight, minHeight, maxHeight);

        // Adjust Wall Mesh
        Mesh wallMesh = roomMeshObject.GetComponent<MeshFilter>().mesh;
        Vector3[] wallVertices = wallMesh.vertices;
        for (int i = 1; i < wallVertices.Length; i += 2)
            wallVertices[i].y = newHeight;

        wallMesh.vertices = wallVertices;
        wallMesh.RecalculateNormals();

        // Adjust Ceiling Mesh
        Mesh ceilingMesh = ceilingMeshObject.GetComponent<MeshFilter>().mesh;
        Vector3[] ceilingVertices = ceilingMesh.vertices;
        for (int i = 0; i < ceilingVertices.Length; i++)
            ceilingVertices[i].y = newHeight;

        ceilingMesh.vertices = ceilingVertices;
        ceilingMesh.RecalculateNormals();
    }

}


public class Triangulator
{
    private List<Vector2> m_points = new List<Vector2>();

    public Triangulator(Vector2[] points)
    {
        m_points = new List<Vector2>(points);
    }

    public int[] Triangulate()
    {
        List<int> indices = new List<int>();

        int n = m_points.Count;
        if (n < 3)
            return indices.ToArray();

        int[] V = new int[n];
        if (Area() > 0)
        {
            for (int i = 0; i < n; i++) V[i] = i;
        }
        else
        {
            for (int i = 0; i < n; i++) V[i] = (n - 1) - i;
        }

        int nv = n;
        int count = 2 * nv;
        for (int m = 0, v = nv - 1; nv > 2;)
        {
            if ((count--) <= 0)
                return indices.ToArray();

            int u = v; if (nv <= u) u = 0;
            v = u + 1; if (nv <= v) v = 0;
            int w = v + 1; if (nv <= w) w = 0;

            if (Snip(u, v, w, nv, V))
            {
                int a, b, c;
                a = V[u]; b = V[v]; c = V[w];
                indices.Add(a);
                indices.Add(b);
                indices.Add(c);

                m++;
                for (int s = v, t = v + 1; t < nv; s++, t++)
                {
                    V[s] = V[t];
                }
                nv--;

                count = 2 * nv;
            }
        }
        indices.Reverse();
        return indices.ToArray();
    }

    private float Area()
    {
        int n = m_points.Count;
        float A = 0.0f;
        for (int p = n - 1, q = 0; q < n; p = q++)
        {
            Vector2 pval = m_points[p];
            Vector2 qval = m_points[q];
            A += pval.x * qval.y - qval.x * pval.y;
        }
        return (A * 0.5f);
    }

    private bool Snip(int u, int v, int w, int n, int[] V)
    {
        int p;
        Vector2 A = m_points[V[u]];
        Vector2 B = m_points[V[v]];
        Vector2 C = m_points[V[w]];

        if (Mathf.Epsilon > (((B.x - A.x) * (C.y - A.y)) - ((B.y - A.y) * (C.x - A.x))))
            return false;

        for (p = 0; p < n; p++)
        {
            if ((p == u) || (p == v) || (p == w)) continue;
            Vector2 P = m_points[V[p]];
            if (InsideTriangle(A, B, C, P))
                return false;
        }
        return true;
    }

    private bool InsideTriangle(Vector2 A, Vector2 B, Vector2 C, Vector2 P)
    {
        float ax, ay, bx, by, cx, cy, px, py;
        float apx, apy, bpx, bpy, cpx, cpy;
        float cCROSSap, bCROSScp, aCROSSbp;

        ax = C.x - B.x; ay = C.y - B.y;
        bx = A.x - C.x; by = A.y - C.y;
        cx = B.x - A.x; cy = B.y - A.y;
        px = P.x; py = P.y;

        apx = px - A.x; apy = py - A.y;
        bpx = px - B.x; bpy = py - B.y;
        cpx = px - C.x; cpy = py - C.y;

        aCROSSbp = ax * bpy - ay * bpx;
        cCROSSap = cx * apy - cy * apx;
        bCROSScp = bx * cpy - by * cpx;

        return ((aCROSSbp >= 0.0f) && (bCROSScp >= 0.0f) && (cCROSSap >= 0.0f));
    }
}

public static class EarClipping
{
    public static List<int> Triangulate(Vector2[] points)
    {
        List<int> indices = new List<int>();
        List<Vector2> polygon = new List<Vector2>(points);
        List<int> remainingIndices = Enumerable.Range(0, points.Length).ToList();

        while (remainingIndices.Count > 3)
        {
            bool earFound = false;
            for (int i = 0; i < remainingIndices.Count; i++)
            {
                int prev = remainingIndices[(i - 1 + remainingIndices.Count) % remainingIndices.Count];
                int curr = remainingIndices[i];
                int next = remainingIndices[(i + 1) % remainingIndices.Count];

                if (IsEar(polygon, prev, curr, next))
                {
                    indices.Add(prev);
                    indices.Add(curr);
                    indices.Add(next);
                    remainingIndices.RemoveAt(i);
                    earFound = true;
                    break;
                }
            }
            if (!earFound) break; // Safety Break in case of infinite loop
        }
        // Add the last remaining triangle
        indices.Add(remainingIndices[0]);
        indices.Add(remainingIndices[1]);
        indices.Add(remainingIndices[2]);
        return indices;
    }

    private static bool IsEar(List<Vector2> polygon, int prev, int curr, int next)
    {
        Vector2 a = polygon[prev], b = polygon[curr], c = polygon[next];
        if (CrossProduct(a, b, c) >= 0) return false; // Clockwise triangle, reject

        for (int i = 0; i < polygon.Count; i++)
        {
            if (i == prev || i == curr || i == next) continue;
            if (PointInTriangle(polygon[i], a, b, c)) return false;
        }
        return true;
    }

    private static float CrossProduct(Vector2 a, Vector2 b, Vector2 c)
    {
        return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float alpha = ((b.y - c.y) * (p.x - c.x) + (c.x - b.x) * (p.y - c.y)) /
                      ((b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y));
        float beta = ((c.y - a.y) * (p.x - c.x) + (a.x - c.x) * (p.y - c.y)) /
                     ((b.y - c.y) * (a.x - c.x) + (c.x - b.x) * (a.y - c.y));
        float gamma = 1.0f - alpha - beta;
        return alpha > 0 && beta > 0 && gamma > 0;
    }
}
