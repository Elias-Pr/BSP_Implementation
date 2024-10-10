using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System.Drawing;
using Unity.Mathematics;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;
using Color = UnityEngine.Color;
using Random = System.Random;

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
//long, faire une méthode, réfarctorer cette partie*
        if (cutHorizontal) {
            cutPosition = UnityEngine.Random.Range((int)(room.Size.y * 0.2f), (int)(room.Size.y * 0.8f));
            room1 = new Room(room.Position, new Vector2Int(room.Size.x, cutPosition));
            room2 = new Room(new Vector2Int(room.Position.x, room.Position.y + cutPosition),
                new Vector2Int(room.Size.x, room.Size.y - cutPosition));

            for (int i = 0; i < room.Size.x; i++) {
                Vector2Int cutPos = new Vector2Int(room.Position.x + i, room.Position.y + cutPosition - 1);
                room1.CutPositions.Add(cutPos);
                _globalCutPositions.Add(cutPos);
            }
        }
        else {
            cutPosition = UnityEngine.Random.Range((int)(room.Size.x * 0.2f), (int)(room.Size.x * 0.8f));
            room1 = new Room(room.Position, new Vector2Int(cutPosition, room.Size.y));
            room2 = new Room(new Vector2Int(room.Position.x + cutPosition, room.Position.y),
                new Vector2Int(room.Size.x - cutPosition, room.Size.y));

            for (int i = 0; i < room.Size.y; i++) {
                Vector2Int cutPos = new Vector2Int(room.Position.x + cutPosition - 1, room.Position.y + i);
                room1.CutPositions.Add(cutPos);
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
        foreach (Room room in Rooms) {
            Instantiate(centerPoint, new Vector3(room.CenterPosition.x,-room.CenterPosition.y,0), quaternion.identity);
        }
    }
    
    private void GenerateTiles() {
        foreach (var room in Rooms) {
            for (int row = 0; row < room.Size.y; row++) {
                for (int col = 0; col < room.Size.x; col++) {
                    Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3((room.Position.x 
                        + col) * _tilemap.cellSize.x, -(room.Position.y + row) * _tilemap.cellSize.y, 0));
                    _tilemap.SetTile(tilePosition, emptyTile);
                }
            }
        }

        foreach (var cutPosition in _globalCutPositions) {
            Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3(cutPosition.x * _tilemap.cellSize.x, 
                -cutPosition.y * _tilemap.cellSize.y, 0));
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
