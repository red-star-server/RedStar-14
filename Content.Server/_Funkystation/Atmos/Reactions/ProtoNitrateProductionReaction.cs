using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ProtoNitrateProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (mixture.Temperature > 20f && mixture.GetMoles(Gas.HyperNoblium) >= 5f)
            return ReactionResult.NoReaction;

        var initPluoxium = mixture.GetMoles(Gas.Pluoxium);
        var initHydrogen = mixture.GetMoles(Gas.Hydrogen);
        var heatEfficiency = Math.Min(
            mixture.Temperature * Atmospherics.ProtoNitrateTemperatureScale,
            Math.Min(initPluoxium * 5f, initHydrogen * 0.5f));

        if (heatEfficiency <= 0f ||
            initPluoxium - heatEfficiency * 0.2f < 0f ||
            initHydrogen - heatEfficiency * 2f < 0f)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Hydrogen, -heatEfficiency * 2f);
        mixture.AdjustMoles(Gas.Pluoxium, -heatEfficiency * 0.2f);
        mixture.AdjustMoles(Gas.ProtoNitrate, heatEfficiency * 2.2f);

        var energyReleased = heatEfficiency * Atmospherics.ProtoNitrateProductionEnergy / heatScale;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }
}
