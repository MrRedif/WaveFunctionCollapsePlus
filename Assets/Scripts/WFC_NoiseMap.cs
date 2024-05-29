using SimplexNoise;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

public class WFC_NoiseMap : MonoBehaviour
{
    [SerializeField] WFCGenerator generator;
    [SerializeField][Range(0.01F, 2F)] float scale;
    public float power;
    public int seed = 209323094;
    public enum NoiseType { Simplex, Circle, Lines, Flat }
    [SerializeField] NoiseType noiseType;
    [SerializeField] bool isInverted;
    float[,] noiseMap;
    Tilemap map;
    [SerializeField] Tile placeHolderTile;
    // Start is called before the first frame update
    void Awake()
    {
        map = GetComponent<Tilemap>();
        noiseMap = new float[generator.generationSize.x, generator.generationSize.y];
        for (int i = 0; i < generator.generationSize.x; i++)
        {
            for (int j = 0; j < generator.generationSize.y; j++)
            {
                var pos = new Vector3Int(i, j, 0);
                map.SetTile(pos, placeHolderTile);
                noiseMap[i, j] = 1f;
            }
        }
        UpdateNoiseMap();
    }

    public void UpdateNoiseMap()
    {
        switch (noiseType)
        {
            case NoiseType.Simplex:
                Noise.Seed = seed;
                noiseMap = Noise.Calc2D(generator.generationSize.x, generator.generationSize.y, scale);
                for (int i = 0; i < generator.generationSize.x; i++)
                {
                    for (int j = 0; j < generator.generationSize.y; j++)
                    {
                        noiseMap[i, j] /= 255f;
                    }
                }
                break;
            case NoiseType.Circle:
                var center = new Vector2(generator.generationSize.x / 2f, generator.generationSize.y / 2f);
                for (int i = 0; i < generator.generationSize.x; i++)
                {
                    for (int j = 0; j < generator.generationSize.y; j++)
                    {
                        noiseMap[i, j] = Vector2.Distance(new Vector2(i, j), center);
                        noiseMap[i, j] /= center.magnitude;
                        noiseMap[i, j] = Mathf.Pow(noiseMap[i, j], scale);
                    }
                }


                break;
            case NoiseType.Lines:
                for (int i = 0; i < generator.generationSize.x; i++)
                {
                    for (int j = 0; j < generator.generationSize.y; j++)
                    {
                        float n = 0f, m = 0f;
                        if (j % 20 < 10)
                        {
                            n += (j % 20) / 9f;
                        }
                        else
                        {
                            n += (19 - (j % 20)) / 9f;
                        }


                        if (i % 20 < 10)
                        {
                            m += (i % 20) / 9f;
                        }
                        else
                        {
                            m += (19 - (i % 20)) / 9f;
                        }


                        noiseMap[i, j] = n / 2f + m / 2f + float.Epsilon;
                    }
                }
                break;
            case NoiseType.Flat:
                for (int i = 0; i < generator.generationSize.x; i++)
                {
                    for (int j = 0; j < generator.generationSize.y; j++)
                    {
                        noiseMap[i, j] = 0.5f;
                    }
                }
                break;
            default:
                break;
        }


        //Update Tiles
        for (int i = 0; i < generator.generationSize.x; i++)
        {
            for (int j = 0; j < generator.generationSize.y; j++)
            {
                var pos = new Vector3Int(i, j, 0);
                var col = new Color(isInverted ? 1 - noiseMap[i, j] : noiseMap[i, j], 0, 0, 1f);


                map.SetTileFlags(pos, TileFlags.None);
                map.SetColor(pos, col);
            }
        }
    }

    public float GetNoisePoint(int x, int y, bool flip = false)
    {
        if (noiseMap == null) return -1f;

        return (isInverted ^ flip ? 1f - noiseMap[x, y] + 0.00001f : noiseMap[x, y]) * power;
    }

    // Update is called once per frame
    void Update()
    {

    }
}
