using System;

namespace PartyIcons.Entities
{
    public enum Job : uint
    {
        ADV = 0,
        GLA = 1,
        PGL = 2,
        MRD = 3,
        LNC = 4,
        ARC = 5,
        CNJ = 6,
        THM = 7,
        CRP = 8,
        BSM = 9,
        ARM = 10,
        GSM = 11,
        LTW = 12,
        WVR = 13,
        ALC = 14,
        CUL = 15,
        MIN = 16,
        BTN = 17,
        FSH = 18,
        PLD = 19,
        MNK = 20,
        WAR = 21,
        DRG = 22,
        BRD = 23,
        WHM = 24,
        BLM = 25,
        ACN = 26,
        SMN = 27,
        SCH = 28,
        ROG = 29,
        NIN = 30,
        MCH = 31,
        DRK = 32,
        AST = 33,
        SAM = 34,
        RDM = 35,
        BLU = 36,
        GNB = 37,
        DNC = 38,
        RPR = 39,
        SGE = 40
    }

    public static class JobExtensions
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "No, it looks dumb")]
        public static GenericRole GetRole(this Job job)
        {
            switch (job)
            {
                case Job.GLA:
                case Job.MRD:
                case Job.PLD:
                case Job.WAR:
                case Job.DRK:
                case Job.GNB:
                    return GenericRole.Tank;

                case Job.CNJ:
                case Job.AST:
                case Job.WHM:
                case Job.SCH: 
                case Job.SGE:
                    return GenericRole.Healer;

                case Job.PGL:
                case Job.LNC:
                case Job.MNK:
                case Job.DRG:
                case Job.ROG:
                case Job.NIN:
                case Job.SAM: 
                case Job.RPR:
                    return GenericRole.Melee;

                case Job.ARC:
                case Job.BRD:
                case Job.MCH:
                case Job.DNC:
                case Job.THM:
                case Job.BLM:
                case Job.ACN:
                case Job.SMN:
                case Job.RDM:
                case Job.BLU:
                    return GenericRole.Ranged;

                case Job.CRP:
                case Job.BSM:
                case Job.ARM:
                case Job.GSM:
                case Job.LTW:
                case Job.WVR:
                case Job.ALC:
                case Job.CUL:
                    return GenericRole.Crafter;

                case Job.MIN:
                case Job.BTN:
                case Job.FSH:
                    return GenericRole.Gatherer;

                default: throw new ArgumentException($"Unknown jobID {(int)job}");
            }
        }
    }
}