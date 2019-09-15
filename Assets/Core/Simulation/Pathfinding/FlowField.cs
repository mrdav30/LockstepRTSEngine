namespace RTSLockstep.Pathfinding
{
    public class FlowField
    {
        public int Distance;
        public Vector2d Direction;
        public bool HasLOS;

        public FlowField(int _distance, Vector2d _direction, bool _hasLOS = false)
        {
            this.Distance = _distance;
            this.Direction = _direction;

            this.HasLOS = _hasLOS;
        }
    }
}