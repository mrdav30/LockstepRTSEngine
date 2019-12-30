using RTSLockstep.Utility.FastCollections;
using RTSLockstep.Simulation.LSMath;

namespace RTSLockstep.Simulation.LSPhysics
{
    /// <summary>
    /// Physics tool for
    /// </summary>
    public static class PhysicsTool
    {
        /// <summary>
        /// Finds all dynamic bodies touching a defined circle.
        /// </summary>
        /// <param name="radius">Radius.</param>
        /// <param name="output">Output.</param>
        public static void CircleCast(Vector2d position, long radius, FastList<LSBody> output)
        {
            long xMin = position.x - radius,
            xmax = position.x + radius;
            long yMin = position.y - radius,
            yMax = position.y + radius;

            //Find the partition tiles we have to search in first
            Partition.GetGridBounds(xMin, xmax, yMin, yMax,
                out int gridXMin, out int gridXMax, out int gridYMin, out int gridYMax);

            for (int i = gridXMin; i <= gridXMax; i++)
            {
                for (int j = gridYMin; j <= gridYMax; j++)
                {
                    PartitionNode node = Partition.GetNode(i, j);
                    for (int k = node.ContainedDynamicObjects.Count - 1; k >= 0; k--)
                    {
                        var body = PhysicsManager.SimObjects[node.ContainedDynamicObjects[k]];
                        long minFastDist = body.Radius + radius;
                        //unnormalized distance value for comparison
                        minFastDist *= minFastDist;

                        if (body.Position.FastDistance(position) <= minFastDist)
                        {
                            //Body touches circle!
                            output.Add(body);
                        }
                    }
                    for (int l = node.ContainedStaticObjects.Count - 1; l >= 0; l--)
                    {
                        var body = PhysicsManager.SimObjects[node.ContainedStaticObjects[l]];
                        long minFastDist = body.Radius + radius;
                        //unnormalized distance value for comparison
                        minFastDist *= minFastDist;

                        if (body.Position.FastDistance(position) <= minFastDist)
                        {
                            //Body touches circle!
                            output.Add(body);
                        }
                    }
                }
            }
        }
    }
}
