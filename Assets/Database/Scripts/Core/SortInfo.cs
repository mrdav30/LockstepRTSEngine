namespace RTSLockstep.Data
{
    public delegate int DataItemSorter(DataItem item);

    public class SortInfo
    {


        public SortInfo(string name, DataItemSorter sorter)
        {

            _sortName = name;
            _degreeGetter = sorter;
        }

        public string _sortName;
        public DataItemSorter _degreeGetter;
        public string sortName { get { return _sortName; } }
        public DataItemSorter degreeGetter { get { return _degreeGetter; } }
    }
}