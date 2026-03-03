using System.Collections.Generic;
using PoleBarnGenerator.Models;

namespace PoleBarnGenerator.UI
{
    /// <summary>
    /// Quick-add presets for common door and window configurations.
    /// </summary>
    public static class OpeningPresets
    {
        public static readonly Dictionary<string, (string Label, DoorOpening Door)> DoorPresets = new()
        {
            ["overhead-16x14"] = ("16'×14' Overhead", new DoorOpening
            {
                Type = DoorType.Overhead, Width = 16, Height = 14,
                TrackType = TrackType.StandardLift, Wall = WallSide.Front, CenterOffset = 15
            }),
            ["overhead-10x10"] = ("10'×10' Overhead", new DoorOpening
            {
                Type = DoorType.Overhead, Width = 10, Height = 10,
                TrackType = TrackType.StandardLift, Wall = WallSide.Front, CenterOffset = 15
            }),
            ["overhead-12x12"] = ("12'×12' Overhead", new DoorOpening
            {
                Type = DoorType.Overhead, Width = 12, Height = 12,
                TrackType = TrackType.StandardLift, Wall = WallSide.Front, CenterOffset = 15
            }),
            ["walk-3x6.8"] = ("3'×6'-8\" Walk", new DoorOpening
            {
                Type = DoorType.Walk, Width = 3, Height = 6.67,
                SwingDirection = SwingDirection.Out, HandingDirection = HandingDirection.Right,
                Wall = WallSide.Left, CenterOffset = 5
            }),
            ["walk-3x7"] = ("3'×7' Walk", new DoorOpening
            {
                Type = DoorType.Walk, Width = 3, Height = 7,
                SwingDirection = SwingDirection.Out, HandingDirection = HandingDirection.Right,
                Wall = WallSide.Left, CenterOffset = 5
            }),
            ["sliding-12x10"] = ("12'×10' Sliding", new DoorOpening
            {
                Type = DoorType.Sliding, Width = 12, Height = 10,
                Wall = WallSide.Left, CenterOffset = 20
            }),
            ["sliding-10x8"] = ("10'×8' Sliding", new DoorOpening
            {
                Type = DoorType.Sliding, Width = 10, Height = 8,
                Wall = WallSide.Left, CenterOffset = 20
            }),
            ["dutch-4x7"] = ("4'×7' Dutch", new DoorOpening
            {
                Type = DoorType.Dutch, Width = 4, Height = 7, SplitHeight = 3.5,
                SwingDirection = SwingDirection.Out, HandingDirection = HandingDirection.Left,
                Wall = WallSide.Left, CenterOffset = 5
            }),
            ["double-6x7"] = ("6'×7' Double", new DoorOpening
            {
                Type = DoorType.Double, Width = 6, Height = 7,
                SwingDirection = SwingDirection.Out,
                Wall = WallSide.Front, CenterOffset = 15
            }),
            ["overhead-9x8"] = ("9'×8' Overhead (Res.)", new DoorOpening
            {
                Type = DoorType.Overhead, Width = 9, Height = 8,
                TrackType = TrackType.StandardLift, Wall = WallSide.Front, CenterOffset = 15
            }),
        };

        public static readonly Dictionary<string, (string Label, WindowOpening Window)> WindowPresets = new()
        {
            ["single-hung-3x4"] = ("3'×4' Single Hung", new WindowOpening
            {
                Type = WindowType.SingleHung, Width = 3, Height = 4, SillHeight = 4,
                Wall = WallSide.Left, CenterOffset = 10
            }),
            ["fixed-4x4"] = ("4'×4' Fixed", new WindowOpening
            {
                Type = WindowType.Fixed, Width = 4, Height = 4, SillHeight = 4,
                Wall = WallSide.Left, CenterOffset = 10
            }),
            ["barn-sash-3x3"] = ("3'×3' Barn Sash", new WindowOpening
            {
                Type = WindowType.BarnSash, Width = 3, Height = 3, SillHeight = 4,
                Wall = WallSide.Left, CenterOffset = 10
            }),
            ["awning-2x3"] = ("2'×3' Awning", new WindowOpening
            {
                Type = WindowType.Awning, Width = 2, Height = 3, SillHeight = 5,
                Wall = WallSide.Left, CenterOffset = 10
            }),
            ["sliding-4x3"] = ("4'×3' Sliding", new WindowOpening
            {
                Type = WindowType.Sliding, Width = 4, Height = 3, SillHeight = 4,
                Wall = WallSide.Left, CenterOffset = 10
            }),
            ["casement-2x4"] = ("2'×4' Casement", new WindowOpening
            {
                Type = WindowType.Casement, Width = 2, Height = 4, SillHeight = 4,
                Wall = WallSide.Left, CenterOffset = 10
            }),
            ["fixed-6x4"] = ("6'×4' Picture Fixed", new WindowOpening
            {
                Type = WindowType.Fixed, Width = 6, Height = 4, SillHeight = 4,
                HasGrid = true, GridPattern = GridPattern.Colonial,
                Wall = WallSide.Front, CenterOffset = 15
            }),
        };

        public static DoorOpening CloneDoor(DoorOpening source)
        {
            return new DoorOpening
            {
                Wall = source.Wall,
                Type = source.Type,
                Width = source.Width,
                Height = source.Height,
                CenterOffset = source.CenterOffset,
                SwingDirection = source.SwingDirection,
                HandingDirection = source.HandingDirection,
                TrackType = source.TrackType,
                HasLite = source.HasLite,
                SplitHeight = source.SplitHeight
            };
        }

        public static WindowOpening CloneWindow(WindowOpening source)
        {
            return new WindowOpening
            {
                Wall = source.Wall,
                Type = source.Type,
                Width = source.Width,
                Height = source.Height,
                SillHeight = source.SillHeight,
                CenterOffset = source.CenterOffset,
                HasGrid = source.HasGrid,
                GridPattern = source.GridPattern
            };
        }
    }
}
