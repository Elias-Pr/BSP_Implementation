using System.Collections.Generic;
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
    
    //DELAUNAY VARIABLES
    public GameObject superTrianglePSummit;
    public GameObject centerPoint;
    
    private void Awake() {
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



    public void BinarySpatialPartionning(Room room, int depth) {
        if (_currentCut >= depth) return;

        bool cutHorizontal = (room.Size.y > room.Size.x) || (UnityEngine.Random.value > 0.5f);
        int cutPosition;

        Room room1, room2;

        if (cutHorizontal) {
            cutPosition = UnityEngine.Random.Range((int)(room.Size.y * 0.2f), (int)(room.Size.y * 0.8f));
            room1 = new Room(room.Position, new Vector2Int(room.Size.x, cutPosition));
            room2 = new Room(new Vector2Int(room.Position.x, room.Position.y + cutPosition + 1),
                new Vector2Int(room.Size.x, room.Size.y - cutPosition - 1));

            for (int i = 0; i < room.Size.x; i++) {
                Vector2Int cutPos = new Vector2Int(room.Position.x + i, room.Position.y + cutPosition);
                _globalCutPositions.Add(cutPos);
            }
        }
        else {
            cutPosition = UnityEngine.Random.Range((int)(room.Size.x * 0.2f), (int)(room.Size.x * 0.8f));
            room1 = new Room(room.Position, new Vector2Int(cutPosition, room.Size.y));
            room2 = new Room(new Vector2Int(room.Position.x + cutPosition + 1, room.Position.y),
                new Vector2Int(room.Size.x - cutPosition - 1, room.Size.y));

            for (int i = 0; i < room.Size.y; i++) {
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


    private void DelaunayTriangulation() {
        
        List<Vector2> superTriangleVertex = new List<Vector2>();
        List<Vector2> centerPoints = new List<Vector2>();
        
        LineIntersection lineIntersection;
        
        lineIntersection = new LineIntersection(new Vector2(gridWidth/2f, gridHeight/2f),0,-gridHeight/2f, -45);
        
        // Convert angle from degrees to radians and calculate the slope
        float slope = Mathf.Tan(lineIntersection.AngleDegrees * Mathf.Deg2Rad);

        // Calculate the y-intercept (b) using the start point (y = mx + b -> b = y - mx)
        float intercept = lineIntersection.StartPoint.y - slope * lineIntersection.StartPoint.x;

        // Find the y value when x reaches targetX
        float yAtTargetX = slope * lineIntersection.TargetX + intercept;

        // Find the x value when y reaches targetY
        float xAtTargetY = (lineIntersection.TargetY - intercept) / slope;
        
        Vector2 vertex1 = new Vector2(0, yAtTargetX);
        superTriangleVertex.Add(vertex1);

        Vector2 vertex2 = new Vector2(xAtTargetY, -gridHeight/2f);
        superTriangleVertex.Add(vertex2);

        Vector2 vertex3 = new Vector2(-xAtTargetY, -gridHeight/2f);
        superTriangleVertex.Add(vertex3);

        foreach (Vector2 point in superTriangleVertex)
        {
            Instantiate(superTrianglePSummit, point, Quaternion.identity);
        }
        
        foreach (Room room in Rooms) {
            Instantiate(centerPoint, new Vector3(room.CenterPosition.x, room.CenterPosition.y, 0), quaternion.identity);
            centerPoints.Add(room.CenterPosition);
        }
        
    }

    
    private void GenerateTiles() {
        foreach (var room in Rooms) {
            for (int row = 0; row < room.Size.y; row++) {
                for (int col = 0; col < room.Size.x; col++) {
                    Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3((room.Position.x 
                        + col) * _tilemap.cellSize.x, (room.Position.y + row) * _tilemap.cellSize.y, 0));
                    _tilemap.SetTile(tilePosition, emptyTile);
                }
            }
        }

        foreach (var cutPosition in _globalCutPositions) {
            Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3(cutPosition.x * _tilemap.cellSize.x, 
                cutPosition.y * _tilemap.cellSize.y, 0));
            _tilemap.SetTile(tilePosition, filledTile);
        }
    }
}

public class Room {
    public Vector2Int Position;
    public Vector2Int Size;
    public Vector2 CenterPosition;
    public List<Vector2Int> CutPositions;

    public Room(Vector2Int position, Vector2Int size) {
        this.Position = position;
        this.Size = size;
        this.CenterPosition = new Vector2(Position.x + Size.x / 2f, Position.y + Size.y / 2f);
        this.CutPositions = new List<Vector2Int>();
    }
}

public class LineIntersection
{
    public Vector2 StartPoint;
    public float TargetX;       
    public float TargetY;       
    public float AngleDegrees;

    public LineIntersection(Vector2 startPoint, float targetX, float targetY, float angleDegrees)
    {
        StartPoint = startPoint;
        TargetX = targetX;
        TargetY = targetY;
        AngleDegrees = angleDegrees;
    }
}