// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui.Equipment;

public sealed partial class MechGrabberUi : UIFragment
{
    private MechGrabberUiFragment? _fragment;
    private BoundUserInterface? _userInterface;
    private EntityUid? _fragmentOwner;

    public override Control GetUIFragmentRoot()
    {
        return EnsureFragment();
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        EnsureFragment();
        _userInterface = userInterface;
        _fragmentOwner = fragmentOwner;

        if (fragmentOwner == null)
            return;
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGrabberUiState grabberState)
            return;

        _fragment?.UpdateContents(grabberState);
    }

    private MechGrabberUiFragment EnsureFragment()
    {
        if (_fragment is { Disposed: false })
            return _fragment;

        _fragment = new MechGrabberUiFragment();
        _fragment.OnEjectAction += OnEjectAction;
        return _fragment;
    }

    private void OnEjectAction(EntityUid target)
    {
        if (_userInterface == null || _fragmentOwner == null)
            return;

        var entManager = IoCManager.Resolve<IEntityManager>();
        _userInterface.SendMessage(new MechGrabberEjectMessage(entManager.GetNetEntity(_fragmentOwner.Value), entManager.GetNetEntity(target)));
    }
}
