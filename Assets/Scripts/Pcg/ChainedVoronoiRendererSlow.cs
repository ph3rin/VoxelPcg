using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;
using Random = System.Random;

namespace Pcg
{
    public class ChainedVoronoiRendererSlow : MonoBehaviour
    {
        //注意这里的Unit代表单位长度u是2的多少次方。u是2的整数次幂算起来会比较方便
        [SerializeField] private int m_seed;
        [SerializeField] private int m_width;
        [SerializeField] private int m_height;
        [SerializeField] private List<int> m_units;
        
        private void Start()
        {
            var targetTexture = new Color[m_width, m_height];
            var generator = new ChainedVoronoiGeneratorSlow(m_seed, m_units);
            var stopwatch = new Stopwatch();
            
            stopwatch.Start();
            generator.GenerateTexture(new RectInt(0, 0, m_width, m_height), targetTexture);
            stopwatch.Stop();
            
            Debug.Log($"Time elapsed: {stopwatch.ElapsedMilliseconds}ms");
            
            GetComponent<RawImage>().texture = ConvertArrayToTexture(targetTexture);
        }
        
        private static Texture2D ConvertArrayToTexture(Color[,] colors)
        {
            var width = colors.GetLength(0);
            var height = colors.GetLength(1);
            var tex = new Texture2D(width, height)
            {
                filterMode = FilterMode.Point
            };
            for (var x = 0; x < width; ++x)
            {
                for (var y = 0; y < height; ++y)
                {
                    tex.SetPixel(x, y, colors[x, y]);
                }
            }
            tex.Apply();
            return tex;
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
        // private Color GetColorOfCellRoot(int level, int cellX, int cellY)
        // {
        //     var rand = new Random(cellX ^ cellY * 0x123456 + level + (m_seed << 2) + 66666);
        //     return new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble());
        // }
        
        private Color GetColorOfCellRoot(int level, int cellX, int cellY)
        {
            var hash = HashCode.Combine(level, cellX, cellY);
            var r = hash & 0xFF;
            var g = (hash >> 8) & 0xFF;
            var b = (hash >> 16) & 0xFF;
            return new Color(r / 255f, g / 255f, b / 255f);
        }

        
        // private Vector2Int GetCellRootPosition(int level, int cellX, int cellY)
        // {
        //     var rand = new Random((((level ^ 0xe12345) * cellX + cellY) ^ 0xabcdef) + level * level + m_seed + 23333);
        //     return new Vector2Int(
        //         (cellX << m_units[level]) + rand.Next(1 << m_units[level]),
        //         (cellY << m_units[level]) + rand.Next(1 << m_units[level]));
        // }
        //
        private Vector2Int GetCellRootPosition(int level, int cellX, int cellY)
        {
            var hash = HashCode.Combine(level ^ 0xDEADBEAF, cellX ^ 0x12345678, cellY ^ 0x87654321);
            var xOffset = hash & ((1 << m_units[level]) - 1);
            var yOffset = (hash >> 16) & ((1 << m_units[level]) - 1);
            return new Vector2Int(
                (cellX << m_units[level]) + xOffset,
                (cellY << m_units[level]) + yOffset);
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