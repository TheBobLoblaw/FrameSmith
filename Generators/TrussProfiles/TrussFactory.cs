using System;
using PoleBarnGenerator.Models;

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
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported truss type.")
            };
        }
    }
}
