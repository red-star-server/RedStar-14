// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega
// лицензированы под GNU GPL v3:
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using Content.Server.Chat.Systems;
using Content.Shared._Wega.Lavaland.Components.Artefacts;
using Content.Shared.Chat;
using Content.Shared.CombatMode.Pacification;
using Content.Shared.Damage;
using Content.Shared.Damage.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Visuals;
using Robust.Server.GameObjects;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Server._Wega.Lavaland.Systems.Artefacts;

public sealed class IndependentArtefactSystem : EntitySystem
{
    [Dependency] private readonly AppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RodOfAsclepiusComponent, UseInHandEvent>(OnRodUse);
        SubscribeLocalEvent<RodOfAsclepiusComponent, RodOathDoAfterEvent>(OnRodOath);
        SubscribeLocalEvent<LinkedCubeComponent, MapInitEvent>(OnCubeMapInit);
        SubscribeLocalEvent<LinkedCubeComponent, UseInHandEvent>(OnCubeUse);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<RodOfAsclepiusComponent>();
        while (query.MoveNext(out var uid, out var rod))
        {
            if (rod.BoundTo is not { } bound ||
                rod.NextHealTime > _timing.CurTime ||
                !Exists(bound))
            {
                continue;
            }

            HealRodTargets(bound, rod);
            rod.NextHealTime = _timing.CurTime + rod.HealInterval;
        }
    }

    private void OnRodUse(Entity<RodOfAsclepiusComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.BoundTo != null)
            return;

        var doAfter = new DoAfterArgs(
            EntityManager,
            args.User,
            TimeSpan.FromSeconds(10),
            new RodOathDoAfterEvent(),
            ent,
            target: args.User,
            used: ent)
        {
            BreakOnDamage = true,
            BreakOnMove = true,
            NeedHand = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnRodOath(Entity<RodOfAsclepiusComponent> ent, ref RodOathDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        ent.Comp.BoundTo = args.User;
        EnsureComp<UnremoveableComponent>(ent);
        EnsureComp<PacifiedComponent>(args.User);
        _appearance.SetData(ent, VisualLayers.Enabled, true);
        _chat.TrySendInGameICMessage(
            args.User,
            "Primum non nocere.",
            InGameICChatType.Speak,
            false);
        args.Handled = true;
    }

    private void HealRodTargets(EntityUid owner, RodOfAsclepiusComponent rod)
    {
        if (!_mobState.IsDead(owner))
            _damage.TryChangeDamage(owner, CreateHealing(rod.HealAmount), true, false);

        foreach (var (target, _) in _lookup.GetEntitiesInRange<MobStateComponent>(
                     Transform(owner).Coordinates,
                     rod.HealRadius))
        {
            if (target == owner || _mobState.IsDead(target))
                continue;

            _damage.TryChangeDamage(target, CreateHealing(rod.HealAmount / 3f), true, false);
        }
    }

    private static DamageSpecifier CreateHealing(float amount)
    {
        var healing = new DamageSpecifier();
        foreach (var type in new[] { "Asphyxiation", "Bloodloss", "Blunt", "Slash", "Piercing", "Heat", "Cold", "Poison" })
            healing.DamageDict[type] = -amount;
        return healing;
    }

    private void OnCubeMapInit(Entity<LinkedCubeComponent> ent, ref MapInitEvent args)
    {
        if (!ent.Comp.IsPrimary || ent.Comp.LinkedCube != null)
            return;

        var pair = Spawn(ent.Comp.PairPrototype, Transform(ent).Coordinates);
        ent.Comp.LinkedCube = pair;
        if (TryComp<LinkedCubeComponent>(pair, out var pairComp))
            pairComp.LinkedCube = ent;
    }

    private void OnCubeUse(Entity<LinkedCubeComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled || ent.Comp.LinkedCube is not { } pair || !Exists(pair) || Paused(pair))
            return;

        var pairPosition = _transform.GetMapCoordinates(pair);
        var userPosition = _transform.GetMapCoordinates(args.User);
        if (pairPosition.MapId == userPosition.MapId &&
            (userPosition.Position - pairPosition.Position).Length() < ent.Comp.MinTeleportDistance)
        {
            return;
        }

        _transform.SetMapCoordinates(args.User, pairPosition);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Magic/blink.ogg"), args.User);
        args.Handled = true;
    }
}
