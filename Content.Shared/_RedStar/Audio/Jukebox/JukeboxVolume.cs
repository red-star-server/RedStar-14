// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Content.Shared._RedStar.Audio.Jukebox;

public static class JukeboxVolume
{
    public const float MinValue = 0f;
    public const float MaxValue = 1f;
    public const float DefaultValue = 0.85f;

    public const float MinDb = -24f;
    public const float MaxDb = 0f;

    public static float Clamp(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
            return DefaultValue;

        return Math.Clamp(value, MinValue, MaxValue);
    }

    public static float ToDb(float value)
    {
        value = Clamp(value);

        if (value <= 0.001f)
            return float.NegativeInfinity;

        return MinDb + (MaxDb - MinDb) * value;
    }
}