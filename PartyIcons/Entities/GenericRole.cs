using System;

namespace PartyIcons.Entities;

public enum GenericRole : uint
{
    Tank = 0,
    Melee = 1,
    Ranged = 2,
    Healer = 3,
    Crafter = 4,
    Gatherer = 5
}

public static class JobRoleExtensions
{
    public static Job[] GetJobs(this GenericRole role)
    {
        return role switch
        {
            GenericRole.Tank => new[] {Job.GLA, Job.MRD, Job.PLD, Job.WAR, Job.DRK, Job.GNB},
            GenericRole.Healer => new[] {Job.CNJ, Job.AST, Job.WHM, Job.SCH},
            GenericRole.Melee => new[] {Job.PGL, Job.LNC, Job.MNK, Job.DRG, Job.ROG, Job.NIN, Job.SAM},
            GenericRole.Ranged => new[]
                {Job.ARC, Job.BRD, Job.MCH, Job.DNC, Job.THM, Job.BLM, Job.ACN, Job.SMN, Job.RDM, Job.BLU},
            GenericRole.Crafter => new[] {Job.CRP, Job.BSM, Job.ARM, Job.GSM, Job.LTW, Job.WVR, Job.ALC, Job.CUL},
            GenericRole.Gatherer => new[] {Job.MIN, Job.BTN, Job.FSH},
            _ => throw new ArgumentException($"Unknown jobRoleID {(int) role}")
        };
    }

    public static GenericRole RoleFromByte(byte roleId) => (GenericRole) (roleId - 1);
}
