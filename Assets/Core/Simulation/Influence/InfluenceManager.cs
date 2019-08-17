using FastCollections;
using System;
using System.Collections;
/*
 * Finds clostest agent that matches conditional parameter
 */
namespace RTSLockstep
{
    public static class InfluenceManager
    {
        static FastList<FastBucket<LSInfluencer>> bufferBuckets = new FastList<FastBucket<LSInfluencer>>();
        public static FastList<RTSAgent> bufferAgents = new FastList<RTSAgent>();
        private static FastList<LSBody> bufferBodies = new FastList<LSBody>();
        const int FoundScanBuffer = 5;

        public static void Initialize()
        {
            //DeltaCache.GenerateCache ();
        }

        public static void Simulate()
        {

        }

        // not sure why this is here...
        //public static int GenerateDeltaCount(long radius)
        //{
        //	radius /= GridManager.ScanResolution;
        //	int ret = FixedMath.Mul(FixedMath.Mul(radius, radius), FixedMath.Pi).CeilToInt();
        //	//if (ret < 5) ret = 5;
        //	return ret;
        //}

        #region Scanning
        // find single unit
        public static RTSAgent Scan(Vector2d position, long radius, Func<RTSAgent, bool> agentConditional, Func<byte, bool> bucketConditional)
        {
            ScanAll(position, radius, agentConditional, bucketConditional, bufferAgents);
            return FindClosestAgent(position, bufferAgents);
        }

        public static void ScanAll(Vector2d position, long radius, Func<RTSAgent, bool> agentConditional, Func<byte, bool> bucketConditional, FastList<RTSAgent> output)
        {
            //If radius is too big and we scan too many tiles, performance will be bad
            const long circleCastRadius = FixedMath.One * 16;
            output.FastClear();

            if (radius >= circleCastRadius)
            {
                bufferBodies.FastClear();
                PhysicsTool.CircleCast(position, radius, bufferBodies);
                for (int i = 0; i < bufferBodies.Count; i++)
                {
                    var body = bufferBodies[i];
                    var agent = body.Agent;
                    //we have to check agent's controller since we did not filter it through buckets
                    if (agent.IsNotNull() && bucketConditional(agent.Controller.ControllerID))
                    {
                        if (agentConditional(agent))
                        {
                            output.Add(agent);
                        }
                    }
                }
                return;
            }

            int xMin = ((position.x - radius - GridManager.OffsetX) / GridManager.ScanResolution).ToInt();
            int xMax = ((position.x + radius - GridManager.OffsetX) / GridManager.ScanResolution).CeilToInt();
            int yMin = ((position.y - radius - GridManager.OffsetY) / GridManager.ScanResolution).ToInt();
            int yMax = ((position.y + radius - GridManager.OffsetY) / GridManager.ScanResolution).CeilToInt();

            long fastRadius = radius * radius;
            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    ScanNode tempNode = GridManager.GetScanNode(x,y);

                    if (tempNode.IsNotNull())
                    {
                        if (tempNode.AgentCount > 0)
                        {
                            bufferBuckets.FastClear();
                            tempNode.GetBucketsWithAllegiance(bucketConditional, bufferBuckets);
                            for (int i = 0; i < bufferBuckets.Count; i++)
                            {
                                FastBucket<LSInfluencer> tempBucket = bufferBuckets[i];
                                BitArray arrayAllocation = tempBucket.arrayAllocation;
                                for (int j = 0; j < tempBucket.PeakCount; j++)
                                {
                                    if (arrayAllocation.Get(j))
                                    {
                                        RTSAgent tempAgent = tempBucket[j].Agent;

                                        long distance = (tempAgent.Body.Position - position).FastMagnitude();
                                        if (distance < fastRadius)
                                        {

                                            if (agentConditional(tempAgent))
                                            {
                                                output.Add(tempAgent);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static RTSAgent FindClosestAgent(Vector2d position, FastList<RTSAgent> agents)
        {
            long sourceX = position.x;
            long sourceY = position.y;
            RTSAgent closestAgent = null;
            long closestDistance = 0;
            int foundBuffer = FoundScanBuffer;
            foreach (RTSAgent agent in agents)
            {

                if (closestAgent != null)
                {
                    long tempDistance = agent.Body._position.FastDistance(sourceX, sourceY);
                    if (tempDistance < closestDistance)
                    {
                        closestAgent = agent;
                        closestDistance = tempDistance;
                        foundBuffer = FoundScanBuffer;
                    }
                    else
                    {
                        foundBuffer--;
                    }
                }
                else
                {
                    closestAgent = agent;
                    closestDistance = agent.Body._position.FastDistance(sourceX, sourceY);
                }
            }
            return closestAgent;
        }
        #endregion
    }
}