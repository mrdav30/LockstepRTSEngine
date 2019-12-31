namespace RTSLockstep.Player
{
    public struct PlayerDetails
    {
        public PlayerDetails(string name, int avatar, int controllerId, int playerIndex)
        {
            Name = name;
            Avatar = avatar;
            ControllerId = controllerId;
            PlayerIndex = playerIndex;
        }
        // using the name of the Player as a unique identifier
        // to support multiplayer at some point this may need to be modified
        public string Name { get; private set; }
        public int Avatar { get; private set; }
        public int ControllerId { get; private set; }
        public int PlayerIndex { get; private set; }
    }
}
