using FastCollections;
using System;
using System.Collections;

namespace RTSLockstep
{
    public class QueueStructure : ICommandData
    {
        public QStructure Value = new QStructure();

        public QueueStructure() { }

        public QueueStructure(QStructure newStructure)
        {
            Value = newStructure;
        }

        public void Write(Writer writer)
        {
            writer.Write(Value.StructureName);
            writer.Write(Value.BuildPoint.x);
            writer.Write(Value.BuildPoint.y);
            writer.Write(Value.RotationPoint.x);
            writer.Write(Value.RotationPoint.y);
            writer.Write(Value.LocalScale.x);
            writer.Write(Value.LocalScale.y);
            writer.Write(Value.LocalScale.z);
            writer.Write(Value.HalfWidth);
            writer.Write(Value.HalfLength);
        }

        public void Read(Reader reader)
        {
            this.Value.StructureName = reader.ReadString();
            long buildX = reader.ReadLong();
            long buildY = reader.ReadLong();
            this.Value.BuildPoint = new Vector2d(buildX, buildY);
            long rotX = reader.ReadLong();
            long rotY = reader.ReadLong();
            this.Value.RotationPoint = new Vector2d(rotX, rotY);
            long scaleX = reader.ReadLong();
            long scaleY = reader.ReadLong();
            long scaleZ = reader.ReadLong();
            this.Value.LocalScale = new Vector3d(scaleX, scaleY, scaleZ);
            this.Value.HalfWidth = reader.ReadLong();
            this.Value.HalfLength = reader.ReadLong();
        }
    }
}