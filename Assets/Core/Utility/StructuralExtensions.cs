using System.Collections;

namespace RTSLockstep.Utility
{
    public static class StructuralExtensions
    {
        public static bool StructuralEquals<T>(this T a, T b)
            where T : IStructuralEquatable
        {
            return a.Equals(b, StructuralComparisons.StructuralEqualityComparer);
        }

        public static int StructuralCompare<T>(this T a, T b)
            where T : IStructuralComparable
        {
            return a.CompareTo(b, StructuralComparisons.StructuralComparer);
        }
    }
}
