using System;

namespace PartyIcons.Entities
{
    public enum JobRole : uint
    {
        Tank     = 0,
        Melee    = 1,
        Ranged   = 2,
        Healer   = 3,
        Crafter  = 4,
        Gatherer = 5,
        Magical  = 6,
    }

    public static class JobRoleExtensions
    {
        public static Job[] GetJobs(this JobRole role)
        {
            return role switch
            {
                JobRole.Tank     => new[] { Job.GLA, Job.MRD, Job.PLD, Job.WAR, Job.DRK, Job.GNB },
                JobRole.Healer   => new[] { Job.CNJ, Job.AST, Job.WHM, Job.SCH },
                JobRole.Melee    => new[] { Job.PGL, Job.LNC, Job.MNK, Job.DRG, Job.ROG, Job.NIN, Job.SAM },
                JobRole.Ranged   => new[] { Job.ARC, Job.BRD, Job.MCH, Job.DNC },
                JobRole.Crafter  => new[] { Job.CRP, Job.BSM, Job.ARM, Job.GSM, Job.LTW, Job.WVR, Job.ALC, Job.CUL },
                JobRole.Gatherer => new[] { Job.MIN, Job.BTN, Job.FSH },
                JobRole.Magical  => new[] { Job.THM, Job.BLM, Job.ACN, Job.SMN, Job.RDM, Job.BLU },
                _                => throw new ArgumentException($"Unknown jobRoleID {(int)role}"),
            };
        }

        public static JobRole RoleFromByte(byte roleId)
        {
            return (JobRole)(roleId - 1);
        }
    }
}
