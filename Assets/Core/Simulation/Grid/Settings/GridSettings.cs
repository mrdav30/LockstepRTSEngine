namespace RTSLockstep
{
    public sealed class GridSettings
    {
        public int Width { get; private set; }

        public int Height { get; private set; }

        public long XOffset { get; private set; }

        public long YOffset { get; private set; }

        public bool UseDiagonalConnections { get; private set; }

        public GridSettings()
        {
            Init(500, 500, FixedMath.Create(-100), FixedMath.Create(-100), true);
        }

        public GridSettings(int width, int height, long xOffset, long yOffset, bool useDiagonalConnections)
        {
            Init(width, height, xOffset, yOffset, useDiagonalConnections);
        }

        private void Init(int width, int height, long xOffset, long yOffset, bool useDiagonalConnections)
        {
            this.Width = width;
            this.Height = height;
            this.XOffset = xOffset;
            this.YOffset = yOffset;
            this.UseDiagonalConnections = useDiagonalConnections;
        }
    }
}