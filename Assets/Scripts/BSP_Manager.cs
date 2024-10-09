using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;

public class BspImplementation : MonoBehaviour
{
    // VISUAL VARIABLES
    public Tile emptyTile;
    public Tile filledTile;

    private Tilemap _tilemap;
    private Camera _mainCamera;

    // BSP VARIABLES
    public List<Room> rooms;
    public int seed;
    public int cutNumber;
    private int _currentCut;
    
    private List<Vector2Int> _globalCutPositions;
    
    // Grid dimensions
    public int gridWidth;
    public int gridHeight;

    private void Awake()
    {
        System.Random generationSeed = new System.Random(seed);

        _tilemap = GetComponent<Tilemap>();
        _mainCamera = FindObjectOfType<Camera>();

        rooms = new List<Room>();
        _globalCutPositions = new List<Vector2Int>();  // Initialize global cut position list

        Room initialRoom = new Room(new Vector2Int(0, 0), new Vector2Int(gridWidth, gridHeight));
        rooms.Add(initialRoom);

        BinarySpatialPartionning(initialRoom, cutNumber);

        GenerateTiles();
    }


    public void BinarySpatialPartionning(Room room, int cuts)
    {
        if (_currentCut >= cuts) return;

        bool cutHorizontal = (room.size.y > room.size.x) || (UnityEngine.Random.value > 0.5f);
        int cutPosition;

        Room room1, room2;

        if (cutHorizontal)
        {
            cutPosition = UnityEngine.Random.Range(1, room.size.y);
            room1 = new Room(room.position, new Vector2Int(room.size.x, cutPosition));
            room2 = new Room(new Vector2Int(room.position.x, room.position.y + cutPosition), new Vector2Int(room.size.x, room.size.y - cutPosition));

            for (int i = 0; i < room.size.x; i++)
            {
                Vector2Int cutPos = new Vector2Int(room.position.x + i, room.position.y + cutPosition - 1);
                room1.cutPositions.Add(cutPos);
                _globalCutPositions.Add(cutPos);
            }
        }
        else
        {
            cutPosition = UnityEngine.Random.Range(1, room.size.x);
            room1 = new Room(room.position, new Vector2Int(cutPosition, room.size.y));
            room2 = new Room(new Vector2Int(room.position.x + cutPosition, room.position.y), new Vector2Int(room.size.x - cutPosition, room.size.y));

            for (int i = 0; i < room.size.y; i++)
            {
                Vector2Int cutPos = new Vector2Int(room.position.x + cutPosition - 1, room.position.y + i);
                room1.cutPositions.Add(cutPos);
                _globalCutPositions.Add(cutPos);
            }
        }

        rooms.Add(room1);
        rooms.Add(room2);

        _currentCut++;

        BinarySpatialPartionning(room1, cuts);
        BinarySpatialPartionning(room2, cuts);
    }




    private void GenerateTiles()
    {
        Vector3 cameraPosition = _mainCamera.transform.position;

        float totalGridWidth = gridWidth * _tilemap.cellSize.x;
        float totalGridHeight = gridHeight * _tilemap.cellSize.y;

        Vector3 gridOffset = new Vector3(cameraPosition.x - totalGridWidth / 2, cameraPosition.y + totalGridHeight / 2, 0);

        foreach (var room in rooms)
        {
            for (int r = 0; r < room.size.y; r++)
            {
                for (int c = 0; c < room.size.x; c++)
                {
                    Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3((room.position.x + c) * _tilemap.cellSize.x, -(room.position.y + r) * _tilemap.cellSize.y, 0) + gridOffset);

                    _tilemap.SetTile(tilePosition, emptyTile);
                }
            }
        }

        foreach (var cutPosition in _globalCutPositions)
        {
            Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3(cutPosition.x * _tilemap.cellSize.x, -cutPosition.y * _tilemap.cellSize.y, 0) + gridOffset);
            _tilemap.SetTile(tilePosition, filledTile);
        }
    }



}

public class Room
{
    public Vector2Int position;
    public Vector2Int size;
    public List<Vector2Int> cutPositions;  // Store cut positions

    public Room(Vector2Int position, Vector2Int size)
    {
        this.position = position;
        this.size = size;
        this.cutPositions = new List<Vector2Int>(); // Initialize the list
    }
}
