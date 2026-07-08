using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ZaukerProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (mixture.Temperature > 20f && mixture.GetMoles(Gas.HyperNoblium) >= 5f)
            return ReactionResult.NoReaction;

        var initHyperNoblium = mixture.GetMoles(Gas.HyperNoblium);
        var initNitrium = mixture.GetMoles(Gas.Nitrium);
        var heatEfficiency = Math.Min(
            mixture.Temperature * Atmospherics.ZaukerTemperatureScale,
            Math.Min(initHyperNoblium * 100f, initNitrium * 2f));

        if (heatEfficiency <= 0f ||
            initHyperNoblium - heatEfficiency * 0.01f < 0f ||
            initNitrium - heatEfficiency * 0.5f < 0f)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.HyperNoblium, -heatEfficiency * 0.01f);
        mixture.AdjustMoles(Gas.Nitrium, -heatEfficiency * 0.5f);
        mixture.AdjustMoles(Gas.Zauker, heatEfficiency * 0.5f);

        var energyConsumed = heatEfficiency * Atmospherics.ZaukerProductionEnergy / heatScale;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap - energyConsumed) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }
}
