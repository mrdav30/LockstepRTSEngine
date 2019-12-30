using RTSLockstep.Data;
using RTSLockstep.Simulation.LSMath;
using RTSLockstep.Utility;

namespace RTSLockstep.BuildSystem
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
            Value.StructureName = reader.ReadString();
            long buildX = reader.ReadLong();
            long buildY = reader.ReadLong();
            Value.BuildPoint = new Vector2d(buildX, buildY);
            long rotX = reader.ReadLong();
            long rotY = reader.ReadLong();
            Value.RotationPoint = new Vector2d(rotX, rotY);
            long scaleX = reader.ReadLong();
            long scaleY = reader.ReadLong();
            long scaleZ = reader.ReadLong();
            Value.LocalScale = new Vector3d(scaleX, scaleY, scaleZ);
            Value.HalfWidth = reader.ReadLong();
            Value.HalfLength = reader.ReadLong();
        }
    }
}