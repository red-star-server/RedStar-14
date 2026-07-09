// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
// SPDX-FileCopyrightText: 2026 ThereDrD <88589686+theredrd0@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.GameStates;
using Robust.Shared.Utility;
using static Robust.Shared.Utility.SpriteSpecifier;

namespace Content.Shared._Orion.Lighting.Shaders;

/// <summary>
/// Adds a client-side glow mask that is composited over matching point lights.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class LightingOverlayComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool? Enabled;

    [DataField]
    public SpriteSpecifier Sprite = new Rsi(new ResPath("_Orion/Effects/LightMasks/128.rsi"), "light_cone");

    [DataField]
    public float OffsetX = 0.0625f;

    [DataField]
    public float OffsetY = 0.5f;

    [DataField]
    public Color? Color;

    [DataField]
    public float Strength = 0.35f;
}
