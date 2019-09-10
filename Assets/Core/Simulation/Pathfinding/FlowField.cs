namespace RTSLockstep.Pathfinding
{
    public class FlowField
    {
        public int Distance;
        public Vector2d Direction;

        public FlowField(int distance, Vector2d direction)
        {
            this.Distance = distance;
            this.Direction = direction;
        }
    }
}