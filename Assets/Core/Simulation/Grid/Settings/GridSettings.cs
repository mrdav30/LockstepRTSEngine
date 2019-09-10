namespace RTSLockstep
{
    public sealed class GridSettings
    {
        public int Width { get; private set; } = 500;

        public int Height { get; private set; } = 500;

        public long XOffset { get; private set; } = FixedMath.Create(-100);

        public long YOffset { get; private set; } = FixedMath.Create(-100);

        public bool UseDiagonalConnections { get; private set; } = true;

        public PathfindingType PathfindingAlgorithm = PathfindingType.VectorFlowField;

        public GridSettings()
        {
            Init(Width, Height, XOffset, YOffset, UseDiagonalConnections, PathfindingAlgorithm);
        }

        public GridSettings(int width, int height, long xOffset, long yOffset, bool useDiagonalConnections, PathfindingType pathfindingAlgorithm)
        {
            Init(width, height, xOffset, yOffset, useDiagonalConnections, pathfindingAlgorithm);
        }

        private void Init(int width, int height, long xOffset, long yOffset, bool useDiagonalConnections, PathfindingType pathfindingAlgorithm)
        {
            this.Width = width;
            this.Height = height;
            this.XOffset = xOffset;
            this.YOffset = yOffset;
            this.UseDiagonalConnections = useDiagonalConnections;
        }
    }
}