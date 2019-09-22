namespace RTSLockstep.Pathfinding
{
    public class FlowField
    {
        public Vector2d WorldPos;
        public int Distance;
        public Vector2d Direction;
        public bool HasLOS;

        public FlowField(Vector2d _worldPos, int _distance, Vector2d _direction, bool _hasLOS = false)
        {
            this.WorldPos = _worldPos;
            this.Distance = _distance;
            this.Direction = _direction;

            this.HasLOS = _hasLOS;
        }
    }
}