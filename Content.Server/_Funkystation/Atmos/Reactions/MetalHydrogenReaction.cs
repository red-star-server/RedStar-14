using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Funkystation.Atmos.Reactions;

[UsedImplicitly]
[DataDefinition]
public sealed partial class MetalHydrogenReaction : IGasReactionEffect
{
    private const float RequiredHydrogen = 300f;
    private const float RequiredBZ = 50f;
    private const float MinPressure = 10000f;
    private const float PressureThreshold = 20000f;
    private const float TemperatureThreshold = 50f;
    private const float BaseRate = 0.10f;

    [DataField]
    public EntProtoId SpawnPrototype = "MetalHydrogen1";

    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (holder is not TileAtmosphere tile || mixture.Pressure < MinPressure)
            return ReactionResult.NoReaction;

        var entityManager = IoCManager.Resolve<IEntityManager>();
        var random = IoCManager.Resolve<IRobustRandom>();
        var mapSystem = entityManager.System<SharedMapSystem>();

        var pressureEfficiency = Math.Min(mixture.Pressure / PressureThreshold, 1f);
        var temperatureEfficiency = Math.Min(TemperatureThreshold / mixture.Temperature, 1f);
        var rate = pressureEfficiency * temperatureEfficiency * BaseRate;

        if (random.NextFloat() > rate)
            return ReactionResult.NoReaction;

        mixture.AdjustMoles(Gas.Hydrogen, -RequiredHydrogen);
        mixture.AdjustMoles(Gas.BZ, -RequiredBZ);

        var tileRef = atmosphereSystem.GetTileRef(tile);
        if (!entityManager.TryGetComponent<MapGridComponent>(tileRef.GridUid, out var grid))
            return ReactionResult.NoReaction;

        var coords = mapSystem.GridTileToLocal(tileRef.GridUid, grid, tileRef.GridIndices);
        entityManager.SpawnEntity(SpawnPrototype, coords);

        return ReactionResult.Reacting;
    }
}
