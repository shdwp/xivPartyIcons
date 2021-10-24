using System;
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
        private ExcelSheet<ContentType>            _contentTypesSheet;

        public NameplateModeSetter(NameplateView nameplateView, Configuration configuration)
        {
            _nameplateView = nameplateView;
            _configuration = configuration;
        }

        public void Enable()
        {
            _contentFinderConditionsSheet = DataManager.GameData.GetExcelSheet<ContentFinderCondition>();
            _contentTypesSheet = DataManager.GameData.GetExcelSheet<ContentType>();

            ForceRefresh();
            ClientState.TerritoryChanged += OnTerritoryChanged;
        }

        public void ForceRefresh()
        {
            OnTerritoryChanged(null, 0);
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
                _nameplateView.Mode = _configuration.Overworld;
                return;
            }

            var type = _contentTypesSheet.GetRow(content.ContentType.Row);
            if (type == null)
            {
                PluginLog.Information($"Content type null {content.ContentType.Row}");
                _nameplateView.Mode = _configuration.Overworld;
                return;
            }

            PluginLog.Debug($"Territory changed {content.Name} ({type.Name}, {ClientState.TerritoryType})");
            if (_configuration.ChatContentMessage)
            {
                ChatGui.Print($"Entering {content.Name} ({type.Name}).");
            }

            _nameplateView.Mode = type.Name.RawString switch
            {
                "Trials"         => _configuration.Raid,
                "Dungeons"       => _configuration.Dungeon,
                "Raids"          => _configuration.Raid,
                "Alliance Raids" => _configuration.AllianceRaid,

                _ => _configuration.Dungeon,
            };
        }
    }
}
