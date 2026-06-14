// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Kevin Zheng <kevinz5000@gmail.com>
// SPDX-FileCopyrightText: 2025 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechBoundUserInterface : BoundUserInterface
{
    [ViewVariables]
    private MechMenu? _menu;

    public MechBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _menu = this.CreateWindowCenteredLeft<MechMenu>();
        _menu.SetEntity(Owner);
        _menu.SetParentBui(this);

        _menu.OnRemoveEquipmentButtonPressed += uid =>
        {
            SendMessage(new MechEquipmentRemoveMessage(EntMan.GetNetEntity(uid)));
        };
        _menu.OnRemoveModuleButtonPressed += uid =>
        {
            SendMessage(new MechModuleRemoveMessage(EntMan.GetNetEntity(uid)));
        };

        // RS14-start
        _menu.OnDnaLockRegister += () => SendMessage(new MechDnaLockRegisterMessage());
        _menu.OnDnaLockToggle += () => SendMessage(new MechDnaLockToggleMessage());
        _menu.OnDnaLockReset += () => SendMessage(new MechDnaLockResetMessage());
        _menu.OnCardLockRegister += () => SendMessage(new MechCardLockRegisterMessage());
        _menu.OnCardLockToggle += () => SendMessage(new MechCardLockToggleMessage());
        _menu.OnCardLockReset += () => SendMessage(new MechCardLockResetMessage());
        // RS14-end
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MechBoundUiState msg)
            return;

        _menu?.UpdateMechStats(msg);
        _menu?.UpdateEquipmentView();
        UpdateEquipmentControls(msg);
    }

    public void UpdateEquipmentControls(MechBoundUiState state)
    {
        if (!EntMan.TryGetComponent<MechComponent>(Owner, out var mechComp))
            return;

        foreach (var ent in mechComp.EquipmentContainer.ContainedEntities)
        {
            var ui = GetEquipmentUi(ent);
            if (ui == null)
                continue;
            foreach (var (attached, estate) in state.EquipmentStates)
            {
                if (ent == EntMan.GetEntity(attached))
                    ui.UpdateState(estate);
            }
        }

        // RS14-start
        foreach (var ent in mechComp.ModuleContainer.ContainedEntities)
        {
            var ui = GetEquipmentUi(ent);
            if (ui == null)
                continue;

            foreach (var (attached, estate) in state.EquipmentStates)
            {
                if (ent == EntMan.GetEntity(attached))
                    ui.UpdateState(estate);
            }
        }
        // RS14-end
    }

    public UIFragment? GetEquipmentUi(EntityUid? uid)
    {
        var component = EntMan.GetComponentOrNull<UIFragmentComponent>(uid);
        return component?.Ui;
    }
}
