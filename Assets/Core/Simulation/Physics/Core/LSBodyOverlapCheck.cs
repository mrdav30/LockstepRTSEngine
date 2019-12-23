using FastCollections;

namespace RTSLockstep
{
    public partial class LSBody
    {
        private static Vector2d cacheAxis;
        private static Vector2d cacheAxisNormal;
        private static Vector2d cacheP1;
        private static Vector2d cacheP2;
        private static long axisMin;
        private static long axisMax;
        private static long cacheProjPerp;
        private static Vector2d perpVector;
        private static bool calculateIntersections;

        public static long CacheProj { get; private set; }
        public static FastList<int> PotentialEdges { get; } = new FastList<int>();

        public static void PrepareAxisCheck(Vector2d p1, Vector2d p2, bool calculateIntersectionPoints = true)
        {
            cacheP1 = p1;
            cacheP2 = p2;
            cacheAxis = p2 - p1;
            cacheAxis.Normalize();
            cacheAxisNormal = cacheAxis.rotatedLeft;

            axisMin = p1.Dot(cacheAxis.x, cacheAxis.y);
            axisMax = p2.Dot(cacheAxis.x, cacheAxis.y);
            CacheProj = cacheP1.Dot(cacheAxis.x, cacheAxis.y);
            cacheProjPerp = cacheP1.Dot(cacheAxisNormal.x, cacheAxisNormal.y);
            perpVector = cacheAxisNormal * cacheProjPerp;

            calculateIntersections = calculateIntersectionPoints;
        }

        public bool Overlaps(FastList<Vector2d> outputIntersectionPoints)
        {
            outputIntersectionPoints.FastClear();
            //Checks if this object overlaps the line formed by p1 and p2
            switch (Shape)
            {
                case ColliderType.Circle:
                    {
                        bool overlaps = false;
                        //Check if the circle completely fits between the line
                        long projPos = _position.Dot(cacheAxis.x, cacheAxis.y);
                        //Circle withing bounds?
                        if (projPos >= axisMin && projPos <= axisMax)
                        {
                            long projPerp = _position.Dot(cacheAxisNormal.x, cacheAxisNormal.y);
                            long perpDif = (cacheProjPerp - projPerp);
                            long perpDist = perpDif.Abs();
                            if (perpDist <= _radius)
                            {
                                overlaps = true;
                            }
                            if (overlaps)
                            {
                                long sin = (perpDif);
                                long cos = FixedMath.Sqrt(_radius.Mul(_radius) - sin.Mul(sin));
                                if (cos == 0)
                                {
                                    outputIntersectionPoints.Add((cacheAxis * projPos) + perpVector);
                                }
                                else
                                {
                                    outputIntersectionPoints.Add(cacheAxis * (projPos - cos) + perpVector);
                                    outputIntersectionPoints.Add(cacheAxis * (projPos + cos) + perpVector);
                                }
                            }
                        }
                        else
                        {
                            //If not, check distances to points
                            long p1Dist = _position.FastDistance(cacheP1.x, cacheP2.y);
                            if (p1Dist <= FastRadius)
                            {
                                outputIntersectionPoints.Add(cacheP1);
                                overlaps = true;
                            }
                            long p2Dist = _position.FastDistance(cacheP2.x, cacheP2.y);
                            if (p2Dist <= FastRadius)
                            {
                                outputIntersectionPoints.Add(cacheP2);
                                overlaps = true;
                            }

                        }
                        return overlaps;
                    }
                case ColliderType.AABox:
                    {

                    }
                    break;
                case ColliderType.Polygon:
                    {
                        bool intersected = false;

                        for (int i = 0; i < Vertices.Length; i++)
                        {
                            int edgeIndex = i;
                            Vector2d pivot = RealPoints[edgeIndex];
                            Vector2d edge = Edges[edgeIndex];
                            long proj1 = 0;
                            int nextIndex = edgeIndex + 1 < RealPoints.Length ? edgeIndex + 1 : 0;
                            Vector2d nextPoint = RealPoints[nextIndex];
                            long proj2 = (nextPoint - pivot).Dot(edge);

                            long min;
                            long max;
                            if (proj1 < proj2)
                            {
                                min = proj1;
                                max = proj2;
                            }
                            else
                            {
                                min = proj2;
                                max = proj1;
                            }

                            long lineProj1 = (cacheP1 - pivot).Dot(edge);
                            long lineProj2 = (cacheP2 - pivot).Dot(edge);

                            long lineMin;
                            long lineMax;
                            if (lineProj1 < lineProj2)
                            {
                                lineMin = lineProj1;
                                lineMax = lineProj2;
                            }
                            else
                            {
                                lineMin = lineProj2;
                                lineMax = lineProj1;
                            }

                            if (CollisionPair.CheckOverlap(min, max, lineMin, lineMax))
                            {
                                Vector2d edgeNorm = EdgeNorms[edgeIndex];
                                long normProj = 0;
                                long normLineProj1 = (cacheP1 - pivot).Dot(edgeNorm);
                                long normLineProj2 = (cacheP2 - pivot).Dot(edgeNorm);

                                long normLineMin;
                                long normLineMax;

                                if (normLineProj1 < normLineProj2)
                                {
                                    normLineMin = normLineProj1;
                                    normLineMax = normLineProj2;
                                }
                                else
                                {
                                    normLineMin = normLineProj2;
                                    normLineMax = normLineProj1;
                                }

                                if (normProj >= normLineMin && normProj <= normLineMax)
                                {
                                    long revProj1 = pivot.Dot(cacheAxisNormal);
                                    long revProj2 = nextPoint.Dot(cacheAxisNormal);

                                    long revMin;
                                    long revMax;
                                    if (revProj1 < revProj2)
                                    {
                                        revMin = revProj1;
                                        revMax = revProj2;
                                    }
                                    else
                                    {
                                        revMin = revProj2;
                                        revMax = revProj1;
                                    }

                                    if (cacheProjPerp >= revMin && cacheProjPerp <= revMax)
                                    {
                                        intersected = true;
                                        if (calculateIntersections)
                                        {
                                            long fraction = normLineProj1.Abs().Div(normLineMax - normLineMin);
                                            long intersectionProj = FixedMath.Lerp(lineProj1, lineProj2, fraction);
                                            outputIntersectionPoints.Add(edge * intersectionProj + pivot);

                                            if (outputIntersectionPoints.Count == 2)
                                            {
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        return intersected;
                    }
            }

            return false;
        }
    }
}