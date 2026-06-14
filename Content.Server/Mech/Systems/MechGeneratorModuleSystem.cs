// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Server.Power.Generator;
using Content.Shared.Materials;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.Module.Components;
using Content.Shared.Power.Generator;

namespace Content.Server.Mech.Systems;

public sealed class MechGeneratorModuleSystem : EntitySystem
{
    [Dependency] private readonly GeneratorSystem _generator = default!;
    [Dependency] private readonly SharedMaterialStorageSystem _materialStorage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechGeneratorModuleComponent, MechEquipmentUiMessageRelayEvent>(OnMechGeneratorMessage);
        SubscribeLocalEvent<MechGeneratorModuleComponent, MechEquipmentUiStateReadyEvent>(OnUiStateReady);
    }

    private void OnMechGeneratorMessage(Entity<MechGeneratorModuleComponent> ent, ref MechEquipmentUiMessageRelayEvent args)
    {
        if (args.Message is not MechGeneratorEjectFuelMessage)
            return;

        if (!TryComp<FuelGeneratorComponent>(ent.Owner, out _))
            return;

        _generator.EmptyGenerator(ent.Owner);
    }

    private void OnUiStateReady(Entity<MechGeneratorModuleComponent> ent, ref MechEquipmentUiStateReadyEvent args)
    {
        var state = new MechGeneratorUiState();

        if (TryComp<MechEnergyAccumulatorComponent>(ent.Owner, out var telemetry))
        {
            state.ChargeCurrent = telemetry.Current;
            state.ChargeMax = telemetry.Max;
        }

        if (ent.Comp.GenerationType == MechGenerationType.FuelGenerator &&
            TryComp<SolidFuelGeneratorAdapterComponent>(ent.Owner, out var solid))
        {
            var amount = _materialStorage.GetMaterialAmount(ent.Owner, solid.FuelMaterial);
            amount += (int) MathF.Floor(solid.FractionalMaterial);

            state.HasFuel = true;
            state.FuelName = solid.FuelMaterial;
            state.FuelAmount = amount;

            if (TryComp<MaterialStorageComponent>(ent.Owner, out var storage))
                state.FuelCapacity = storage.StorageLimit ?? 0;
        }

        args.States[GetNetEntity(ent.Owner)] = state;
    }
}
