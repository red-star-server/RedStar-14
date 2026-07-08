using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ProtoNitrateBZConversionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (mixture.Temperature > 20f && mixture.GetMoles(Gas.HyperNoblium) >= 5f)
            return ReactionResult.NoReaction;

        var initProtoNitrate = mixture.GetMoles(Gas.ProtoNitrate);
        var initBZ = mixture.GetMoles(Gas.BZ);
        var consumedAmount = Math.Min(
            mixture.Temperature / 2240f * initBZ * initProtoNitrate / (initBZ + initProtoNitrate),
            Math.Min(initBZ, initProtoNitrate));

        if (consumedAmount <= 0f || initBZ - consumedAmount < 0f)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.BZ, -consumedAmount);
        mixture.AdjustMoles(Gas.Nitrogen, consumedAmount * 0.4f);
        mixture.AdjustMoles(Gas.Helium, consumedAmount * 1.6f);
        mixture.AdjustMoles(Gas.Plasma, consumedAmount * 0.8f);

        var energyReleased = consumedAmount * Atmospherics.ProtoNitrateBZConversionEnergy;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }
}
