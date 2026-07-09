// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
// SPDX-FileCopyrightText: 2026 ThereDrD <88589686+theredrd0@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using DrawDepth = Content.Shared.DrawDepth.DrawDepth;

namespace Content.Client._Orion.Lighting.Shaders;

public sealed class LightingOverlay : Overlay
{
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceEntities;

    private static readonly ProtoId<ShaderPrototype> LightingOverlayShader = "LightingOverlay";
    public readonly List<PreparedLightOverlay> PreparedLights = [];
    public bool Enabled;

    public LightingOverlay(IPrototypeManager prototypeManager)
    {
        _shader = prototypeManager.Index(LightingOverlayShader).InstanceUnique();
        ZIndex = (int) DrawDepth.Overdoors;
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        return Enabled && PreparedLights.Count > 0;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var handle = args.WorldHandle;
        var bounds = args.WorldAABB.Enlarged(5f);

        handle.UseShader(_shader);

        foreach (var light in PreparedLights)
        {
            if (light.MapId != args.MapId)
                continue;

            if (!bounds.Contains(light.WorldPosition))
                continue;

            _shader.SetParameter("overlay_strength", light.Strength);

            handle.SetTransform(light.WorldMatrix);
            handle.DrawTexture(light.Texture, light.Offset, light.Color);
        }

        handle.UseShader(null);
        handle.SetTransform(Matrix3x2.Identity);
    }
}

public readonly record struct PreparedLightOverlay(
    MapId MapId,
    Vector2 WorldPosition,
    Matrix3x2 WorldMatrix,
    Texture Texture,
    Vector2 Offset,
    Color Color,
    float Strength);
