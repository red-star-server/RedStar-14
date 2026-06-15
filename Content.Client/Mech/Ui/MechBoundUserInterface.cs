// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 TemporalOroboros <TemporalOroboros@gmail.com>
// SPDX-FileCopyrightText: 2024 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
// SPDX-FileCopyrightText: 2025 Kevin Zheng <kevinz5000@gmail.com>
// SPDX-FileCopyrightText: 2025 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface;
using Content.Shared.Mech;
using Content.Shared.Mech.Module.Components;
using JetBrains.Annotations;
using Robust.Client.Timing;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechBoundUserInterface : BoundUserInterface, IBuiPreTickUpdate
{
    [Dependency] private readonly IClientGameTiming _gameTiming = default!;

    [ViewVariables]
    private MechMenu? _menu;

    private BuiPredictionState? _pred;
    private InputCoalescer<bool> _airtightCoalescer;
    private InputCoalescer<bool> _fanCoalescer;
    private InputCoalescer<bool> _filterCoalescer;

    public MechBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
    }

    protected override void Open()
    {
        base.Open();

        _pred = new BuiPredictionState(this, _gameTiming);

        _menu = this.CreateWindowCenteredLeft<MechMenu>();
        _menu.SetEntity(Owner);
        _menu.SetParentBui(this);

        _menu.OnRemoveEquipmentButtonPressed += uid =>
        {
            _pred.SendMessage(new MechEquipmentRemoveMessage(EntMan.GetNetEntity(uid)));
        };
        _menu.OnRemoveModuleButtonPressed += uid =>
        {
            _pred.SendMessage(new MechModuleRemoveMessage(EntMan.GetNetEntity(uid)));
        };

        // RS14-start
        _menu.OnDnaLockRegister += () => _pred.SendMessage(new MechDnaLockRegisterMessage());
        _menu.OnDnaLockToggle += () => _pred.SendMessage(new MechDnaLockToggleMessage());
        _menu.OnDnaLockReset += () => _pred.SendMessage(new MechDnaLockResetMessage());
        _menu.OnCardLockRegister += () => _pred.SendMessage(new MechCardLockRegisterMessage());
        _menu.OnCardLockToggle += () => _pred.SendMessage(new MechCardLockToggleMessage());
        _menu.OnCardLockReset += () => _pred.SendMessage(new MechCardLockResetMessage());
        _menu.OnCabinPurge += () => _pred.SendMessage(new MechCabinAirMessage());
        _menu.OnAirtightToggle += isAirtight => _airtightCoalescer.Set(isAirtight);
        _menu.OnFanToggle += isActive => _fanCoalescer.Set(isActive);
        _menu.OnFilterToggle += enabled => _filterCoalescer.Set(enabled);
        // RS14-end
    }

    void IBuiPreTickUpdate.PreTickUpdate()
    {
        if (_pred == null)
            return;

        if (_airtightCoalescer.CheckIsModified(out var airtightValue))
            _pred.SendMessage(new MechAirtightMessage(airtightValue));

        if (_fanCoalescer.CheckIsModified(out var fanValue))
            _pred.SendMessage(new MechFanToggleMessage(fanValue));

        if (_filterCoalescer.CheckIsModified(out var filterValue))
            _pred.SendMessage(new MechFilterToggleMessage(filterValue));
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is not MechBoundUiState msg)
            return;

        if (_pred != null)
        {
            foreach (var replayMsg in _pred.MessagesToReplay())
            {
                switch (replayMsg)
                {
                    case MechAirtightMessage airtight:
                        msg.IsAirtight = airtight.IsAirtight;
                        break;
                    case MechFanToggleMessage fanToggle:
                        msg.FanActive = fanToggle.IsActive;
                        msg.FanState = fanToggle.IsActive ? MechFanState.On : MechFanState.Off;
                        break;
                    case MechFilterToggleMessage filterToggle:
                        msg.FilterEnabled = filterToggle.Enabled;
                        break;
                }
            }
        }

        _menu?.UpdateState(msg);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _menu?.Close();
        _menu = null;
    }
}
