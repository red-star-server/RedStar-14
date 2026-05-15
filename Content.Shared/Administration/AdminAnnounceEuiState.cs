// SPDX-FileCopyrightText: 2021 moonheart08 <moonheart08@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 Veritius <veritiusgaming@gmail.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2022 metalgearsloth <comedian_vs_clown@hotmail.com>
// SPDX-FileCopyrightText: 2022 wrexbe <81056464+wrexbe@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 Leon Friedrich <60421075+ElectroJr@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using Content.Shared.Eui;
using Robust.Shared.Serialization;

namespace Content.Shared.Administration
{
    // RS14-start
    public static class AdminAnnounceDefaults
    {
        public const string DefaultColorHex = "#1d8bad";
        public const string ServerColorHex = "#f0973d";
        public const string DefaultSoundPath = "/Audio/Announcements/announce.ogg";

        public static string GetDefaultColorHex(AdminAnnounceType type)
        {
            return type == AdminAnnounceType.Server
                ? ServerColorHex
                : DefaultColorHex;
        }
    }
    // RS14-end
    public enum AdminAnnounceType
    {
        Station,
        Server,
    }

    [Serializable, NetSerializable]
    public sealed class AdminAnnounceEuiState : EuiStateBase
    {
    }

    public static class AdminAnnounceEuiMsg
    {
        [Serializable, NetSerializable]
        public sealed class DoAnnounce : EuiMessageBase
        {
            public bool CloseAfter;
            public string Announcer = default!;
            public string Announcement = default!;
            public AdminAnnounceType AnnounceType;
            public string Voice = default!; // CorvaxGoob-TTS
            // RS14-start
            public bool Global = true;
            public string ColorHex = AdminAnnounceDefaults.DefaultColorHex;
            public string SoundPath = AdminAnnounceDefaults.DefaultSoundPath;
            public string Sender = "";
        }
    }

    public static class AdminAnnounceHelpers
    {
        public static string NormalizeText(string? value) => value?.Trim() ?? string.Empty;

        public static string NormalizeSoundPath(string? value)
        {
            var path = NormalizeText(value);
            return IsValidResourcePath(path) ? path : string.Empty;
        }

        public static string GetValidatedColorHex(AdminAnnounceType type, string? value)
        {
            var normalized = NormalizeText(value);
            if (Color.TryFromHex(normalized) is { } color)
                return color.ToHexNoAlpha();

            return AdminAnnounceDefaults.GetDefaultColorHex(type);
        }

        public static Color GetColor(AdminAnnounceType type, string? value)
        {
            var normalized = NormalizeText(value);
            if (Color.TryFromHex(normalized) is { } color)
                return color;

            return Color.FromHex(AdminAnnounceDefaults.GetDefaultColorHex(type));
        }

        public static bool IsValidResourcePath(string? value)
        {
            var path = NormalizeText(value);
            return path.StartsWith('/') && !path.Contains("..") && !path.Contains('\\');
        }

        public static string FormatAnnouncement(string announcement, string? sender)
        {
            var trimmedSender = NormalizeText(sender);
            if (string.IsNullOrWhiteSpace(trimmedSender))
                return announcement;

            return $"{announcement}\n{Loc.GetString("admin-announce-sent-by")} {trimmedSender}";
        }
        // RS14-end
    }
}