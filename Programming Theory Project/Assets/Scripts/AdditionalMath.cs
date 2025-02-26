using System.Collections;
using UnityEngine;

namespace Assets.Scripts
{
    public static class AdditionalMath
    {
        public static int Sgn(int x)
        {
            if (x < 0) return -1;
            if (x > 0) return 1;
            return 0;
        }
    }
}