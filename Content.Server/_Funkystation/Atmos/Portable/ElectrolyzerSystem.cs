using Content.Server.Atmos.EntitySystems;
using Content.Server.Atmos.Piping.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Server.Stack;
using Content.Shared._Funkystation.Atmos.Visuals;
using Content.Shared.Atmos;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.DeviceLinking;
using Content.Shared.DeviceLinking.Events;
using Content.Shared.Hands.Components;
using Content.Shared.Interaction;
using Content.Shared.Stacks;
using Content.Shared.Tag;
using Robust.Server.Audio;
using Robust.Shared.Audio;

namespace Content.Server._Funkystation.Atmos.Portable;

public sealed partial class ElectrolyzerSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphereSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly GasTileOverlaySystem _gasOverlaySystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly StackSystem _stackSystem = default!;
    [Dependency] private readonly HandsSystem _handsSystem = default!;
    [Dependency] private readonly TagSystem _tagSystem = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    private const float WorkingPower = 2f;
    private const float PowerEfficiency = 1f;
    private const string PlasmaTag = "SheetPlasma";
    private const string UraniumTag = "SheetUranium";
    private const string FuelSlot = "fuel";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ElectrolyzerComponent, SignalReceivedEvent>(OnSignalReceived);
        SubscribeLocalEvent<ElectrolyzerComponent, AtmosDeviceUpdateEvent>(OnDeviceUpdated);
        SubscribeLocalEvent<ElectrolyzerComponent, ActivateInWorldEvent>(OnActivate);
        SubscribeLocalEvent<ElectrolyzerComponent, InteractUsingEvent>(OnInteractUsingFuel);
        SubscribeLocalEvent<ElectrolyzerComponent, AnchorStateChangedEvent>(OnAnchorChanged);
    }

    private void OnSignalReceived(EntityUid uid, ElectrolyzerComponent comp, SignalReceivedEvent args)
    {
        if (!TryComp<DeviceLinkSinkComponent>(uid, out _))
            return;

        bool? newState = args.Port switch
        {
            "On" => true,
            "Off" => false,
            "Toggle" => !comp.IsPowered,
            _ => null
        };

        if (newState == null || newState == comp.IsPowered)
            return;

        if (newState.Value)
        {
            TryTurnOn(uid, comp);
        }
        else
        {
            comp.IsPowered = false;
            UpdateAppearance(uid, comp);
        }
    }

    private void OnActivate(EntityUid uid, ElectrolyzerComponent comp, ActivateInWorldEvent args)
    {
        if (args.Handled)
            return;

        if (comp.IsPowered)
        {
            comp.IsPowered = false;
            _popup.PopupEntity(Loc.GetString("electrolyzer-turned-off"), uid, args.User);
            UpdateAppearance(uid, comp);
        }
        else
        {
            TryTurnOn(uid, comp, args.User);
        }

        args.Handled = true;
    }

    private void OnDeviceUpdated(EntityUid uid, ElectrolyzerComponent electrolyzer, ref AtmosDeviceUpdateEvent args)
    {
        if (!Transform(uid).Anchored || !electrolyzer.IsPowered)
            return;

        if (electrolyzer.CurrentFuel <= 0f && !TryConsumeFuel(uid, electrolyzer))
        {
            electrolyzer.IsPowered = false;
            UpdateAppearance(uid, electrolyzer);
            _popup.PopupEntity(Loc.GetString("electrolyzer-no-fuel"), uid);
            return;
        }

        UpdateAppearance(uid, electrolyzer);

        var mixture = _atmosphereSystem.GetContainingMixture(uid, args.Grid, args.Map);
        if (mixture is null)
            return;

        var initH2O = mixture.GetMoles(Gas.WaterVapor);
        var initHyperNob = mixture.GetMoles(Gas.HyperNoblium);
        var initBZ = mixture.GetMoles(Gas.BZ);
        var oldHeatCapacity = _atmosphereSystem.GetHeatCapacity(mixture, true);
        var powerLoad = 100f;
        var activeLoad = (4200f * (3f * WorkingPower) * WorkingPower) / (PowerEfficiency + WorkingPower);

        if (initH2O > 0.05f)
        {
            var maxProportion = 2.5f * MathF.Pow(WorkingPower, 2);
            var proportion = Math.Min(initH2O * 0.5f, maxProportion);
            var temperatureEfficiency = Math.Min(mixture.Temperature / 1123.15f, 1f);

            var h2oRemoved = proportion * 2f;
            var oxyProduced = proportion * temperatureEfficiency;
            var hydrogenProduced = proportion * 2f * temperatureEfficiency;

            mixture.AdjustMoles(Gas.WaterVapor, -h2oRemoved);
            mixture.AdjustMoles(Gas.Oxygen, oxyProduced);
            mixture.AdjustMoles(Gas.Hydrogen, hydrogenProduced);

            var reactionPower = activeLoad * (hydrogenProduced / (maxProportion * 2f));
            powerLoad = Math.Max(reactionPower, powerLoad);
        }

        if (initHyperNob > 0.01f && mixture.Temperature < 150f)
        {
            var maxProportion = 1.5f * MathF.Pow(WorkingPower, 2);
            var proportion = Math.Min(initHyperNob, maxProportion);
            mixture.AdjustMoles(Gas.HyperNoblium, -proportion);
            mixture.AdjustMoles(Gas.AntiNoblium, proportion * 0.5f);

            powerLoad = Math.Max(powerLoad, activeLoad * (proportion / maxProportion));
        }

        if (initBZ > 0.01f)
        {
            var proportion = Math.Min(
                initBZ * (1f - MathF.Exp(-0.5f * mixture.Temperature * WorkingPower / Atmospherics.FireMinimumTemperatureToExist)),
                initBZ);

            mixture.AdjustMoles(Gas.BZ, -proportion);
            mixture.AdjustMoles(Gas.Oxygen, proportion * 0.2f);
            mixture.AdjustMoles(Gas.Halon, proportion * 2f);

            var energyReleased = proportion * Atmospherics.HalonProductionEnergy;
            var newHeatCapacity = _atmosphereSystem.GetHeatCapacity(mixture, true);
            if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
                mixture.Temperature = Math.Max((mixture.Temperature * oldHeatCapacity + energyReleased) / newHeatCapacity, Atmospherics.TCMB);

            powerLoad = Math.Max(powerLoad, activeLoad * Math.Min(proportion / 30f, 1f));
        }

        var finalHeatCapacity = _atmosphereSystem.GetHeatCapacity(mixture, true);
        if (finalHeatCapacity > Atmospherics.MinimumHeatCapacity && finalHeatCapacity != oldHeatCapacity)
            mixture.Temperature = Math.Max(mixture.Temperature * oldHeatCapacity / finalHeatCapacity, Atmospherics.TCMB);

        electrolyzer.CurrentFuel = Math.Max(0f, electrolyzer.CurrentFuel - powerLoad);
        _gasOverlaySystem.UpdateSessions();
    }

    private bool TryConsumeFuel(EntityUid uid, ElectrolyzerComponent electrolyzer)
    {
        if (!_itemSlots.TryGetSlot(uid, FuelSlot, out var slot) ||
            slot.ContainerSlot?.ContainedEntity is not { } fuelEntity ||
            !TryComp<StackComponent>(fuelEntity, out var stack) ||
            stack.Count <= 0)
        {
            return false;
        }

        var fuelPerSheet = 0f;
        if (_tagSystem.HasTag(fuelEntity, PlasmaTag))
            fuelPerSheet = electrolyzer.PlasmaFuelConversion;
        else if (_tagSystem.HasTag(fuelEntity, UraniumTag))
            fuelPerSheet = electrolyzer.UraniumFuelConversion;
        else
            return false;

        _stackSystem.SetCount(fuelEntity, stack.Count - 1, stack);
        electrolyzer.CurrentFuel = fuelPerSheet;

        if (stack.Count <= 0)
            QueueDel(fuelEntity);

        return true;
    }

    private void OnInteractUsingFuel(EntityUid uid, ElectrolyzerComponent comp, InteractUsingEvent args)
    {
        if (args.Handled || args.Target != uid)
            return;

        if (!_itemSlots.TryGetSlot(uid, FuelSlot, out var slot) || slot.ContainerSlot == null)
            return;

        var heldItem = args.Used;
        var existingItem = slot.ContainerSlot.ContainedEntity;
        var heldIsPlasma = _tagSystem.HasTag(heldItem, PlasmaTag);
        var heldIsUranium = _tagSystem.HasTag(heldItem, UraniumTag);

        if (!heldIsPlasma && !heldIsUranium)
            return;

        args.Handled = true;

        if (existingItem == null)
        {
            if (_itemSlots.TryInsert(uid, FuelSlot, heldItem, args.User))
                _popup.PopupEntity(Loc.GetString("electrolyzer-fuel-inserted"), uid, args.User);

            return;
        }

        var existingIsPlasma = _tagSystem.HasTag(existingItem.Value, PlasmaTag);
        var existingIsUranium = _tagSystem.HasTag(existingItem.Value, UraniumTag);

        if ((heldIsPlasma && existingIsPlasma) || (heldIsUranium && existingIsUranium))
        {
            MergeFuelStacks(uid, heldItem, existingItem.Value, args.User);
            return;
        }

        SwapFuel(uid, heldItem, args.User);
    }

    private void MergeFuelStacks(EntityUid uid, EntityUid heldItem, EntityUid existingItem, EntityUid user)
    {
        if (!TryComp<StackComponent>(heldItem, out var heldStack) ||
            !TryComp<StackComponent>(existingItem, out var existingStack))
        {
            _popup.PopupEntity(Loc.GetString("electrolyzer-cannot-merge-invalid-stack"), uid, user);
            return;
        }

        var maxStack = _stackSystem.GetMaxCount(existingStack);
        var total = existingStack.Count + heldStack.Count;

        if (total > maxStack)
        {
            var toAdd = maxStack - existingStack.Count;
            _stackSystem.SetCount(existingItem, maxStack, existingStack);
            _stackSystem.SetCount(heldItem, heldStack.Count - toAdd, heldStack);
        }
        else
        {
            _stackSystem.SetCount(existingItem, total, existingStack);
            QueueDel(heldItem);
        }
    }

    private void SwapFuel(EntityUid uid, EntityUid heldItem, EntityUid user)
    {
        if (!_itemSlots.TryEject(uid, FuelSlot, user, out var ejected))
            return;

        if (!_itemSlots.TryInsert(uid, FuelSlot, heldItem, user))
            return;

        _popup.PopupEntity(Loc.GetString("electrolyzer-fuel-swapped"), uid, user);

        if (ejected == null || ejected == EntityUid.Invalid || !TryComp<HandsComponent>(user, out var hands))
            return;

        var activeHandId = hands.ActiveHandId;
        if (activeHandId != null)
            _handsSystem.TryPickup(user, ejected.Value, handId: activeHandId, handsComp: hands);
        else
            _handsSystem.PickupOrDrop(user, ejected.Value);
    }

    private void TryTurnOn(EntityUid uid, ElectrolyzerComponent comp, EntityUid? user = null)
    {
        if (comp.IsPowered)
            return;

        if (!Transform(uid).Anchored)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("electrolyzer-must-be-anchored"), uid, user.Value);

            return;
        }

        var hasFuel = comp.CurrentFuel > 0f ||
                      (_itemSlots.TryGetSlot(uid, FuelSlot, out var slot) &&
                       slot.ContainerSlot?.ContainedEntity != null);

        if (!hasFuel)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("electrolyzer-no-fuel"), uid, user.Value);
            else
                _popup.PopupEntity(Loc.GetString("electrolyzer-no-fuel"), uid);

            return;
        }

        comp.IsPowered = true;
        if (user != null)
            _popup.PopupEntity(Loc.GetString("electrolyzer-turned-on"), uid, user.Value);
        else
            _popup.PopupEntity(Loc.GetString("electrolyzer-turned-on"), uid);

        if (comp.OnSound != null)
            _audio.PlayPvs(comp.OnSound, uid, AudioParams.Default.WithVolume(-4f));

        UpdateAppearance(uid, comp);
    }

    private void OnAnchorChanged(EntityUid uid, ElectrolyzerComponent comp, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored || !comp.IsPowered)
            return;

        comp.IsPowered = false;
        UpdateAppearance(uid, comp);
        _popup.PopupEntity(Loc.GetString("electrolyzer-turned-off"), uid);
    }

    private void UpdateAppearance(EntityUid uid, ElectrolyzerComponent comp)
    {
        _appearance.SetData(uid, ElectrolyzerVisuals.State, comp.IsPowered ? ElectrolyzerState.On : ElectrolyzerState.Off);
    }
}
