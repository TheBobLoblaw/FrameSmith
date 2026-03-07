using System;

namespace PoleBarnGenerator.Generators
{
    /// <summary>
    /// Utility for structural-grid alphabetic labels (A..Z, AA..AZ, ...).
    /// </summary>
    public static class GridLabelGenerator
    {
        public static string GetBayLabel(int index)
        {
            if (index < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            int value = index + 1;
            string label = string.Empty;

            while (value > 0)
            {
                value--;
                label = (char)('A' + (value % 26)) + label;
                value /= 26;
            }

            return label;
        }
    }
}
