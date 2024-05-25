using KaimiraGames;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WFCGenerator : MonoBehaviour
{
    [SerializeField] WFC_NoiseMap noiseMap;
    public List<WFC_Tile> possibleTiles = new List<WFC_Tile>();
    public Vector2Int generationSize;
    [Range(0.001f, 0.2f)] public float stepTime;

    Tilemap tilemap;
    public Tile markTile;

    public List<Tile> placeholders = new List<Tile>();
    private void Awake()
    {
        tilemap = GetComponent<Tilemap>();
    }

    public WFC_GridPoint[,] CreateGrid()
    {
        tilemap.ClearAllTiles();
        var grid = new WFC_GridPoint[generationSize.x, generationSize.y];
        for (int i = 0; i < generationSize.x; i++)
        {
            for (int j = 0; j < generationSize.y; j++)
            {
                var point = new WFC_GridPoint();
                point.gridPosition = new Vector2Int(i, j);
                point.possibleOptions = new List<WFC_Tile>(possibleTiles);
                point.UpdateEntropy(noiseMap);
                grid[i, j] = point;
                tilemap.SetTile(new Vector3Int(i, j, 0), placeholders[point.possibleOptions.Count]);
            }
        }
        return grid;
    }

    public IEnumerator Generate()
    {
        var grid = CreateGrid();
        List<WFC_GridPoint> remainigPoints = new List<WFC_GridPoint>();
        for (int i = 0; i < generationSize.x; i++)
        {
            for (int j = 0; j < generationSize.y; j++)
            {
                remainigPoints.Add(grid[i, j]);
            }
        }
        while (remainigPoints.Count > 0)
        {

            //Get least entropy tile
            var leastEntropyPoint = GetLeastEntropyTile(remainigPoints);


            var x = leastEntropyPoint.gridPosition.x;
            var y = leastEntropyPoint.gridPosition.y;

            //Show position and wait
            //tilemap.SetTile(new Vector3Int(x, y, 0), markTile);
            yield return new WaitForSeconds(stepTime);

            //Collapse tile
            var options = leastEntropyPoint.possibleOptions;
            if (options.Count <= 0)
            {
                Debug.LogError("No possible options!");
                break;
            }
            var weightedList = new WeightedList<WFC_Tile>();
            foreach (var tile in options)
            {
                weightedList.Add(tile, tile.weight * (int)noiseMap.GetNoisePoint(x, y, tile.Type == WFC_Tile.TileType.Road));
            }

            WFC_Tile collapsedTile = weightedList.Next();
            tilemap.SetTile(new Vector3Int(x, y, 0), collapsedTile.tile);

            //Update Top
            if (y < generationSize.y - 1 && !grid[x, y + 1].isCollapsed)
            {
                var topTile = grid[x, y + 1];
                var intersection = topTile.possibleOptions.Where(x => collapsedTile.topTiles.Contains(x)).ToList();
                topTile.possibleOptions = intersection;
                tilemap.SetTile(new Vector3Int(x, y + 1, 0), placeholders[topTile.possibleOptions.Count]);
                topTile.UpdateEntropy(noiseMap);
            }

            //Update Right
            if (x < generationSize.x - 1 && !grid[x + 1, y].isCollapsed)
            {
                var rightTile = grid[x + 1, y];
                var intersection = rightTile.possibleOptions.Where(x => collapsedTile.rightTiles.Contains(x)).ToList();
                rightTile.possibleOptions = intersection;
                tilemap.SetTile(new Vector3Int(x + 1, y, 0), placeholders[rightTile.possibleOptions.Count]);
                rightTile.UpdateEntropy(noiseMap);
            }

            //Update Bottom
            if (y > 0 && !grid[x, y - 1].isCollapsed)
            {
                var bottomTile = grid[x, y - 1];
                var intersection = bottomTile.possibleOptions.Where(x => collapsedTile.bottomTiles.Contains(x)).ToList();
                bottomTile.possibleOptions = intersection;
                tilemap.SetTile(new Vector3Int(x, y - 1, 0), placeholders[bottomTile.possibleOptions.Count]);
                bottomTile.UpdateEntropy(noiseMap);
            }

            //Updat Left
            if (x > 0 && !grid[x - 1, y].isCollapsed)
            {
                var leftTile = grid[x - 1, y];
                var intersection = leftTile.possibleOptions.Where(x => collapsedTile.leftTiles.Contains(x)).ToList();
                leftTile.possibleOptions = intersection;
                tilemap.SetTile(new Vector3Int(x - 1, y, 0), placeholders[leftTile.possibleOptions.Count]);
                leftTile.UpdateEntropy(noiseMap);
            }

            remainigPoints.Remove(leastEntropyPoint);
            leastEntropyPoint.isCollapsed = true;
        }


    }

    private WFC_GridPoint GetLeastEntropyTile(List<WFC_GridPoint> remainigPoints)
    {
        double currentLowestEntropy = double.MaxValue;
        var foundTiles = new List<WFC_GridPoint>();
        foreach (WFC_GridPoint tile in remainigPoints)
        {
            if (tile.entropy < currentLowestEntropy)
            {
                foundTiles.Clear();
                currentLowestEntropy = tile.entropy;
                foundTiles.Add(tile);
            }
            else if (tile.entropy == currentLowestEntropy)
            {
                foundTiles.Add(tile);
            }
        }

        return foundTiles.Count > 0 ? foundTiles[Random.Range(0, foundTiles.Count)] : null;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StopAllCoroutines();
            StartCoroutine(Generate());
        }
    }





    public class WFC_GridPoint
    {
        public WFC_Tile WFCTile;
        public Vector2Int gridPosition;
        public List<WFC_Tile> possibleOptions;
        public bool isCollapsed;
        public double entropy;

        public void UpdateEntropy(WFC_NoiseMap noiseMap)
        {
            double sumOfWeights = 0f;
            double sumOfWeightLogWeights = 0f;
            foreach (var option in possibleOptions)
            {
                double weight = option.weight;
                weight *= noiseMap.GetNoisePoint(gridPosition.x,gridPosition.y,option.Type == WFC_Tile.TileType.Road);

                sumOfWeights += weight;
                sumOfWeightLogWeights += weight * System.Math.Log(weight);
            }
            entropy = System.Math.Log(sumOfWeights) - sumOfWeightLogWeights / sumOfWeights;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + new Vector3(generationSize.x, generationSize.y) / 2f, new Vector3(generationSize.x, generationSize.y));
    }
}
