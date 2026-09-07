using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// Orthogonal (up/down/left/right) board-cell helpers for support abilities.
/// </summary>
public static class BoardCellNeighborhood
{
    private static readonly List<TowerBoardCell> Scratch = new List<TowerBoardCell>(32);

    public static void FindAllCells(List<TowerBoardCell> results)
    {
        results.Clear();
        TowerBoardCell[] cells = Object.FindObjectsByType<TowerBoardCell>(FindObjectsSortMode.None);
        for (int i = 0; i < cells.Length; i++)
        {
            if (cells[i] != null)
                results.Add(cells[i]);
        }
    }

    public static bool TryGetGridCoords(TowerBoardCell cell, out int row, out int column)
    {
        row = 0;
        column = 0;
        if (cell == null || !TryParseCellIndex(cell.gameObject.name, out int index))
            return false;

        // Cell names are typically 1-based (Cell_1 … Cell_25).
        int zeroBased = index >= 1 ? index - 1 : index;
        int gridSize = BoardGridLayout.GridSize;
        row = zeroBased / gridSize;
        column = zeroBased % gridSize;
        return row >= 0 && row < gridSize && column >= 0 && column < gridSize;
    }

    public static void GetOrthogonalNeighbors(TowerBoardCell origin, List<TowerBoardCell> results)
    {
        results.Clear();
        if (origin == null || !TryGetGridCoords(origin, out int row, out int column))
            return;

        Transform boardRoot = origin.transform.parent;
        FindAllCells(Scratch);
        for (int i = 0; i < Scratch.Count; i++)
        {
            TowerBoardCell candidate = Scratch[i];
            if (candidate == null || candidate == origin)
                continue;
            if (boardRoot != null && candidate.transform.parent != boardRoot)
                continue;
            if (!TryGetGridCoords(candidate, out int r, out int c))
                continue;

            int manhattan = Mathf.Abs(r - row) + Mathf.Abs(c - column);
            if (manhattan == 1)
                results.Add(candidate);
        }
    }

    private static bool TryParseCellIndex(string cellName, out int index)
    {
        index = 0;
        if (string.IsNullOrWhiteSpace(cellName))
            return false;

        int underscore = cellName.LastIndexOf('_');
        if (underscore < 0 || underscore >= cellName.Length - 1)
            return false;

        return int.TryParse(
            cellName.Substring(underscore + 1),
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out index);
    }
}
