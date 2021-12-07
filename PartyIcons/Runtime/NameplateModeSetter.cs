using System.Linq;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Gui;
using Dalamud.IoC;
using Dalamud.Logging;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.View;

namespace PartyIcons.Runtime
{
    public sealed class NameplateModeSetter
    {
        [PluginService] public ClientState ClientState { get; set; }
        [PluginService] public DataManager DataManager { get; set; }
        [PluginService] public ChatGui     ChatGui     { get; set; }

        private readonly NameplateView _nameplateView;
        private readonly Configuration _configuration;

        private ExcelSheet<ContentFinderCondition> _contentFinderConditionsSheet;

        public NameplateModeSetter(NameplateView nameplateView, Configuration configuration)
        {
            _nameplateView = nameplateView;
            _configuration = configuration;
        }

        public void Enable()
        {
            _contentFinderConditionsSheet = DataManager.GameData.GetExcelSheet<ContentFinderCondition>();

            ForceRefresh();
            ClientState.TerritoryChanged += OnTerritoryChanged;
        }

        public void ForceRefresh()
        {
            OnTerritoryChanged(null, 0);
            _nameplateView.OthersMode = _configuration.Others;
        }

        public void Disable()
        {
            ClientState.TerritoryChanged -= OnTerritoryChanged;
        }

        public void Dispose()
        {
            Disable();
        }

        private void OnTerritoryChanged(object? sender, ushort e)
        {
            var content = _contentFinderConditionsSheet.FirstOrDefault(t => t.TerritoryType.Row == ClientState.TerritoryType);
            if (content == null)
            {
                PluginLog.Information($"Content null {ClientState.TerritoryType}");
                _nameplateView.PartyMode = _configuration.Overworld;
                return;
            }

            if (_configuration.ChatContentMessage)
            {
                ChatGui.Print($"Entering {content.Name}.");
            }

            PluginLog.Debug($"Territory changed {content.Name} (type {content.ContentType.Row}, terr {ClientState.TerritoryType}, memtype {content.ContentMemberType.Row})");

            switch (content.ContentMemberType.Row)
            {
                case 2:
                    _nameplateView.PartyMode = _configuration.Dungeon;
                    break;

                case 3:
                    _nameplateView.PartyMode = _configuration.Raid;
                    break;

                case 4:
                    _nameplateView.PartyMode = _configuration.AllianceRaid;
                    break;

                default:
                    _nameplateView.PartyMode = _configuration.Dungeon;
                    break;
            }
        }
    }
}
