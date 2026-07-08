using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ZaukerDecompositionReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (mixture.Temperature > 20f && mixture.GetMoles(Gas.HyperNoblium) >= 5f)
            return ReactionResult.NoReaction;

        var initZauker = mixture.GetMoles(Gas.Zauker);
        var initNitrogen = mixture.GetMoles(Gas.Nitrogen);
        var burnedFuel = Math.Min(Atmospherics.ZaukerDecompositionMaxRate, Math.Min(initNitrogen, initZauker));

        if (burnedFuel <= 0f || initZauker - burnedFuel < 0f)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Zauker, -burnedFuel);
        mixture.AdjustMoles(Gas.Oxygen, burnedFuel * 0.3f);
        mixture.AdjustMoles(Gas.Nitrogen, burnedFuel * 0.7f);

        var energyReleased = burnedFuel * Atmospherics.ZaukerDecompositionEnergy;
        var heatCap = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (heatCap > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = Math.Max((mixture.Temperature * heatCap + energyReleased) / heatCap, Atmospherics.TCMB);

        return ReactionResult.Reacting;
    }
}
