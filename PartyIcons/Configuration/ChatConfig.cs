namespace PartyIcons.Configuration;

public class ChatConfig
{
    public ChatConfig(ChatMode mode, bool useRoleColor = true)
    {
        Mode = mode;
        UseRoleColor = useRoleColor;
    }

    public ChatMode Mode { get; set; }

    public bool UseRoleColor { get; set; }
}