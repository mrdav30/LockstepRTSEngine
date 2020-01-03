using System;
using System.Collections.Generic;

using RTSLockstep.Utility.FastCollections;
using RTSLockstep.BuildSystem;
using RTSLockstep.Data;
using RTSLockstep.Player.Utility;
using RTSLockstep.Simulation.Influence;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep.Player.Commands
{
    public partial class Command
    {
        public byte ControllerID;
        public ushort InputCode;

        private static readonly FastList<byte> _serializeList = new FastList<byte>();
        private static readonly Writer _writer = new Writer(_serializeList);
        private static readonly Reader _reader = new Reader();

        private static ushort _registerCount;

        private static BiDictionary<Type, ushort> _registeredData = new BiDictionary<Type, ushort>();
        /// <summary>
        /// Backward compatability for InputCode
        /// </summary>
        /// <value>The le input.</value>
        private Dictionary<ushort, FastList<ICommandData>> _containedData = new Dictionary<ushort, FastList<ICommandData>>();
        private ushort _containedTypesCount;

        public static void Setup()
        {
            RegisterDefaults();
        }

        private static void RegisterDefaults()
        {
            //			#if UNITY_IOS
            Register<DefaultData>();
            Register<EmptyData>();
            Register<Coordinate>();
            Register<Selection>();
            Register<Influence>();
            Register<Vector2d>();
            Register<Vector3d>();
            Register<QueueStructure>();
            //			#else
            //				foreach (Type t in Assembly.GetCallingAssembly().GetTypes())
            //				{
            //					if (t.GetInterface("ICommandData") != null)
            //					{
            //						Register (t);
            //					} 
            //				}
            //			#endif
        }

        private static void Register<TData>() where TData : ICommandData
        {
            Register(typeof(TData));
        }

        private static void Register(Type t)
        {
            if (_registerCount > ushort.MaxValue)
            {
                throw new Exception(string.Format("Cannot register more than {0} types of data.", ushort.MaxValue + 1));
            }
            if (_registeredData.ContainsKey(t))
            {
                return;
            }

            _registeredData.Add(t, _registerCount++);
        }

        public Command()
        {
        }

        public Command(ushort inputCode, byte controllerID = byte.MaxValue)
        {
            Initialize();
            InputCode = inputCode;
            ControllerID = controllerID;
        }

        public void Initialize()
        {
            _containedData.Clear();
            _containedTypesCount = 0;
        }

        public void Add<TData>(params TData[] addItems) where TData : ICommandData
        {
            for (int i = 0; i < addItems.Length; i++)
            {
                Add(addItems[i]);
            }
        }

        public void Add<TData>(TData item) where TData : ICommandData
        {
            if (_registeredData.TryGetValue(typeof(TData), out ushort dataID))
            {
                FastList<ICommandData> items = GetItemsList(dataID);
                if (items.Count == ushort.MaxValue)
                {
                    throw new Exception("No more than '{0}' of a type can be added to a Command.");
                }
                if (items.Count == 0)
                {
                    _containedTypesCount++;
                }

                items.Add(item);
            }
            else
            {
                throw new Exception(string.Format("Type '{0}' not registered.", typeof(TData)));
            }
        }

        public bool ContainsData<TData>()
        {
            return GetDataCount<TData>() > 0;
        }

        public int GetDataCount<TData>()
        {
            if (!_registeredData.TryGetValue(typeof(TData), out ushort dataID))
            {
                return 0;
            }

            if (!_containedData.TryGetValue(dataID, out FastList<ICommandData> items))
            {
                return 0;
            }

            return items.Count;
        }

        public TData GetData<TData>(int index = 0) where TData : ICommandData
        {
            if (TryGetData(out TData item, index))
            {
                return item;
            }
            return default;
        }

        public TData[] GetDataArray<TData>() where TData : ICommandData
        {
            int count = GetDataCount<TData>();
            TData[] array = new TData[count];
            for (int i = 0; i < count; i++)
            {
                array[i] = GetData<TData>(i);
            }

            return array;
        }

        public bool TryGetData<TData>(out TData data, int index = 0) where TData : ICommandData
        {
            data = default;
            if (!_registeredData.TryGetValue(typeof(TData), out ushort dataID))
            {
                return false;
            }

            if (!_containedData.TryGetValue(dataID, out FastList<ICommandData> items))
            {
                return false;
            }

            if (items.Count <= index)
            {
                return false;
            }

            data = (TData)items[index];
            return true;
        }

        public void SetData<TData>(TData value, int index = 0) where TData : ICommandData
        {
            _containedData[_registeredData[typeof(TData)]][index] = value;
        }

        public bool SetFirstData<TData>(TData value) where TData : ICommandData
        {
            if (!_registeredData.TryGetValue(typeof(TData), out ushort dataID))
            {
                return false;
            }

            if (!_containedData.TryGetValue(dataID, out FastList<ICommandData> items))
            {
                return false;
            }

            if (items.Count == 0)
            {
                items.Add(value);
            }
            else
            {
                items[0] = value;
            }

            return true;
        }

        public void ClearData<TData>()
        {
            _containedData[_registeredData[typeof(TData)]].Clear();
        }

        /// <summary>
        /// Reconstructs this command from a serialized command and returns the size of the command.
        /// </summary>
        public int Reconstruct(byte[] Source, int StartIndex = 0)
        {
            _reader.Initialize(Source, StartIndex);
            ControllerID = _reader.ReadByte();
            InputCode = _reader.ReadUShort();
            _containedTypesCount = _reader.ReadUShort();
            for (int i = 0; i < _containedTypesCount; i++)
            {
                ushort dataID = _reader.ReadUShort();
                ushort dataCount = _reader.ReadUShort();

                FastList<ICommandData> items = GetItemsList(dataID);
                Type dataType = _registeredData.GetReversed(dataID);
                for (int j = 0; j < dataCount; j++)
                {
                    ICommandData item = Activator.CreateInstance(dataType) as ICommandData;
                    item.Read(_reader);
                    items.Add(item);
                }
            }

            return _reader.Position - StartIndex;
        }

        public byte[] Serialized
        {
            get
            {
                _writer.Reset();

                //Essential Information
                _writer.Write(ControllerID);
                _writer.Write(InputCode);
                _writer.Write(_containedTypesCount);
                foreach (KeyValuePair<ushort, FastList<ICommandData>> pair in _containedData)
                {
                    _writer.Write(pair.Key);
                    _writer.Write((ushort)pair.Value.Count);
                    for (int i = 0; i < pair.Value.Count; i++)
                    {
                        pair.Value[i].Write(_writer);
                    }
                }

                return _serializeList.ToArray();
            }
        }

        FastList<ICommandData> GetItemsList(ushort dataID)
        {
            if (!_containedData.TryGetValue(dataID, out FastList<ICommandData> items))
            {
                items = new FastList<ICommandData>();
                _containedData.Add(dataID, items);
            }

            return items;
        }

        public Command Clone()
        {
            Command com = new Command
            {
                ControllerID = ControllerID,
                InputCode = InputCode
            };
            foreach (KeyValuePair<ushort, FastList<ICommandData>> pair in _containedData)
            {
                FastList<ICommandData> list = new FastList<ICommandData>();
                pair.Value.CopyTo(list);

                com._containedData.Add(pair.Key, list);
            }

            return com;
        }
    }
}