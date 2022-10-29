namespace PartyIcons.View;

public enum ChatMode
{
    GameDefault,
    Role,
    Job
}

public struct ChatConfig
{
    public ChatConfig(ChatMode mode, bool colored = true)
    {
        Mode = mode;
        Colored = colored;
    }

    public ChatMode Mode { get; set; }
    public bool Colored { get; set; }

};