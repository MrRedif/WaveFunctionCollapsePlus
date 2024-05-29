using KaimiraGames;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WFCGenerator : MonoBehaviour
{
    [SerializeField] WFC_NoiseMap noiseMap;
    public List<WFC_Tile> possibleTiles = new List<WFC_Tile>();
    public Vector2Int generationSize;
    public Vector2Int partSize;
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
        for (int i = 0; i < generationSize.x - partSize.x; i += partSize.x - 1)
        {
            for (int j = 0; j < generationSize.y - partSize.y; j += partSize.y - 1)
            {
                yield return StartCoroutine(GeneratePart(grid, i, j));
            }
        }
    }

    private IEnumerator GeneratePart(WFC_GridPoint[,] grid, int xStart, int yStart)
    {
        bool failed = false;
        do
        {
            List<WFC_GridPoint> remainigPoints = new List<WFC_GridPoint>();

            //Reset Area
            for (int i = xStart; i < xStart + partSize.x; i++)
            {
                for (int j = yStart; j < yStart + partSize.y; j++)
                {
                    var p = new WFC_GridPoint();
                    p.gridPosition = new Vector2Int(i, j);
                    p.possibleOptions = new List<WFC_Tile>(possibleTiles);
                    p.UpdateEntropy(noiseMap);
                    remainigPoints.Add(p);

                    try
                    {
                        grid[i, j] = p;
                    }catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }

                }
            }

            WFC_GridPoint.UpdateGrid(grid, xStart, yStart, noiseMap, partSize);
            //Collapse one by one
            while (remainigPoints.Count > 0)
            {
                failed = false;

                //Get least entropy tile
                var leastEntropyPoint = GetLeastEntropyTile(remainigPoints);

                if (leastEntropyPoint == null)
                {
                    failed = true;
                    yield return new WaitForEndOfFrame();
                    break;
                }

                var x = leastEntropyPoint.gridPosition.x;
                var y = leastEntropyPoint.gridPosition.y;

                //Show position and wait
                yield return new WaitForSeconds(stepTime);

                //Collapse tile
                var options = leastEntropyPoint.possibleOptions;
                if (options.Count <= 0)
                {
                    Debug.LogError("No possible options!");
                    failed = true;
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

                //Update Left
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
                leastEntropyPoint.possibleOptions = new List<WFC_Tile>() { collapsedTile };
            }
        } while (failed);



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

        internal static void UpdateGrid(WFC_GridPoint[,] grid, int xStart, int yStart, WFC_NoiseMap noiseMap, Vector2Int partSize)
        {
            Stack<WFC_GridPoint> pointStack = new Stack<WFC_GridPoint>();
            pointStack.Push(grid[xStart, yStart]);

            while (pointStack.Count > 0)
            {
                var point = pointStack.Pop();
                var count = point.possibleOptions.Count;

                //Left N
                if (point.gridPosition.x > 0)
                {
                    var left = grid[point.gridPosition.x - 1, point.gridPosition.y];
                    HashSet<WFC_Tile> h = new HashSet<WFC_Tile>();
                    foreach (var option in left.possibleOptions)
                    {
                        h.AddRange(option.rightTiles);
                    }
                    point.possibleOptions = point.possibleOptions.Where(x => h.Contains(x)).ToList();
                    if (point.possibleOptions.Count != count && !left.isCollapsed && CheckBounds(xStart, yStart, partSize, left))
                    {

                        pointStack.Push(left);
                    }
                }

                //Bottom N
                if (point.gridPosition.y > 0)
                {
                    var bottom = grid[point.gridPosition.x, point.gridPosition.y - 1];
                    HashSet<WFC_Tile> h = new HashSet<WFC_Tile>();
                    foreach (var option in bottom.possibleOptions)
                    {
                        h.AddRange(option.topTiles);
                    }
                    point.possibleOptions = point.possibleOptions.Where(x => h.Contains(x)).ToList();
                    if (point.possibleOptions.Count != count && !bottom.isCollapsed && CheckBounds(xStart, yStart, partSize, bottom))
                    {
                        pointStack.Push(bottom);
                    }
                }

                //Right N
                if (point.gridPosition.x < grid.GetLength(0) - 1)
                {
                    var right = grid[point.gridPosition.x + 1, point.gridPosition.y];
                    HashSet<WFC_Tile> h = new HashSet<WFC_Tile>();
                    foreach (var option in right.possibleOptions)
                    {
                        h.AddRange(option.leftTiles);
                    }
                    point.possibleOptions = point.possibleOptions.Where(x => h.Contains(x)).ToList();
                    if (point.possibleOptions.Count != count && !right.isCollapsed && CheckBounds(xStart, yStart, partSize, right))
                    {
                        pointStack.Push(right);
                    }
                }

                //Top N
                if (point.gridPosition.y < grid.GetLength(1) - 1)
                {
                    var top = grid[point.gridPosition.x, point.gridPosition.y + 1];
                    HashSet<WFC_Tile> h = new HashSet<WFC_Tile>();
                    foreach (var option in top.possibleOptions)
                    {
                        h.AddRange(option.bottomTiles);
                    }
                    point.possibleOptions = point.possibleOptions.Where(x => h.Contains(x)).ToList();
                    if (point.possibleOptions.Count != count && !top.isCollapsed && CheckBounds(xStart, yStart, partSize, top))
                    {
                        pointStack.Push(top);
                    }
                }

                if (count != point.possibleOptions.Count)
                {
                    point.UpdateEntropy(noiseMap);
                }
            }
        }

        private static bool CheckBounds(int xStart, int yStart, Vector2Int partSize, WFC_GridPoint point)
        {
            var inBounds = point.gridPosition.x >= xStart && point.gridPosition.y >= yStart && point.gridPosition.x < xStart + partSize.x &&
                point.gridPosition.y < yStart + partSize.y;
            return inBounds;
        }

        public void UpdateEntropy(WFC_NoiseMap noiseMap)
        {
            double sumOfWeights = 0f;
            double sumOfWeightLogWeights = 0f;
            foreach (var option in possibleOptions)
            {
                double weight = option.weight;
                weight *= noiseMap.GetNoisePoint(gridPosition.x, gridPosition.y, option.Type == WFC_Tile.TileType.Road);

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
