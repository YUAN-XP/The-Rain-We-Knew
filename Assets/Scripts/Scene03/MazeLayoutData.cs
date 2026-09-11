using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game Data/Maze Layout")]
public class MazeLayoutData : ScriptableObject
{
    [Tooltip("Top to bottom; 16 rows of 20 edges. 1 = wall, 0 = opening.")]
    public string[] horizontalRows;
    [Tooltip("Top to bottom; 15 rows of 21 edges. 1 = wall, 0 = opening.")]
    public string[] verticalRows;
    public const int Rows = 15, Columns = 20;

    public bool IsValid()
    {
        return ValidRows(horizontalRows, Rows + 1, Columns) && ValidRows(verticalRows, Rows, Columns + 1);
    }
    static bool ValidRows(string[] rows, int count, int width)
    {
        if (rows == null || rows.Length != count) return false;
        foreach (string row in rows)
        {
            if (row == null || row.Length != width) return false;
            foreach (char c in row) if (c != '0' && c != '1') return false;
        }
        return true;
    }
    public bool CanStep(Vector2Int from, Vector2Int to)
    {
        if (to.x < 0 || to.x >= Rows || to.y < 0 || to.y >= Columns) return false;
        if (to.x == from.x - 1 && to.y == from.y) return horizontalRows[from.x][from.y] == '0';
        if (to.x == from.x + 1 && to.y == from.y) return horizontalRows[from.x + 1][from.y] == '0';
        if (to.y == from.y - 1 && to.x == from.x) return verticalRows[from.x][from.y] == '0';
        if (to.y == from.y + 1 && to.x == from.x) return verticalRows[from.x][from.y + 1] == '0';
        return false;
    }
    public IEnumerable<Rect> WallRects(float cellSize, float thickness)
    {
        for (int row = 0; row <= Rows; row++)
            for (int col = 0; col < Columns; col++)
                if (horizontalRows[row][col] == '1')
                    yield return new Rect(col * cellSize - thickness / 2, (Rows - row) * cellSize - thickness / 2, cellSize + thickness, thickness);
        for (int row = 0; row < Rows; row++)
            for (int col = 0; col <= Columns; col++)
                if (verticalRows[row][col] == '1')
                    yield return new Rect(col * cellSize - thickness / 2, (Rows - row - 1) * cellSize - thickness / 2, thickness, cellSize + thickness);
    }
}
