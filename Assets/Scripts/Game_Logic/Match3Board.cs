using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class Match3Board
{
    private readonly int _size;
    private readonly Match3Tile[,] _tiles;
    public Match3Tile[,] Tiles => _tiles;
    private TileType[] _availableTileTypes;

    public int Size => _size;
    public Match3Board(int size, TileType[] availableTileTypes)
    {
        _size = size;
        _tiles = new Match3Tile[_size, _size];
        for (int i = 0; i < _size; i++)
        {
            for (int j = 0; j < _size; j++)
            {
                _tiles[i, j] = new Match3Tile(new Vector2Int(i, j));
            }   
        }

        _availableTileTypes = availableTileTypes;
        SetRandomTileTypeForAllTiles();
    }

    private void SetRandomTileTypeForAllTiles()
    {
        foreach (var tile in _tiles)
        {
            tile.Type = GetRandomTileType();
        }
    }

    private TileType GetRandomTileType()
    {
        return _availableTileTypes[Random.Range(0, _availableTileTypes.Length)];
    }

    public void ClickedOnTile(int row, int col)
    {
        var clickedTile = _tiles[row, col];
        
        if (clickedTile.IsValid()) return;
        
        //We need at least 2 connected tiles, so no connected neighbors means
        //no need to check for anything else
        
        var tileListToDestroy = GetTilesToDestroy(row, col);
        
        if(tileListToDestroy.Count < 2) return;
        
        Match3Actions.UsedMove();
        
        DestroyTiles(tileListToDestroy);
        DropExistingTiles();
        FillEmptySlots();
        
        if (!IsThereValidMove())
        {
            Shuffle();
        }
    }

    private bool IsThereValidMove()
    {
        for (int i = 0; i < _size; i++)
        {
            for (int j = 0; j < _size; j++)
            {
                if (HasTileAnyMatchingNeighbor(_tiles[i,j]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal void Shuffle()
    {
        //Fisher–Yates algorithm
            System.Random random = new System.Random();
            int lengthRow = _tiles.GetLength(1);

            for (int i = _tiles.Length - 1; i > 0; i--)
            {
                int i0 = i / lengthRow;
                int i1 = i % lengthRow;

                int j = random.Next(i + 1);
                int j0 = j / lengthRow;
                int j1 = j % lengthRow;

                var temp = _tiles[i0, i1].Type;
                _tiles[i0, i1].Type = _tiles[j0, j1].Type;
                _tiles[j0, j1].Type = temp;
            }
            Match3Actions.BoardShuffle();
    }
    private void DestroyTiles(List<Vector2Int> tileListToDestroy)
    {
        var type = _tiles[tileListToDestroy[0].x, tileListToDestroy[0].y].Type;
        for (int i = 0; i < tileListToDestroy.Count; i++)
        {
            var tilePos = tileListToDestroy[i];
            _tiles[tilePos.x, tilePos.y].Type = TileType.Empty;
        }
        Match3Actions.TilesDestroyed(tileListToDestroy);
        
        Dictionary<TileType, int> destroyedTileDict = new Dictionary<TileType, int>();
        destroyedTileDict.Add(type, tileListToDestroy.Count);
        Match3Actions.TileTypeOfAmountDestroyed(destroyedTileDict);
    }


    private void DropExistingTiles()
    {
        //No need to check bottom row for drop
        for (int i = 1; i < _size; i++)
        {
            for (int j = 0; j < _size; j++)
            {
                int k = 0;
                var tile = _tiles[i, j];
                if(tile.Type != TileType.Empty)
                {
                    int currentRow = i;
                    while (currentRow > 0 && k < _size-1 && IsBottomNeighborEmpty(currentRow, j))
                    {
                        currentRow--;
                        k++;
                    }
                    //Found out that below exists for swapping 2 array elements
                    (_tiles[currentRow, j], _tiles[currentRow + k, j]) = (_tiles[currentRow + k, j], _tiles[currentRow, j]);
                    _tiles[currentRow, j].DroppedBy(k);
                }
            }
        }
        Match3Actions.ExistingTilesDropped();
    }
    private void FillEmptySlots()
    {
        List<Vector2Int> filledTilePositions = new List<Vector2Int>();

        for (int i = 0; i < _size; i++)
        {
            for (int j = 0; j < _size; j++)
            {
                var tile = _tiles[i, j];
                if (tile.Type == TileType.Empty)
                {
                    tile.Type = GetRandomTileType();
                    tile.Position = new Vector2Int(i, j);
                    tile.HasDropped = true;
                    tile.DropAmount = _size;
                    filledTilePositions.Add(tile.Position);
                }  
            }
        }

        Match3Actions.TilesFilledFromAbove(filledTilePositions);

    }
    private bool IsBottomNeighborEmpty(int row, int col)
    {
        if (row == 0) return false;
        return _tiles[row - 1, col].Type == TileType.Empty;
    }


    private bool HasTileAnyMatchingNeighbor(Match3Tile tile)
    {
        if (IsLeftNeighborValidAndTheSameType(tile)) return true;
        if (IsRightNeighborValidAndTheSameType(tile)) return true;
        if (IsBottomNeighborValidAndTheSameType(tile)) return true;
        if (IsTopNeighborValidAndTheSameType(tile)) return true;
        return false;
    }

    private bool IsLeftNeighborValidAndTheSameType(Match3Tile tile)
    {
        return tile.Position.y > 0 && tile == LeftOf(tile);
    }

    private bool IsRightNeighborValidAndTheSameType(Match3Tile tile)
    {
        return tile.Position.y < _size - 1 && tile == RightOf(tile);
    }
    private bool IsTopNeighborValidAndTheSameType(Match3Tile tile)
    {
        return tile.Position.x < _size - 1 && tile == TopOf(tile);
    }

    private bool IsBottomNeighborValidAndTheSameType(Match3Tile tile)
    {
        return tile.Position.x > 0 && tile == BottomOf(tile);
    }

    public List<Vector2Int> GetTilesToDestroy(int row, int col)
    {
        List<Vector2Int> tilePositions = new List<Vector2Int>();
        Queue<Match3Tile> tilesToSearch = new Queue<Match3Tile>();
        
        ResetAllTilesSearchStatus();
        
        tilesToSearch.Enqueue(_tiles[row,col]);
        while (tilesToSearch.Count > 0)
        {
            var currentTile = tilesToSearch.Dequeue();
            if (!tilePositions.Contains(currentTile.Position))
            {
                tilePositions.Add(currentTile.Position);
            }
            currentTile.Searched = true;
            
            if(IsLeftNeighborValidAndTheSameType(currentTile)){
                ProcessNeighbor(currentTile, LeftOf(currentTile), tilesToSearch, tilePositions);
            }
            if(IsRightNeighborValidAndTheSameType(currentTile)){
                ProcessNeighbor(currentTile, RightOf(currentTile), tilesToSearch, tilePositions);
            }

            if (IsTopNeighborValidAndTheSameType(currentTile))
            {
                ProcessNeighbor(currentTile, TopOf(currentTile), tilesToSearch, tilePositions);
            }

            if (IsBottomNeighborValidAndTheSameType(currentTile))
            {
                ProcessNeighbor(currentTile, BottomOf(currentTile), tilesToSearch, tilePositions);
            }

        }

        return tilePositions;
    }

    private void ProcessNeighbor(Match3Tile currentTile, Match3Tile neighbor,
        Queue<Match3Tile> tilesToSearch, List<Vector2Int> tilePositions)
    {
        if(currentTile != neighbor || neighbor.Searched) return;
        tilesToSearch.Enqueue(neighbor);
        tilePositions.Add(neighbor.Position);
        neighbor.Searched = true;
    }

    private void ResetAllTilesSearchStatus()
    {
        foreach (var tile in _tiles)
        {
            tile.Searched = false;
        }
    }

    private Match3Tile TopOf(Match3Tile tile)
    {
        return _tiles[tile.Position.x +1 , tile.Position.y ];
    }
    private Match3Tile BottomOf(Match3Tile tile)
    {
        return _tiles[tile.Position.x - 1 , tile.Position.y ];
    }
    private Match3Tile RightOf(Match3Tile tile)
    {
        return _tiles[tile.Position.x, tile.Position.y + 1];
    }
    private Match3Tile LeftOf(Match3Tile tile)
    {
        return _tiles[tile.Position.x, tile.Position.y - 1];
    }
    public void ResetDropFlag(int row, int col)
    {
        _tiles[row, col].HasDropped = false;
        _tiles[row, col].DropAmount = 0;
    }

    public override string ToString()
    {
        if (_tiles == null || _size == 0) return "";
        
        StringBuilder builder = new StringBuilder();
        
        for (int i = _size - 1; i >= 0 ; i--)
        {
            for (int j = 0; j < _size; j++)
            {
                builder.Append(_tiles[i, j]);
            }

            builder.Append("\n");
        }
        return builder.ToString();
    }

}
