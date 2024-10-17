using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Tilemaps;

public class BspImplementation : MonoBehaviour
{
    // VISUAL VARIABLES
    public Tile emptyTile;
    public Tile filledTile;
    private Tilemap _tilemap;
    private Camera _mainCamera;
    public int gridWidth;
    public int gridHeight;

    // BSP VARIABLES
    public List<Room> Rooms;
    public int seed;
    public int cutDepth;
    private int _currentCut;
    private List<Vector2Int> _globalCutPositions;
    private System.Random _generationSeed;

    // DELAUNAY VARIABLES
    public GameObject superTrianglePSummit;
    public GameObject centerPoint;

    private void Awake()
    {
        UnityEngine.Random.InitState(seed);

        _tilemap = GetComponent<Tilemap>();
        _mainCamera = FindObjectOfType<Camera>();

        Rooms = new List<Room>();
        _globalCutPositions = new List<Vector2Int>();

        Vector3 cameraPosition = _mainCamera.transform.position;
        Vector2Int initialRoomPosition = new Vector2Int(
            Mathf.RoundToInt(cameraPosition.x - (gridWidth / 2f)),
            Mathf.RoundToInt(cameraPosition.y - (gridHeight / 2f))
        );

        Room initialRoom = new Room(initialRoomPosition, new Vector2Int(gridWidth, gridHeight));
        Rooms.Add(initialRoom);

        BinarySpatialPartionning(initialRoom, cutDepth);
        DelaunayTriangulation();
        GenerateTiles();
    }

    public void BinarySpatialPartionning(Room room, int depth)
    {
        if (_currentCut >= depth) return;

        bool cutHorizontal = (room.Size.y > room.Size.x) || (UnityEngine.Random.value > 0.5f);
        int cutPosition;

        Room room1, room2;

        if (cutHorizontal)
        {
            cutPosition = UnityEngine.Random.Range((int)(room.Size.y * 0.2f), (int)(room.Size.y * 0.8f));
            room1 = new Room(room.Position, new Vector2Int(room.Size.x, cutPosition));
            room2 = new Room(new Vector2Int(room.Position.x, room.Position.y + cutPosition + 1),
                new Vector2Int(room.Size.x, room.Size.y - cutPosition - 1));

            for (int i = 0; i < room.Size.x; i++)
            {
                Vector2Int cutPos = new Vector2Int(room.Position.x + i, room.Position.y + cutPosition);
                _globalCutPositions.Add(cutPos);
            }
        }
        else
        {
            cutPosition = UnityEngine.Random.Range((int)(room.Size.x * 0.2f), (int)(room.Size.x * 0.8f));
            room1 = new Room(room.Position, new Vector2Int(cutPosition, room.Size.y));
            room2 = new Room(new Vector2Int(room.Position.x + cutPosition + 1, room.Position.y),
                new Vector2Int(room.Size.x - cutPosition - 1, room.Size.y));

            for (int i = 0; i < room.Size.y; i++)
            {
                Vector2Int cutPos = new Vector2Int(room.Position.x + cutPosition, room.Position.y + i);
                _globalCutPositions.Add(cutPos);
            }
        }

        Rooms.Remove(room);
        Rooms.Add(room1);
        Rooms.Add(room2);

        Room randomRoom = Rooms[UnityEngine.Random.Range(0, Rooms.Count)];
        _currentCut++;

        BinarySpatialPartionning(randomRoom, depth);
    }

    private void DelaunayTriangulation()
    {
        List<Vector2> superTriangleVertex = new List<Vector2>();
        List<Vector2> centerPoints = new List<Vector2>();

        superTriangleVertex.Add(new Vector2(-gridWidth * 2, -gridHeight * 2));
        superTriangleVertex.Add(new Vector2(gridWidth * 3, -gridHeight * 2));
        superTriangleVertex.Add(new Vector2(gridWidth / 2, gridHeight * 3));

        foreach (Room room in Rooms)
        {
            Instantiate(centerPoint, new Vector3(room.CenterPosition.x, room.CenterPosition.y, 0), Quaternion.identity);
            centerPoints.Add(room.CenterPosition);
        }

        List<Triangle> triangles = BowyerWatson(superTriangleVertex, centerPoints);

        RemoveUnwantedEdges(triangles, superTriangleVertex);

        List<Edge> edges = new List<Edge>();
        foreach (var triangle in triangles)
        {
            VisualizeTriangle(triangle);
            edges.AddRange(triangle.Edges);
        }

        edges = edges.Distinct().ToList();

        List<Edge> mstEdges = KruskalAlgorithm(edges);

        foreach (var edge in mstEdges)
        {
            VisualizeEdge(edge);
        }
    }

    private List<Triangle> BowyerWatson(List<Vector2> superTriangleVertices, List<Vector2> points)
    {
        List<Triangle> triangles = new List<Triangle> {
            new Triangle(superTriangleVertices[0], superTriangleVertices[1], superTriangleVertices[2])
        };

        foreach (Vector2 point in points)
        {
            List<Triangle> badTriangles = new List<Triangle>();

            foreach (var triangle in triangles)
            {
                if (IsPointInsideCircumcircle(point, triangle))
                {
                    badTriangles.Add(triangle);
                }
            }

            List<Edge> polygon = new List<Edge>();

            foreach (var badTriangle in badTriangles)
            {
                triangles.Remove(badTriangle);

                foreach (var edge in badTriangle.Edges)
                {
                    bool shared = badTriangles.Any(otherTriangle => otherTriangle != badTriangle && otherTriangle.Edges.Contains(edge));

                    if (!shared)
                    {
                        polygon.Add(edge);
                    }
                }
            }

            foreach (var edge in polygon)
            {
                triangles.Add(new Triangle(edge.Start, edge.End, point));
            }
        }

        return triangles;
    }

