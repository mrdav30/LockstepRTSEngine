namespace RTSLockstep
{
    public enum CursorState 
    {
        Pointer,
        Select,
        Move,
        Attack,
        PanLeft,
        PanRight,
        PanUp,
        PanDown,
        Harvest,
        Deposit,
        RallyPoint
    }

    public enum ResourceType
    {
        Gold, 
        Army,
        Ore,
        Crystal,
        Stone,
        Wood,
        Food,
        Unknown
    }

    public enum WorkerRole
    {
        Harvester,
        Builder
    }

    public enum FlagState
    {
        SetFlag,
        SettingFlag,
        FlagSet
    }

    public enum SelectionBoxState
    {
        Selected,
        Highlighted,
        None
    }
	
	public enum AnimState
    {
        Idling,
        IdlingWood,
        IdlingOre,
        Moving,
        MovingWood,
        MovingOre,
        Dying,
        Engaging,
        EngagingWood,
        EngagingOre,
        SpecialEngaging,
        Constructing,
        Building,
        Spawning,
        Working
    }

    public enum AnimImpulse
    {
        Fire,
        SpecialFire,
        SpecialAttack,
        Extra
    }
}
