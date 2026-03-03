using PoleBarnGenerator.Models;
using System;

namespace PoleBarnGenerator.Generators.TrussProfiles
{
    /// <summary>
    /// Factory for creating truss profile strategy instances.
    /// </summary>
    public static class TrussFactory
    {
        public static ITrussProfile GetTrussProfile(TrussType type)
        {
            return type switch
            {
                TrussType.Common => new CommonTrussProfile(),
                TrussType.Gambrel => new GambrelTrussProfile(),
                TrussType.MonoSlope => new MonoSlopeTrussProfile(),
                TrussType.Scissor => new ScissorTrussProfile(),
                TrussType.Monitor => new MonitorTrussProfile(),
                TrussType.Attic => new AtticTrussProfile(),
                _ => throw new ArgumentException($"Unknown truss type: {type}")
            };
        }
    }
}
