// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega
// лицензированы под GNU GPL v3:
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Shared._Wega.Lavaland.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Tools.Systems;
using Robust.Server.GameObjects;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Lavaland.Systems;

public sealed class FloraSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FloraComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<FloraComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<FloraComponent, InteractUsingEvent>(OnInteractUsing);
        SubscribeLocalEvent<FloraComponent, FloraHarvestDoAfterEvent>(OnHarvestDoAfter);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<FloraComponent>();
        while (query.MoveNext(out var uid, out var component))
        {
            if (!component.IsGrown && _timing.CurTime >= component.NextGrowthTime)
                SetGrown(uid, component, true);
        }
    }

    private void OnMapInit(Entity<FloraComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.IsGrown && ent.Comp.NextGrowthTime == TimeSpan.Zero)
            ScheduleGrowth(ent.Comp);

        UpdateAppearance(ent);
    }

    private void OnInteractHand(Entity<FloraComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled || !ent.Comp.IsGrown || ent.Comp.SpecialTool != null)
            return;

        args.Handled = TryStartHarvest(ent, args.User, args.User);
    }

    private void OnInteractUsing(Entity<FloraComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !ent.Comp.IsGrown)
            return;

        if (ent.Comp.SpecialTool != null && !_tool.HasQuality(args.Used, ent.Comp.SpecialTool.Value))
            return;

        args.Handled = TryStartHarvest(ent, args.User, args.Used);
    }

    private bool TryStartHarvest(Entity<FloraComponent> ent, EntityUid user, EntityUid used)
    {
        var doAfterArgs = new DoAfterArgs(
            EntityManager,
            user,
            ent.Comp.HarvestDuration,
            new FloraHarvestDoAfterEvent(),
            ent.Owner,
            ent.Owner,
            used)
        {
            BreakOnDamage = true,
            BreakOnHandChange = true,
            BreakOnMove = true,
            NeedHand = false,
        };

        return _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnHarvestDoAfter(Entity<FloraComponent> ent, ref FloraHarvestDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !ent.Comp.IsGrown)
            return;

        if (ent.Comp.SpecialTool != null &&
            (args.Used == null || !_tool.HasQuality(args.Used.Value, ent.Comp.SpecialTool.Value)))
        {
            return;
        }

        var coordinates = Transform(ent).Coordinates;
        var minYield = Math.Max(0, Math.Min(ent.Comp.MinYield, ent.Comp.MaxYield));
        var maxYield = Math.Max(0, Math.Max(ent.Comp.MinYield, ent.Comp.MaxYield));
        var yield = minYield == maxYield
            ? minYield
            : _random.Next(minYield, maxYield == int.MaxValue ? int.MaxValue : maxYield + 1);
        for (var i = 0; i < yield; i++)
        {
            Spawn(ent.Comp.HarvestPrototype, coordinates);
        }

        _audio.PlayPvs(ent.Comp.HarvestSound, ent);
        SetGrown(ent, ent.Comp, false);
        args.Handled = true;
    }

    private void SetGrown(EntityUid uid, FloraComponent component, bool grown)
    {
        component.IsGrown = grown;
        if (!grown)
            ScheduleGrowth(component);

        UpdateAppearance((uid, component));
    }

    private void ScheduleGrowth(FloraComponent component)
    {
        var minimum = (float) Math.Max(0,
            Math.Min(component.MinGrowthTime.TotalSeconds, component.MaxGrowthTime.TotalSeconds));
        var maximum = (float) Math.Max(0,
            Math.Max(component.MinGrowthTime.TotalSeconds, component.MaxGrowthTime.TotalSeconds));
        var seconds = minimum >= maximum
            ? minimum
            : _random.NextFloat(minimum, maximum);
        component.NextGrowthTime = _timing.CurTime + TimeSpan.FromSeconds(seconds);
    }

    private void UpdateAppearance(Entity<FloraComponent> ent)
    {
        if (TryComp<PointLightComponent>(ent, out var light))
            _light.SetEnabled(ent, ent.Comp.IsGrown, light);

        if (TryComp<AppearanceComponent>(ent, out var appearance))
        {
            var state = ent.Comp.IsGrown ? FloraState.Grown : FloraState.Harvested;
            _appearance.SetData(ent, FloraVisuals.State, state, appearance);
        }
    }
}
