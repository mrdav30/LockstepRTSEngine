namespace RTSLockstep.LSResources
{
    public enum AgentTag
    {
        None,
        Builder,
        Harvester,
        Offensive,
        Convoy,
        Environment
    }

    public enum AgentType
    {
        Unit,
        Structure,
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
        Construct,
        RallyPoint
    }

    public enum RawMaterialType
    {
        Gold,
        Provision,
        Ore,
        Crystal,
        Stone,
        Wood,
        Food
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

    public enum MovementType
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
        Attack,
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

    public enum PathfindingType
    {
        AStar,
        VectorFlowField
    }

    public enum InformationGatherType
    {
        None,
        Position,
        Target,
        PositionOrTarget,
        PositionOrAction
    }

    public enum SelectionRingState
    {
        Selected,
        Highlighted,
        None
    }

    public enum PlacementResult
    {
        Placed,
        Returned,
        Limbo
    }

    public enum MessageType : byte
    {
        Input,
        Frame,
        Init,
        Matchmaking,
        Register,
        Test,
    }

    public enum MarkerType
    {
        None,
        Friendly,
        Neutral,
        Aggresive
    }

    public enum HitType
    {
        None,
        Single,
        Area,
        Cone
    }

    public enum TargetingType
    {
        Timed,
        Homing,
        Directional,
        Positional
    }
}
