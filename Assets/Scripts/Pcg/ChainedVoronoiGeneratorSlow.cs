using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace Pcg
{
    public class ChainedVoronoiGeneratorSlow : ITextureGenerator<Color>
    {
        private readonly List<int> m_units;
        private readonly int m_seed;

        public ChainedVoronoiGeneratorSlow(int seed, List<int> units)
        {
            m_seed = seed;
            m_units = units;
        }

        public void GenerateTexture(RectInt rect, Color[,] target)
        {
            for (var x = rect.x; x < rect.xMax; ++x)
            {
                for (var y = rect.y; y < rect.yMax; ++y)
                {
                    target[x, y] = GetTextureColorAt(0, x, y);
                }
            }
        }

        private static int DistanceSqr(Vector2Int a, Vector2Int b)
        {
            return (a - b).sqrMagnitude;
        }

        //把地图h渲染到材质上
        private Texture2D RenderVoronoiGraph(int width, int height)
        {
            var tex = new Texture2D(width, height)
            {
                filterMode = FilterMode.Point
            };
            for (var x = 0; x < width; ++x)
            {
                for (var y = 0; y < height; ++y)
                {
                    tex.SetPixel(x, y, GetTextureColorAt(x, y));
                }
            }

            tex.Apply();
            return tex;
        }

        //获取该点所在区域的颜色
        private Color GetTextureColorAt(int x, int y)
        {
            return GetTextureColorAt(0, x, y);
        }

        private Color GetTextureColorAt(int level, int x, int y)
        {
            if (level == m_units.Count - 1)
            {
                return GetClosestRootColorFrom(level, x, y);
            }

            var next = GetClosestRootPositionFrom(level, x, y);
            return GetTextureColorAt(level + 1, next.x, next.y);
        }

        //获取某一个Cell的根是什么颜色的
        private Color GetColorOfCellRoot(int level, int cellX, int cellY)
        {
            var rand = new Random(cellX ^ cellY * 0x123456 + level + (m_seed << 2) + 66666);
            return new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
        }

        private Vector2Int GetCellRootPosition(int level, int cell_x, int cell_y)
        {
            var rand = new Random((((level ^ 0xe12345) * cell_x + cell_y) ^ 0xabcdef) + level * level + m_seed + 23333);
            return new Vector2Int(
                (cell_x << m_units[level]) + rand.Next(1 << m_units[level]),
                (cell_y << m_units[level]) + rand.Next(1 << m_units[level]));
        }

        private Vector2Int GetClosestRootPositionFrom(int level, int x, int y)
        {
            int cx = x >> m_units[level], cy = y >> m_units[level];
            var minDist = int.MaxValue;
            var ret = Vector2Int.zero;
            for (var i = -1; i <= 1; ++i)
            {
                for (var j = -1; j <= 1; ++j)
                {
                    int ax = cx + i, ay = cy + j;
                    var pos = GetCellRootPosition(level, ax, ay);
                    var dist = DistanceSqr(pos, new Vector2Int(x, y));
                    if (dist < minDist)
                    {
                        minDist = dist;
                        ret = pos;
                    }
                }
            }

            return ret;
        }

        private Color GetClosestRootColorFrom(int level, int x, int y)
        {
            int cx = x >> m_units[level], cy = y >> m_units[level];
            var min_dist = int.MaxValue;
            var ret = Color.red;
            for (var i = -1; i <= 1; ++i)
            {
                for (var j = -1; j <= 1; ++j)
                {
                    int ax = cx + i, ay = cy + j;
                    var pos = GetCellRootPosition(level, ax, ay);
                    var color = GetColorOfCellRoot(level, ax, ay);
                    var dist = DistanceSqr(pos, new Vector2Int(x, y));
                    if (dist < min_dist)
                    {
                        min_dist = dist;
                        ret = color;
                    }
                }
            }

            return ret;
        }
    }
}