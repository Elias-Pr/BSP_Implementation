using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BspImplementation : MonoBehaviour
{
    public Tile emptyTile;
    public Tile filledTile;
    
    private Tile[,] _tileMatrix;
    public int rows = 5;
    public int cols = 5;
    
    private void Awake()
    {
        _tileMatrix = new Tile[rows, cols];
        for (int r = 0; r < rows; r++)
        {
            for (int i = 0; i < cols; i++)
            {
                _tileMatrix[r, i] = emptyTile;
            }
        }
    }
    
    void Start()
    {
        
    }

    void Update()
    {
        
    }
}
