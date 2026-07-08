using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class HydrogenFireReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (mixture.Temperature > 20f && mixture.GetMoles(Gas.HyperNoblium) >= 5f)
            return ReactionResult.NoReaction;

        var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
        var temperature = mixture.Temperature;
        var location = holder as TileAtmosphere;
        mixture.ReactionResults[(byte) GasReaction.Fire] = 0f;
        var initialHydrogen = mixture.GetMoles(Gas.Hydrogen);
        var initialOxygen = mixture.GetMoles(Gas.Oxygen);

        float burnedFuel;
        if (initialOxygen < initialHydrogen ||
            Atmospherics.MinimumHydrogenOxyburnEnergy > temperature * oldHeatCapacity * heatScale)
        {
            burnedFuel = initialOxygen / Atmospherics.HydrogenBurnOxyFactor;
            if (burnedFuel > initialHydrogen)
                burnedFuel = initialHydrogen;
        }
        else
        {
            burnedFuel = Math.Min(initialHydrogen, initialOxygen / Atmospherics.TritiumBurnFuelRatio) /
                         Atmospherics.HydrogenBurnH2Factor;
        }

        if (burnedFuel <= 0f)
            return ReactionResult.NoReaction;

        var oxygenConsumed = burnedFuel / Atmospherics.TritiumBurnFuelRatio;
        if (initialHydrogen - burnedFuel < 0f || initialOxygen - oxygenConsumed < 0f)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Hydrogen, -burnedFuel);
        mixture.AdjustMoles(Gas.Oxygen, -oxygenConsumed);
        mixture.AdjustMoles(Gas.WaterVapor, burnedFuel);
        mixture.ReactionResults[(byte) GasReaction.Fire] += burnedFuel;

        var energyReleased = Atmospherics.FireHydrogenEnergyReleased * burnedFuel / heatScale;
        if (energyReleased > 0f)
        {
            var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
            if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;
        }

        if (location != null)
        {
            temperature = mixture.Temperature;
            if (temperature > Atmospherics.FireMinimumTemperatureToExist)
                atmosphereSystem.HotspotExpose(location, temperature, mixture.Volume);
        }

        return mixture.ReactionResults[(byte) GasReaction.Fire] != 0f
            ? ReactionResult.Reacting
            : ReactionResult.NoReaction;
    }
}
