// Все модификации и наработки в ss14-wega под тегом Corvax-Wega и директориях _Wega лицензированы под GNU GPL v3.
// https://github.com/corvax-team/ss14-wega/blob/master/LICENSE.TXT

using System.Linq;
using Content.Shared._Goobstation.Weapons.Ranged;
using Content.Shared._Lavaland.Weapons.Marker;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Projectiles;
using Content.Shared.Tag;
using Content.Shared.Weapons.Marker;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared._Wega.Lavaland.Upgrades;

public sealed class CrusherUpgradeEffectsSystem : EntitySystem
{
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _threshold = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    private static readonly ProtoId<TagPrototype> Pickaxe = "Pickaxe";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CrusherLegionSkullUpgradeComponent, GunRefreshModifiersEvent>(OnLegionRefresh);
        SubscribeLocalEvent<CrusherLegionSkullUpgradeComponent, RechargeBasicEntityAmmoGetCooldownModifiersEvent>(OnLegionRecharge);
        SubscribeLocalEvent<CrusherLegionSkullUpgradeComponent, GetMeleeAttackRateEvent>(OnLegionMeleeSpeed);
        SubscribeLocalEvent<CrusherGoliathTentacleUpgradeComponent, MarkerAttackAttemptEvent>(OnGoliathMarker);
        SubscribeLocalEvent<CrusherGoliathTentacleUpgradeComponent, MeleeHitEvent>(OnGoliathMelee);
        SubscribeLocalEvent<CrusherAncientGoliathTentacleUpgradeComponent, MarkerAttackAttemptEvent>(OnAncientGoliathMarker);
        SubscribeLocalEvent<CrusherAncientGoliathTentacleUpgradeComponent, MeleeHitEvent>(OnAncientGoliathMelee);
        SubscribeLocalEvent<CrusherWatcherWingUpgradeComponent, GunShotEvent>(OnWatcherShot);
        SubscribeLocalEvent<CrusherMagmaWingUpgradeComponent, AfterMarkerAttackedEvent>(OnMagmaMarker);
        SubscribeLocalEvent<CrusherMagmaWingUpgradeComponent, GunShotEvent>(OnMagmaShot);
        SubscribeLocalEvent<CrusherDemonClawsUpgradeComponent, MeleeHitEvent>(OnDemonMelee);
        SubscribeLocalEvent<CrusherBlasterTubesUpgradeComponent, GunRefreshModifiersEvent>(OnColossusRefresh);

    }

    private void OnLegionRefresh(Entity<CrusherLegionSkullUpgradeComponent> ent, ref GunRefreshModifiersEvent args)
    {
        if (!_tag.HasTag(Transform(ent).ParentUid, Pickaxe))
            args.FireRate *= ent.Comp.FireRateCoefficient;
    }

    private void OnLegionRecharge(Entity<CrusherLegionSkullUpgradeComponent> ent, ref RechargeBasicEntityAmmoGetCooldownModifiersEvent args)
    {
        if (!_tag.HasTag(Transform(ent).ParentUid, Pickaxe))
            args.Multiplier /= ent.Comp.FireRateCoefficient;
    }

    private void OnLegionMeleeSpeed(
        Entity<CrusherLegionSkullUpgradeComponent> ent,
        ref GetMeleeAttackRateEvent args)
    {
        if (_tag.HasTag(Transform(ent).ParentUid, Pickaxe))
            args.Multipliers *= ent.Comp.MeleeAttackRateCoefficient;
    }

    private bool TryGetDamageFraction(EntityUid uid, MobState thresholdState, out float fraction)
    {
        fraction = 0f;
        if (!TryComp<DamageableComponent>(uid, out var damageable)
            || !_threshold.TryGetThresholdForState(uid, thresholdState, out var threshold)
            || threshold.Value <= 0)
            return false;

        fraction = Math.Clamp(damageable.Damage.GetTotal().Float() / threshold.Value.Float(), 0f, 1f);
        return true;
    }

    private void OnGoliathMarker(Entity<CrusherGoliathTentacleUpgradeComponent> ent, ref MarkerAttackAttemptEvent args)
    {
        if (TryGetDamageFraction(args.User, ent.Comp.TargetState, out var fraction))
            args.DamageModifier += ent.Comp.MaxCoefficient * fraction;
    }

    private void OnGoliathMelee(Entity<CrusherGoliathTentacleUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        if (TryGetDamageFraction(args.User, ent.Comp.TargetState, out var fraction))
            args.BonusDamage += args.BaseDamage * (ent.Comp.MaxCoefficient * fraction);
    }

    private bool IsAncientBonusTarget(EntityUid target, CrusherAncientGoliathTentacleUpgradeComponent component)
    {
        if (!TryComp<DamageableComponent>(target, out var damageable)
            || !_threshold.TryGetThresholdForState(target, MobState.Dead, out var threshold))
            return false;

        return damageable.Damage.GetTotal() <= threshold * (1f - component.HealthThreshold);
    }

    private void OnAncientGoliathMarker(Entity<CrusherAncientGoliathTentacleUpgradeComponent> ent, ref MarkerAttackAttemptEvent args)
    {
        if (IsAncientBonusTarget(args.Target, ent.Comp))
            args.DamageModifier += ent.Comp.Coefficient;
    }

    private void OnAncientGoliathMelee(Entity<CrusherAncientGoliathTentacleUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Any(target => IsAncientBonusTarget(target, ent.Comp)))
            args.BonusDamage += args.BaseDamage * ent.Comp.Coefficient;
    }

    private void OnWatcherShot(Entity<CrusherWatcherWingUpgradeComponent> ent, ref GunShotEvent args)
    {
        foreach (var (ammo, _) in args.Ammo)
        {
            if (ammo is not { } projectile || !HasComp<ProjectileComponent>(projectile))
                continue;

            EnsureComp<ProjectileTimerResetUpgradeComponent>(projectile).CooldownIncrease = ent.Comp.CooldownIncrease;
        }
    }

    private void OnMagmaMarker(Entity<CrusherMagmaWingUpgradeComponent> ent, ref AfterMarkerAttackedEvent args)
        => ent.Comp.Active = true;

    private void OnMagmaShot(Entity<CrusherMagmaWingUpgradeComponent> ent, ref GunShotEvent args)
        => ApplyNextShotDamage(ent.Comp.Damage, args.Ammo, ref ent.Comp.Active);

    private void OnDemonMelee(Entity<CrusherDemonClawsUpgradeComponent> ent, ref MeleeHitEvent args)
    {
        if (!args.HitEntities.Any(target => HasComp<MobStateComponent>(target) && !_mobState.IsDead(target)))
            return;

        args.BonusDamage += args.BaseDamage * ent.Comp.DamageMultiplier;
        if (!ent.Comp.MeleeHeal.Empty)
            _damage.TryChangeDamage(args.User, ent.Comp.MeleeHeal, true, false, origin: args.Weapon);
    }

    private void OnColossusRefresh(Entity<CrusherBlasterTubesUpgradeComponent> ent, ref GunRefreshModifiersEvent args)
        => args.ProjectileSpeed *= ent.Comp.ProjectileSpeedCoefficient;

    private void ApplyNextShotDamage(
        DamageSpecifier damage,
        List<(EntityUid? Uid, IShootable Shootable)> ammo,
        ref bool active)
    {
        if (!active)
            return;

        foreach (var (uid, _) in ammo)
        {
            if (uid is not { } projectile || !TryComp<ProjectileComponent>(projectile, out var projectileComponent))
                continue;

            projectileComponent.Damage += damage;
            Dirty(projectile, projectileComponent);
            active = false;
            break;
        }
    }

}
