namespace RTSLockstep
{
    public enum AgentTag
    {
        None,
        Builder,
        Harvester,
        Infantry,
        Ranged,
        Convoy,
        Environment
    }

    public enum AgentType
    {
        Unit,
        Building,
        Resource
    }

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
        Provision,
        Ore,
        Crystal,
        Stone,
        Wood,
        Food,
        Unknown
    }

    // Used to determine influence on structure
    public enum StructureType
    {
        None,
        Defensive,
        Wall,
        Blocker,
        SpawnAndResearch
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

    public enum MovementType : long
    {
        Group,
        GroupIndividual,
        Individual
    }

    public enum AnimState
    {
        None,
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

    public enum UserInputKeyMappings
    {
        RotateLeftShortCut,
        RotateRightShortCut,
        MainMenuShortCut,
        SpawnMenuShortCut,
        ConstructMenuShortCut,
        RepairShortCut,
        RallyShortCut,
        AttackShortCut
    }

    //Implemented as flags for selecting multiple types.
    [System.Flags]
    public enum AllegianceType : byte
    {
        Neutral = 1 << 0,
        Friendly = 1 << 1,
        Enemy = 1 << 2,
        All = 0xff
    }
}
