using System;
using Dalamud.Configuration;
using Dalamud.Plugin;

namespace PartyIcons
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public event Action OnSave;

        public int  Version { get; set; } = 1;
        public bool ChatContentMessage       = true;
        public bool HideLocalPlayerNameplate = true;
        public bool TestingMode = true;

        public NameplateMode Overworld    { get; set; } = NameplateMode.SmallJobIcon;
        public NameplateMode AllianceRaid { get; set; } = NameplateMode.BigJobIcon;
        public NameplateMode Dungeon      { get; set; } = NameplateMode.BigJobIcon;
        public NameplateMode Raid         { get; set; } = NameplateMode.BigRole;

        private DalamudPluginInterface _interface;

        public void Initialize(DalamudPluginInterface @interface)
        {
            _interface = @interface;
        }

        public void Save()
        {
            _interface.SavePluginConfig(this);
            OnSave?.Invoke();
        }
    }
}
