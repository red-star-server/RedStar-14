using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class HyperNobliumProductionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (mixture.Temperature > 20f && mixture.GetMoles(Gas.HyperNoblium) >= 5f)
            return ReactionResult.NoReaction;

        var initNitrogen = mixture.GetMoles(Gas.Nitrogen);
        var initTritium = mixture.GetMoles(Gas.Tritium);
        var initBZ = mixture.GetMoles(Gas.BZ);

        var reductionFactor = Math.Clamp(initTritium / (initTritium + initBZ), 0.001f, 1f);
        var nobliumFormed = Math.Min(
            (initNitrogen + initTritium) * 0.01f,
            Math.Min(initTritium / (5f * reductionFactor), initNitrogen * 0.1f));

        if (nobliumFormed <= 0f ||
            initTritium - 5f * nobliumFormed * reductionFactor < 0f ||
            initNitrogen - 10f * nobliumFormed < 0f)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Tritium, -5f * nobliumFormed * reductionFactor);
        mixture.AdjustMoles(Gas.Nitrogen, -10f * nobliumFormed);
        mixture.AdjustMoles(Gas.HyperNoblium, nobliumFormed);

        var energyReleased = nobliumFormed * (Atmospherics.HyperNobliumProductionEnergy / Math.Max(initBZ, 1f));
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }
}
