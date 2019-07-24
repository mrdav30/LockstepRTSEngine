using FastCollections;
using System;
using System.Collections;

namespace RTSLockstep
{
    public class Influence : ICommandData
    {
        private static readonly FastList<byte> bufferBites = new FastList<byte>();

        private static int bigIndex, smallIndex;
        private static ulong castedBigIndex;
        private static byte cullGroup;
        private static byte castedSmallIndex;
        private static int curIndex;

        public ushort InfluencedAgentLocalID;
        private BitArray Header;
        private readonly byte Data;

        public Influence() { }

        public Influence(RTSAgent influencedAgent)
        {
            InfluencedAgentLocalID = influencedAgent.LocalID;
        }

        public byte[] GetBytes()
        {
            this.Serialize();

            bufferBites.FastClear();
            //Serialize header
            int headerLength = Header.Length;
            int headerArraySize = (headerLength - 1) / 8 + 1;

            bufferBites.Add((byte)headerArraySize);
            byte[] headerBytes = new byte[headerArraySize];

            Header.CopyTo(headerBytes, 0);

            bufferBites.AddRange(headerBytes);

            //Serializing the good stuff
            for (int i = 0; i < Header.Length; i++)
            {
                if (Header.Get(i))
                {
                    bufferBites.Add(Data);
                }
            }
            return bufferBites.ToArray();
        }

        private void Serialize()
        {
            int headerLength = (InfluencedAgentLocalID + 1 - 1) / 8 + 1;
            Header = new BitArray(headerLength, false);

            SerializeID(InfluencedAgentLocalID);
        }

        private void SerializeID(ushort id)
        {

            bigIndex = (id / 8);
            smallIndex = (id % 8);

            Header.Set(bigIndex, true);
        }

        public int Reconstruct(byte[] source, int startIndex)
        {

            curIndex = startIndex;

            byte headerArraySize = source[curIndex++];

            byte[] headerBytes = new byte[headerArraySize];
            Array.Copy(source, curIndex, headerBytes, 0, headerArraySize);
            curIndex += headerArraySize;
            Header = new BitArray(headerBytes);

            for (int i = 0; i < Header.Length; i++)
            {
                if (Header.Get(i))
                {
                    cullGroup = source[curIndex++];
                    for (int j = 0; j < 8; j++)
                    {
                        castedSmallIndex = (byte)(1 << j);
                        if ((cullGroup & (castedSmallIndex)) == castedSmallIndex)
                        {
                            InfluencedAgentLocalID = (ushort)(i * 8 + j);
                        }
                    }
                }
            }
            return curIndex - startIndex;
        }

        public void Write(Writer writer)
        {
            writer.Write(this.GetBytes());
        }
        public void Read(Reader reader)
        {
            int move = this.Reconstruct(reader.Source, reader.Position);
            reader.MovePosition(move);
        }
    }
}