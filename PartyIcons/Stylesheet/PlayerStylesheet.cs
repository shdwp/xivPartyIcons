using System.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using PartyIcons.Entities;
using PartyIcons.Utils;

namespace PartyIcons.Stylesheet
{
    public sealed class PlayerStylesheet
    {
        private readonly Configuration _configuration;

        public PlayerStylesheet(Configuration configuration)
        {
            _configuration = configuration;
        }

        public ushort GetGenericRoleColor(GenericRole role)
        {
            switch (role)
            {
                case GenericRole.Tank:
                    return 37;

                case GenericRole.Melee:
                    return 524;

                case GenericRole.Ranged:
                    return 32;

                case GenericRole.Healer:
                    return 42;

                default:
                    return 0;
            }
        }

        public ushort GetRoleColor(RoleId roleId)
        {
            switch (roleId)
            {
                case RoleId.MT:
                case RoleId.OT:
                    return GetGenericRoleColor(GenericRole.Tank);

                case RoleId.M1:
                case RoleId.M2:
                    return GetGenericRoleColor(GenericRole.Melee);

                case RoleId.R1:
                case RoleId.R2:
                    return GetGenericRoleColor(GenericRole.Ranged);

                case RoleId.H1:
                case RoleId.H2:
                    return GetGenericRoleColor(GenericRole.Healer);

                default:
                    return 0;
            }
        }

        public string GetRoleIconset(RoleId roleId)
        {
            return roleId switch
            {
                RoleId.MT => "Blue",
                RoleId.OT => "Blue",
                RoleId.M1 => "Red",
                RoleId.M2 => "Red",
                RoleId.R1 => "Orange",
                RoleId.R2 => "Orange",
                RoleId.H1 => "Green",
                RoleId.H2 => "Green",
                _         => "Grey",
            };
        }

        public string GetRoleName(RoleId roleId)
        {
            return roleId switch
            {
                RoleId.MT => "MT",
                RoleId.OT => _configuration.EasternNamingConvention ? "ST" : "OT",
                _         => roleId.ToString(),
            };
        }

        public SeString GetGenericRolePlate(GenericRole genericRole)
        {
            if (genericRole <= GenericRole.Healer)
            {
                return genericRole switch
                {
                    GenericRole.Tank   => SeStringUtils.Text(BoxedCharacterString("T"), GetGenericRoleColor(genericRole)),
                    GenericRole.Melee  => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M"), GetGenericRoleColor(genericRole)),
                    GenericRole.Ranged => SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R"), GetGenericRoleColor(genericRole)),
                    GenericRole.Healer => SeStringUtils.Text(BoxedCharacterString("H"), GetGenericRoleColor(genericRole)),
                };
            }
            else
            {
                return "";
            }
        }

        public SeString GetRolePlate(RoleId roleId)
        {
            switch (roleId)
            {
                case RoleId.MT:
                    return SeStringUtils.Text(BoxedCharacterString("MT"), GetRoleColor(roleId));

                case RoleId.OT:
                    return SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "ST" : "OT"), GetRoleColor(roleId));

                case RoleId.M1:
                case RoleId.M2:
                    return SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "M") + GetRolePlateNumber(roleId), GetRoleColor(roleId));

                case RoleId.R1:
                case RoleId.R2:
                    return SeStringUtils.Text(BoxedCharacterString(_configuration.EasternNamingConvention ? "D" : "R") + GetRolePlateNumber(roleId), GetRoleColor(roleId));

                case RoleId.H1:
                case RoleId.H2:
                    return SeStringUtils.Text(BoxedCharacterString("H") + GetRolePlateNumber(roleId), GetRoleColor(roleId));

                default:
                    return string.Empty;
            }
        }

        public SeString GetRolePlateNumber(RoleId roleId)
        {
            if (_configuration.EasternNamingConvention)
            {
                return roleId switch
                {
                    RoleId.MT => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                    RoleId.OT => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                    RoleId.H1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                    RoleId.H2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                    RoleId.M1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                    RoleId.M2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                    RoleId.R1 => SeStringUtils.Text(BoxedCharacterString("3"), GetRoleColor(roleId)),
                    RoleId.R2 => SeStringUtils.Text(BoxedCharacterString("4"), GetRoleColor(roleId)),
                };
            }
            else
            {
                return roleId switch
                {
                    RoleId.MT => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                    RoleId.OT => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                    RoleId.H1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                    RoleId.H2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                    RoleId.M1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                    RoleId.M2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                    RoleId.R1 => SeStringUtils.Text(BoxedCharacterString("1"), GetRoleColor(roleId)),
                    RoleId.R2 => SeStringUtils.Text(BoxedCharacterString("2"), GetRoleColor(roleId)),
                };
            }
        }

        public SeString GetPartySlotNumber(uint number, GenericRole genericRole)
        {
            return SeStringUtils.Text(BoxedCharacterString(number.ToString()), GetGenericRoleColor(genericRole));
        }

        public SeString GetRoleChatPrefix(RoleId roleId)
        {
            return GetRolePlate(roleId);
        }

        public ushort GetRoleChatColor(RoleId roleId)
        {
            return GetRoleColor(roleId);
        }

        public SeString GetGenericRoleChatPrefix(ClassJob classJob)
        {
            return GetGenericRolePlate(JobExtensions.GetRole((Job)classJob.RowId));
        }

        public ushort GetGenericRoleChatColor(ClassJob classJob)
        {
            return GetGenericRoleColor(JobExtensions.GetRole((Job)classJob.RowId));
        }


        public SeString GetJobChatPrefix(ClassJob classJob)
        {
            if (true)
            {
                return new SeString(
                    new UIGlowPayload(GetGenericRoleChatColor(classJob)),
                    new UIForegroundPayload(GetGenericRoleChatColor(classJob)),
                    new TextPayload(classJob.Abbreviation),
                    UIForegroundPayload.UIForegroundOff,
                    UIGlowPayload.UIGlowOff
                );
            }
        }

        public ushort GetJobChatColor(ClassJob classJob)
        {
            return GetGenericRoleColor(JobExtensions.GetRole((Job)classJob.RowId));
        }

        public string BoxedCharacterString(string str)
        {
            var builder = new StringBuilder(str.Length);
            foreach (var ch in str.ToLower())
            {
                builder.Append(ch switch
                {
                    _ when (ch >= 'a' && ch <= 'z') => (char)(ch + 57360),
                    _ when (ch >= '0' && ch <= '9') => (char)(ch + 57439),

                    _ => ch,
                });
            }

            return builder.ToString();
        }
    }
}