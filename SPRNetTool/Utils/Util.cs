using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Media3D;

namespace ArtWiz.Utils
{
    public static class Util
    {
        public static bool IsValid(this Thickness thickness, bool allowNegative, bool allowNaN, bool allowPositiveInfinity, bool allowNegativeInfinity)
        {
            if (!allowNegative)
            {
                if (thickness.Left < 0d || thickness.Right < 0d || thickness.Top < 0d || thickness.Bottom < 0d)
                    return false;
            }

            if (!allowNaN)
            {
                if (double.IsNaN(thickness.Left) || double.IsNaN(thickness.Right) || double.IsNaN(thickness.Top) || double.IsNaN(thickness.Bottom))
                    return false;
            }

            if (!allowPositiveInfinity)
            {
                if (Double.IsPositiveInfinity(thickness.Left) || Double.IsPositiveInfinity(thickness.Right) || Double.IsPositiveInfinity(thickness.Top) || Double.IsPositiveInfinity(thickness.Bottom))
                {
                    return false;
                }
            }

            if (!allowNegativeInfinity)
            {
                if (Double.IsNegativeInfinity(thickness.Left) || Double.IsNegativeInfinity(thickness.Right) || Double.IsNegativeInfinity(thickness.Top) || Double.IsNegativeInfinity(thickness.Bottom))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
