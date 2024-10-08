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
    

    // MATRIX VARIABLES
    private static Tile[,] _tileMatrix;

    public int rows;
    public int cols;

    // BSP VARIABLES
    public int seed;
    public int cutNumber;

    private int _currentCut;

    // Variables to store the current bounds of the largest room
    private int currentMinRow;
    private int currentMaxRow;
    private int currentMinCol;
    private int currentMaxCol;

    private void Awake()
    {
        System.Random randomSeed = new System.Random(seed);

        _tilemap = GetComponent<Tilemap>();
        _mainCamera = FindObjectOfType<Camera>();

        _tileMatrix = new Tile[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                _tileMatrix[r, c] = emptyTile;
            }
        }

        currentMinRow = 0;
        currentMaxRow = rows - 1;
        currentMinCol = 0;
        currentMaxCol = cols - 1;

        BspLaunch(cutNumber);
        GenerateTiles();
    }

    public void BspLaunch(int cuts)
    {
        while (_currentCut < cuts)
        {
            Vector2Int largestRoom = FindLargestRoom(currentMinRow, currentMaxRow, currentMinCol, currentMaxCol);

            if (largestRoom == Vector2Int.one * -1)
                break;

            int row = largestRoom.x;
            int col = largestRoom.y;

            bool cutHorizontal = (rows > cols) || (UnityEngine.Random.value > 0.5f);
            int cutPosition;

            if (cutHorizontal)
            {
                cutPosition = UnityEngine.Random.Range(currentMinRow, currentMaxRow + 1);
                for (int c = currentMinCol; c <= currentMaxCol; c++)
                {
                    _tileMatrix[cutPosition, c] = filledTile;
                }

                currentMaxRow = cutPosition - 1;
            }
            else
            {
                cutPosition = UnityEngine.Random.Range(currentMinCol, currentMaxCol + 1);
                for (int r = currentMinRow; r <= currentMaxRow; r++)
                {
                    _tileMatrix[r, cutPosition] = filledTile;
                }

                currentMaxCol = cutPosition - 1;
            }

            _currentCut++;
        }
    }

    private Vector2Int FindLargestRoom(int minRow, int maxRow, int minCol, int maxCol)
    {
        Vector2Int largestRoom = new Vector2Int(-1, -1);
        int largestSize = 0;

        for (int r = minRow; r <= maxRow; r++)
        {
            for (int c = minCol; c <= maxCol; c++)
            {
                if (_tileMatrix[r, c] == emptyTile)
                {
                    int roomSize = GetRoomSize(r, c);
                    if (roomSize > largestSize)
                    {
                        largestSize = roomSize;
                        largestRoom.Set(r, c);
                    }
                }
            }
        }

        return largestRoom;
    }

    private int GetRoomSize(int startRow, int startCol)
    {
        bool[,] visited = new bool[rows, cols];
        int roomSize = 0;

        Stack<Vector2Int> stack = new Stack<Vector2Int>();
        stack.Push(new Vector2Int(startRow, startCol));

        while (stack.Count > 0)
        {
            Vector2Int current = stack.Pop();
            int row = current.x;
            int col = current.y;

            if (row < 0 || row >= rows || col < 0 || col >= cols || visited[row, col] || _tileMatrix[row, col] != emptyTile)
                continue;

            visited[row, col] = true;
            roomSize++;

            // Push adjacent cells
            stack.Push(new Vector2Int(row + 1, col)); // Down
            stack.Push(new Vector2Int(row - 1, col)); // Up
            stack.Push(new Vector2Int(row, col + 1)); // Right
            stack.Push(new Vector2Int(row, col - 1)); // Left
        }

        return roomSize;
    }

    private void GenerateTiles()
    {
        Vector3 cameraPosition = _mainCamera.transform.position;

        float gridWidth = cols * _tilemap.cellSize.x;
        float gridHeight = rows * _tilemap.cellSize.y;

        Vector3 gridOffset = new Vector3(cameraPosition.x - gridWidth / 2, cameraPosition.y + gridHeight / 2, 0);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                Tile currentTile = _tileMatrix[r, c];

                Vector3Int tilePosition = _tilemap.WorldToCell(new Vector3(c * _tilemap.cellSize.x, -r * _tilemap.cellSize.y, 0) + gridOffset);

                _tilemap.SetTile(tilePosition, currentTile);
            }
        }
    }
}

public class Salle
{
    private int _width;
    private int _height;

    private int[] _center;

}
