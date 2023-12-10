using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = System.Random;

namespace SquareUp.Runtime.Generation
{
    [SelectionBase]
    [DisallowMultipleComponent]
    public class LevelGenerator : MonoBehaviour
    {
        public Tile wallTile;
        public bool generate;
        public bool clear;
        public string seed;

        [Header("BOUNDARY")]
        public int boundaryChunkSize = 16;
        public int boundaryChunkCount = 12;

        [Header("PILLARS")]
        public int pillarSizeMin = 2;
        public int pillarSizeMax = 3;
        [Range(0.0f, 1.0f)]
        public float pillarsPerChunk = 1.0f;
        [Range(0.0f, 1.0f)]
        public float pillarRandomness = 0.5f;

        private Random rng;
        private Tilemap tilemap;
        private HashSet<Vector2Int> walls = new();

        private void Awake() { Generate(); }

        public void Generate()
        {
            tilemap = GetComponentInChildren<Tilemap>();
            tilemap.ClearAllTiles();

            walls.Clear();

            rng = new Random(seed.GetHashCode());

            var chunks = PickChunks();
            FillWallChunks(chunks);
            PlacePillars(chunks);

            var tiles = new Tile[walls.Count];
            for (var i = 0; i < tiles.Length; i++)
            {
                tiles[i] = wallTile;
            }

            var positions = new Vector3Int[walls.Count];
            var h = 0;
            foreach (var e in walls)
            {
                positions[h++] = (Vector3Int)e;
            }

            if (Application.isPlaying)
            {
                tilemap.SetTiles(positions, tiles);
            }
            else
            {
                tilemap.SetTiles(positions, tiles);
            }
        }

        private void PlacePillars(List<(Vector2Int, ChunkType)> chunks)
        {
            foreach (var c in chunks)
            {
                if (c.Item2 != ChunkType.Empty) continue;

                var count = pillarsPerChunk;

                while (true)
                {
                    if (count < (float)rng.NextDouble()) break;
                    count--;
                    
                    var size = rng.Next(pillarSizeMin, pillarSizeMax + 1);
                    var px = rng.Next(0, boundaryChunkSize - size);
                    var py = rng.Next(0, boundaryChunkSize - size);

                    px = Mathf.RoundToInt(Mathf.Lerp((boundaryChunkSize - size) * 0.5f, px, pillarRandomness));
                    py = Mathf.RoundToInt(Mathf.Lerp((boundaryChunkSize - size) * 0.5f, py, pillarRandomness));

                    for (var j = 0; j < size * size; j++)
                    {
                        var x = (c.Item1.x * boundaryChunkSize) + px + (j % size);
                        var y = (c.Item1.y * boundaryChunkSize) + py + (j / size);

                        var tilePos = new Vector2Int(x, y);
                        if (!walls.Contains(tilePos)) walls.Add(tilePos);
                    }
                }
            }
        }

        private void FillWallChunks(List<(Vector2Int, ChunkType)> chunks)
        {
            foreach (var c in chunks)
            {
                if (c.Item2 != ChunkType.Wall) continue;

                for (var i = 0; i < boundaryChunkSize * boundaryChunkSize; i++)
                {
                    var x = (c.Item1.x * boundaryChunkSize) + (i % boundaryChunkSize);
                    var y = (c.Item1.y * boundaryChunkSize) + (i / boundaryChunkSize);

                    var tilePos = new Vector2Int(x, y);
                    if (!walls.Contains(tilePos)) walls.Add(tilePos);
                }
            }
        }

        private List<(Vector2Int, ChunkType)> PickChunks()
        {
            var carve = RandomFill(boundaryChunkCount);

            var neighbours = new Vector2Int[]
            {
                new(0, 1),
                new(0, -1),
                new(1, 0),
                new(-1, 0),

                new(1, 1),
                new(-1, 1),
                new(1, -1),
                new(-1, -1),
            };

            var chunkPositions = new List<(Vector2Int, ChunkType)>();
            foreach (var seed in carve)
            {
                foreach (var offset in neighbours)
                {
                    var wallPos = seed + offset;
                    if (chunkPositions.Exists(e => e.Item1 == wallPos)) continue;

                    ChunkType type;
                    if (carve.Contains(wallPos))
                    {
                        type = ChunkType.Empty;
                    }
                    else
                    {
                        type = ChunkType.Wall;
                    }

                    chunkPositions.Add((wallPos, type));
                }
            }

            return chunkPositions;
        }

        public List<Vector2Int> RandomFill(int count)
        {
            var res = new List<Vector2Int>() { Vector2Int.zero };
            var neighbours = new Vector2Int[]
            {
                new(0, 1),
                new(0, -1),
                new(1, 0),
                new(-1, 0),
            };

            for (var i = 1; i < count; i++)
            {
                var seed = res[rng.Next(res.Count)];
                var offset = neighbours[rng.Next(neighbours.Length)];

                var chunk = seed + offset;
                if (!res.Contains(chunk)) res.Add(chunk);
                else i--;
            }

            return res;
        }

        public void OnValidate()
        {
            if (generate)
            {
                generate = false;
                Generate();
            }

            if (clear)
            {
                clear = false;
                tilemap.ClearAllTiles();
            }
        }

        public enum ChunkType
        {
            Wall,
            Empty,
        }
    }
}