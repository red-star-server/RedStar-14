using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Server.NodeContainer.NodeGroups;
using Content.Server.Radiation.Components;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Robust.Shared.Timing;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class PluoxiumRadiationProductionReaction : IGasReactionEffect
{
    private const float RadiationThreshold = 0.01f;
    private static readonly TimeSpan TimerDuration = TimeSpan.FromSeconds(5);

    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var radiationLevel = GetRadiationLevel(holder);
        if (radiationLevel < RadiationThreshold)
            return ReactionResult.NoReaction;

        var initOxygen = mixture.GetMoles(Gas.Oxygen);
        var initCarbonDioxide = mixture.GetMoles(Gas.CarbonDioxide);
        var producedAmount = Math.Min(radiationLevel, Math.Min(initCarbonDioxide, initOxygen * 2f));

        if (producedAmount <= 0f)
            return ReactionResult.NoReaction;

        var co2Removed = producedAmount;
        var oxygenRemoved = producedAmount * 0.5f;
        if (co2Removed > initCarbonDioxide || oxygenRemoved > initOxygen)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.CarbonDioxide, -co2Removed);
        mixture.AdjustMoles(Gas.Oxygen, -oxygenRemoved);
        mixture.AdjustMoles(Gas.Pluoxium, producedAmount);

        var energyReleased = producedAmount * Atmospherics.PluoxiumProductionEnergy / heatScale;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }

    private static float GetRadiationLevel(IGasMixtureHolder? holder)
    {
        if (holder is null)
            return 0f;

        var entityManager = IoCManager.Resolve<IEntityManager>();
        var timing = IoCManager.Resolve<IGameTiming>();

        if (holder is Component component)
            return EnsureReceiver(entityManager, timing, component.Owner);

        if (holder is not PipeNet pipeNet || pipeNet.Nodes.Count == 0)
            return 0f;

        var totalRads = 0f;
        var sampledNodes = 0;
        foreach (var node in pipeNet.Nodes)
        {
            totalRads += EnsureReceiver(entityManager, timing, node.Owner);
            sampledNodes++;
        }

        return sampledNodes == 0 ? 0f : totalRads / sampledNodes;
    }

    private static float EnsureReceiver(IEntityManager entityManager, IGameTiming timing, EntityUid uid)
    {
        if (entityManager.TryGetComponent<RadiationReceiverComponent>(uid, out var existingReceiver))
            return existingReceiver.CurrentRadiation;

        entityManager.EnsureComponent<RadiationReceiverComponent>(uid);
        var timer = entityManager.EnsureComponent<RadiationReceiverTimerComponent>(uid);
        timer.TimerExpiresAt = timing.CurTime + TimerDuration;
        timer.AddedReceiver = true;

        return 0f;
    }
}

[RegisterComponent]
[Access(typeof(PluoxiumRadiationProductionReaction), typeof(RadiationTimerSystem))]
public sealed partial class RadiationReceiverTimerComponent : Component
{
    public TimeSpan TimerExpiresAt { get; set; } = TimeSpan.Zero;

    public bool AddedReceiver { get; set; }
}

public sealed partial class RadiationTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<RadiationReceiverTimerComponent>();
        while (query.MoveNext(out var uid, out var timer))
        {
            if (_timing.CurTime < timer.TimerExpiresAt)
                continue;

            if (timer.AddedReceiver)
                RemComp<RadiationReceiverComponent>(uid);

            RemComp<RadiationReceiverTimerComponent>(uid);
        }
    }
}
