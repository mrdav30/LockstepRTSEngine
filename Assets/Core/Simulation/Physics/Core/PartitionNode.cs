using UnityEngine;
using FastCollections;

namespace RTSLockstep
{
    public class PartitionNode
    {

        /// <summary>
        /// Stores dynamic bodies' PhysicsManager IDs.
        /// </summary>
        public readonly FastList<int> ContainedDynamicObjects = new FastList<int>();
        public readonly FastList<int> ContainedStaticObjects = new FastList<int>();

        private static int id1, id2;
        private static CollisionPair pair;

        public int DynamicCount { get { return ContainedDynamicObjects.Count; } }


        public void Reset()
        {
            ContainedDynamicObjects.FastClear();
            ContainedStaticObjects.FastClear();
        }

        private int activationID;

        public void AddDynamicObject(int item)
        {
            if (DynamicCount == 0)
            {
                activationID = Partition.AddNode(this);
            }
            ContainedDynamicObjects.Add(item);
        }

        public void AddStaticObject(int item)
        {
            ContainedStaticObjects.Add(item);

        }

        public void RemoveDynamicObject(int item)
        {
            //todo get rid of this linear search
            if (ContainedDynamicObjects.Remove(item))
            {
                if (DynamicCount == 0)
                {
                    Partition.RemoveNode(activationID);
                    activationID = -1;
                }
            }
            else
            {
                Debug.LogError("Dynamic item not removed");
            }
        }

        public void RemoveStaticObject(int item)
        {
            if (!ContainedStaticObjects.Remove(item))
            {
                Debug.LogError("Static item not removed");
            }
        }

        public void Distribute()
        {
            int nodePeakCount = DynamicCount;
            int immovableObjectsCount = ContainedStaticObjects.Count;
            for (int j = 0; j < nodePeakCount; j++)
            {
                id1 = ContainedDynamicObjects[j];
                for (int k = j + 1; k < nodePeakCount; k++)
                {
                    id2 = ContainedDynamicObjects[k];
                    if (id1 != id2)
                    {
                        ProcessPair();
                    }
                }
                for (int k = 0; k < immovableObjectsCount; k++)
                {
                    id2 = ContainedStaticObjects[k];
                    ProcessPair();
                }
            }
        }

        void ProcessPair()
        {
            Partition.count++;
            pair = PhysicsManager.GetCollisionPairRaw(id1, id2);
            if (pair.IsNotNull())
            {
                //Ensures collision pairs are not run twice
                if (pair.PartitionVersion != Partition._Version)
                {
                    pair.PartitionVersion = Partition._Version;
                    pair.CheckAndDistributeCollision();
                }
            }

        }

        public int this[int index]
        {
            get
            {
                return ContainedDynamicObjects[index];
            }
            set
            {
                ContainedDynamicObjects[index] = value;
            }
        }

    }
}