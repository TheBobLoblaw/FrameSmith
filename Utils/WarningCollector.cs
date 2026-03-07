using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using System;
using System.Collections.Generic;

namespace PoleBarnGenerator.Utils
{
    /// <summary>
    /// Collects non-fatal generation warnings for end-of-run summaries.
    /// </summary>
    public class WarningCollector
    {
        [ThreadStatic]
        private static WarningCollector _current;

        private readonly List<string> _warnings = new();

        public IReadOnlyList<string> Warnings => _warnings;
        public int Count => _warnings.Count;

        public static WarningCollector Current => _current;

        public static IDisposable BeginSession(WarningCollector collector)
        {
            WarningCollector previous = _current;
            _current = collector;
            return new SessionScope(previous);
        }

        public void Add(string warning)
        {
            if (string.IsNullOrWhiteSpace(warning))
            {
                return;
            }

            _warnings.Add(warning);
        }

        public static void Report(Editor ed, WarningCollector collector, string context, Exception ex)
        {
            string warning = $"{context}: {ex.Message}";
            collector?.Add(warning);
            ed?.WriteMessage($"\n  WARNING: {warning}");
        }

        public static void ReportCurrent(string context, Exception ex)
        {
            Editor editor = Application.DocumentManager.MdiActiveDocument?.Editor;
            Report(editor, _current, context, ex);
        }

        private sealed class SessionScope : IDisposable
        {
            private readonly WarningCollector _previous;
            private bool _disposed;

            public SessionScope(WarningCollector previous)
            {
                _previous = previous;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }

                _current = _previous;
                _disposed = true;
            }
        }
    }
}
