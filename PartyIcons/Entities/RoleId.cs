namespace PartyIcons.Entities;

public enum RoleId
{
    Undefined,
    MT = 1,
    OT,
    M1,
    M2,
    R1,
    R2,
    H1,
    H2
}

public static class RoleIdUtils
{
    public static RoleId Counterpart(RoleId roleId)
    {
        return roleId switch
        {
            RoleId.MT => RoleId.OT,
            RoleId.OT => RoleId.MT,
            RoleId.H1 => RoleId.H2,
            RoleId.H2 => RoleId.H1,
            RoleId.M1 => RoleId.M2,
            RoleId.M2 => RoleId.M1,
            RoleId.R1 => RoleId.R2,
            RoleId.R2 => RoleId.R1,
            _ => RoleId.Undefined
        };
    }
}
