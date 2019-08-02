namespace RTSLockstep
{
    public class QStructure
    {
        public string StructureName { get; set; }
        public Vector2d BuildPoint { get; set; }
        public Vector2d RotationPoint { get; set; }
        public Vector3d LocalScale { get; set; }

        public int BuildSizeLow { get; set; }
        public int BuildSizeHigh { get; set; }
    }
}
