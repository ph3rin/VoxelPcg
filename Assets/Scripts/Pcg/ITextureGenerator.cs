using UnityEngine;

namespace Pcg
{
    public interface ITextureGenerator<in T>
    {
        void GenerateTexture(RectInt rect, T[,] target);
    }
}