    private List<Edge> KruskalAlgorithm(List<Edge> edges)
    {
        List<Edge> kruskalEdges = new List<Edge>();
        DisjointSet disjointSet = new DisjointSet();

        foreach (var edge in edges)
        {
            disjointSet.MakeSet(edge.Start);
            disjointSet.MakeSet(edge.End);
        }

        edges.Sort((a, b) => Vector2.Distance(a.Start, a.End).CompareTo(Vector2.Distance(b.Start, b.End)));

        foreach (var edge in edges)
        {
            if (disjointSet.FindSet(edge.Start) != disjointSet.FindSet(edge.End))
            {
                kruskalEdges.Add(edge);
                disjointSet.Union(edge.Start, edge.End);
            }
        }

        return kruskalEdges;
    }
    
    private void RemoveUnwantedEdges(List<Triangle> triangles, List<Vector2> superTriangleVertices)
    {
        triangles.RemoveAll(triangle => superTriangleVertices.Any(v => triangle.Vertices.Contains(v)));
    }

    private bool IsPointInsideCircumcircle(Vector2 point, Triangle triangle)
    {
        Vector2 a = triangle.Vertices[0];
        Vector2 b = triangle.Vertices[1];
        Vector2 c = triangle.Vertices[2];

        float det = (a.x * (b.y - c.y)) - (b.x * (a.y - c.y)) + (c.x * (a.y - b.y));

        if (det == 0)
        {
            return false;
        }

        float asq = a.x * a.x + a.y * a.y;
        float bsq = b.x * b.x + b.y * b.y;
        float csq = c.x * c.x + c.y * c.y;

        float circumDet = ((asq * (b.y - c.y)) + (bsq * (c.y - a.y)) + (csq * (a.y - b.y)));

        float circumX = circumDet / (2 * det);
        float circumY = ((asq * (c.x - b.x)) + (bsq * (a.x - c.x)) + (csq * (b.x - a.x))) / (2 * det);

        float circumRadiusSq = (circumX - a.x) * (circumX - a.x) + (circumY - a.y) * (circumY - a.y);

        float distanceSq = (circumX - point.x) * (circumX - point.x) + (circumY - point.y) * (circumY - point.y);

        return distanceSq <= circumRadiusSq;
    }
    
    private void VisualizeTriangle(Triangle triangle)
    {
        for (int i = 0; i < 3; i++)
        {
            Vector2 start = triangle.Vertices[i];
            Vector2 end = triangle.Vertices[(i + 1) % 3];
            Debug.DrawLine(start, end, Color.red, 100f);
        }
    }

    private void VisualizeEdge(Edge edge)
    {
        Debug.DrawLine(edge.Start, edge.End, Color.green, 100f);
    }
    
    private void GenerateTiles()
    {
        foreach (var room in Rooms)
        {
            for (int row = 0; row < room.Size.y; row++)
            {
                for (int col = 0; col < room.Size.x; col++)
                {
                    Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3((room.Position.x
                        + col) * _tilemap.cellSize.x, (room.Position.y + row) * _tilemap.cellSize.y, 0));
                    _tilemap.SetTile(tilePosition, emptyTile);
                }
            }
        }

        foreach (var cutPosition in _globalCutPositions)
        {
            Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3(cutPosition.x * _tilemap.cellSize.x,
                cutPosition.y * _tilemap.cellSize.y, 0));
            _tilemap.SetTile(tilePosition, filledTile);
        }
    }
    
    
}

public class Room
{
    public Vector2Int Position;
    public Vector2Int Size;
    public Vector2 CenterPosition;
    public List<Vector2Int> CutPositions;

    public Room(Vector2Int position, Vector2Int size)
    {
        this.Position = position;
        this.Size = size;
        this.CenterPosition = new Vector2(Position.x + Size.x / 2f, Position.y + Size.y / 2f);
        this.CutPositions = new List<Vector2Int>();
    }
}

public class Triangle
{
    public List<Vector2> Vertices;
    public List<Edge> Edges;

    public Triangle(Vector2 a, Vector2 b, Vector2 c)
    {
        Vertices = new List<Vector2> { a, b, c };
        Edges = new List<Edge> {
            new Edge(a, b),
            new Edge(b, c),
            new Edge(c, a)
        };
    }
}

public class Edge
{
    public Vector2 Start;
    public Vector2 End;

    public Edge(Vector2 start, Vector2 end)
    {
        Start = start;
        End = end;
    }

    public override bool Equals(object obj)
    {
        if (obj is Edge otherEdge)
        {
            return (Start == otherEdge.Start && End == otherEdge.End) ||
                   (Start == otherEdge.End && End == otherEdge.Start);
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Start.GetHashCode() ^ End.GetHashCode();
    }
}

public class DisjointSet
{
    private Dictionary<Vector2, Vector2> parent = new Dictionary<Vector2, Vector2>();
    private Dictionary<Vector2, int> rank = new Dictionary<Vector2, int>();

    public void MakeSet(Vector2 x)
    {
        parent[x] = x;
        rank[x] = 0;
    }

    public Vector2 FindSet(Vector2 x)
    {
        if (parent[x] != x)
        {
            parent[x] = FindSet(parent[x]);
        }
        return parent[x];
    }

    public void Union(Vector2 x, Vector2 y)
    {
        Vector2 rootX = FindSet(x);
        Vector2 rootY = FindSet(y);

        if (rootX != rootY)
        {
            if (rank[rootX] > rank[rootY])
            {
                parent[rootY] = rootX;
            }
            else if (rank[rootX] < rank[rootY])
            {
                parent[rootX] = rootY;
            }
            else
            {
                parent[rootY] = rootX;
                rank[rootX]++;
            }
        }
    }
}
