using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu()]
public class WFC_Tile : ScriptableObject
{
    public enum TileType { Road,Space}
    public TileType Type = TileType.Road;
    public Tile tile;
    public int weight = 1;
    public List<WFC_Tile> topTiles = new List<WFC_Tile>();
    public List<WFC_Tile> bottomTiles = new List<WFC_Tile>();
    public List<WFC_Tile> leftTiles = new List<WFC_Tile>();
    public List<WFC_Tile> rightTiles = new List<WFC_Tile>();
}
