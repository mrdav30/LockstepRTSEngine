//Thanks to Sebastian Lague's tutorial: https://www.youtube.com/watch?v=3Dw5d7PlcTM

using System;

namespace RTSLockstep
{
    public static class GridHeap
    {
        public static GridNode[] items = new GridNode[GridManager.DefaultCapacity];
        private static int capacity = GridManager.DefaultCapacity;
        public static uint Count;
        public static uint _Version = 1;

        private static uint childIndexLeft;
        private static uint childIndexRight;
        private static uint swapIndex;
        private static GridNode swapNode;

        private static GridNode curNode;
        private static GridNode newNode;

        private static uint parentIndex;

        private static uint itemAIndex;

        public static void Add(GridNode item)
        {
            item.HeapIndex = Count;
            items[Count++] = item;
            SortUp(item);
            item.HeapVersion = _Version;
        }

        public static GridNode RemoveFirst()
        {
            curNode = items[0];
            newNode = items[--Count];
            items[0] = newNode;
            newNode.HeapIndex = 0;
            SortDown(newNode);
            curNode.HeapVersion--;
            return curNode;
        }

        public static void UpdateItem(GridNode item)
        {
            SortUp(item);
        }

        public static bool Contains(GridNode item)
        {
            return item.HeapVersion == _Version;
        }

        public static void Close(GridNode item)
        {
            item.ClosedHeapVersion = _Version;
        }

        public static bool Closed(GridNode item)
        {
            return item.ClosedHeapVersion == _Version;
        }

        public static void FastClear()
        {
            _Version++;
            Count = 0;
        }

        public static void Reset()
        {
            _Version = 1;
            Count = 0;
        }

        private static void SortDown(GridNode item)
        {
            while (true)
            {
                childIndexLeft = item.HeapIndex * 2 + 1;
                childIndexRight = item.HeapIndex * 2 + 2;
                swapIndex = 0;

                if (childIndexLeft < Count)
                {
                    swapIndex = childIndexLeft;

                    if (childIndexRight < Count)
                    {
                        if (items[childIndexLeft].fCost > (items[childIndexRight]).fCost)
                        {
                            swapIndex = childIndexRight;
                        }
                    }

                    swapNode = items[swapIndex];
                    if (item.fCost > swapNode.fCost)
                    {
                        Swap(item, swapNode);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }
            }
        }

        private static void SortUp(GridNode item)
        {
            if (item.HeapIndex == 0)
            {
                return;
            }
            parentIndex = (item.HeapIndex - 1) / 2;

            while (true)
            {
                curNode = items[parentIndex];
                if (item.fCost < curNode.fCost)
                {
                    Swap(item, curNode);
                }
                else
                {
                    return;
                }

                if (parentIndex == 0)
                {
                    return;
                }

                parentIndex = (item.HeapIndex - 1) / 2;
            }
        }

        private static void Swap(GridNode itemA, GridNode itemB)
        {
            itemAIndex = itemA.HeapIndex;

            items[itemAIndex] = itemB;
            items[itemB.HeapIndex] = itemA;

            itemA.HeapIndex = itemB.HeapIndex;
            itemB.HeapIndex = itemAIndex;
        }
    }
}