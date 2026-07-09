// SPDX-FileCopyrightText: 2026 PuroSlavKing <puroslavking@yahoo.com>
// SPDX-FileCopyrightText: 2026 ThereDrD <88589686+theredrd0@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Numerics;
using Content.Shared._Orion.CCVar;
using Content.Shared._Orion.Lighting.Shaders;
using Content.Shared.Light.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._Orion.Lighting.Shaders;

public sealed partial class LightingOverlaySystem : EntitySystem
{
    [Dependency] private IOverlayManager _overlayManager = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private SpriteSystem _sprite = default!;
    [Dependency] private TransformSystem _transform = default!;

    private LightingOverlay? _lightingOverlay;
    private EntityQuery<TransformComponent> _xformQuery;
    private Action<bool>? _lightCvarChanged;
    private readonly Dictionary<SpriteSpecifier, Texture> _textureCache = [];

    public override void Initialize()
    {
        base.Initialize();

        _xformQuery = GetEntityQuery<TransformComponent>();
        var overlay = new LightingOverlay(_prototypeManager);
        _lightingOverlay = overlay;
        _lightCvarChanged = value =>
        {
            overlay.Enabled = value;
            if (!value)
                overlay.PreparedLights.Clear();
        };

        _cfg.OnValueChanged(OrionCCVars.EnableLightsGlowing, _lightCvarChanged, true);
        _overlayManager.AddOverlay(overlay);
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_lightingOverlay == null || !_lightingOverlay.Enabled)
            return;

        var lights = _lightingOverlay.PreparedLights;
        lights.Clear();

        var query = EntityQueryEnumerator<LightingOverlayComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var component, out var pointLight, out var xform))
        {
            if (component.Enabled is false || !pointLight.Enabled)
                continue;

            if (HasComp<RgbLightControllerComponent>(uid))
                continue;

            var texture = GetTexture(component.Sprite);
            var offset = new Vector2(
                component.OffsetX - texture.Width / 2f / EyeManager.PixelsPerMeter,
                component.OffsetY - texture.Height / 2f / EyeManager.PixelsPerMeter);
            var (worldPosition, _, worldMatrix) = _transform.GetWorldPositionRotationMatrix(xform, _xformQuery);

            lights.Add(new PreparedLightOverlay(
                xform.MapID,
                worldPosition,
                worldMatrix,
                texture,
                offset,
                component.Color ?? pointLight.Color,
                component.Strength));
        }
    }

    private Texture GetTexture(SpriteSpecifier sprite)
    {
        if (_textureCache.TryGetValue(sprite, out var texture))
            return texture;

        texture = _sprite.Frame0(sprite);
        _textureCache[sprite] = texture;
        return texture;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        if (_lightCvarChanged != null)
        {
            _cfg.UnsubValueChanged(OrionCCVars.EnableLightsGlowing, _lightCvarChanged);
            _lightCvarChanged = null;
        }

        if (_lightingOverlay == null)
            return;

        _overlayManager.RemoveOverlay(_lightingOverlay);
        _lightingOverlay.Dispose();
        _lightingOverlay = null;
    }
}
