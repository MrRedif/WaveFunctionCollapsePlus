using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(WFC_Tile))]
public class WFC_TileEditor : Editor
{
    float previewTileSize = 32;
    float areaSize = 128;

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        var wfcTile = target as WFC_Tile;
        Texture2D spritePreview = AssetPreview.GetAssetPreview(wfcTile.tile.sprite);
        var last = GUILayoutUtility.GetLastRect();

        if(wfcTile.tile != null)
        {

            //Draw Left
            var leftRect = new Rect(last.xMin, last.yMax, areaSize, areaSize);
            DrawNeighbors(wfcTile, leftRect, wfcTile.leftTiles);

            //Preview
            var previewRect = new Rect(leftRect.xMax, leftRect.yMax, areaSize, areaSize);
            GUI.DrawTexture(previewRect, spritePreview, ScaleMode.ScaleToFit);

            //Draw Right
            var rightRect = new Rect(leftRect.xMax * 4, leftRect.yMin, areaSize, areaSize);
            DrawNeighbors(wfcTile, rightRect, wfcTile.rightTiles);

            //Draw Top
            var topRect = new Rect(previewRect.xMax * 1.5f, previewRect.yMin - areaSize, 0, 0);
            DrawNeighbors(wfcTile, topRect, wfcTile.topTiles);

            //Draw Bottom
            var bottomRect = new Rect(previewRect.xMax * 1.5f, previewRect.yMin + areaSize + previewTileSize, 0, 0);
            DrawNeighbors(wfcTile, bottomRect, wfcTile.bottomTiles);

        }


        serializedObject.Update();
        serializedObject.ApplyModifiedProperties();

    }

    private void DrawNeighbors(WFC_Tile wfcTile, Rect startRect, List<WFC_Tile> neighbors)
    {
        if (neighbors == null) return;

        var step = Mathf.FloorToInt(areaSize / previewTileSize);

        for (int i = 0; i < neighbors.Count; i++)
        {
            if(neighbors[i] == null) continue;

            Texture2D preview = AssetPreview.GetAssetPreview(neighbors[i].tile.sprite);
            var rect = new Rect(new Vector2((startRect.xMax / 2f - 64) + (i % step) * previewTileSize, startRect.yMax + (i / step) * previewTileSize), new Vector2(previewTileSize, previewTileSize));
            GUI.DrawTexture(rect, preview);
        }
    }
}
