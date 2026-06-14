// SPDX-FileCopyrightText: 2022 Nemanja <98561806+EmoGarbage404@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 DrSmugleaf <DrSmugleaf@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <39013340+deltanedas@users.noreply.github.com>
// SPDX-FileCopyrightText: 2023 deltanedas <@deltanedas:kde.org>
// SPDX-FileCopyrightText: 2023 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
// SPDX-FileCopyrightText: 2024 Piras314 <p1r4s@proton.me>
// SPDX-FileCopyrightText: 2025 Aiden <28298836+Aidenkrz@users.noreply.github.com>
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui.Equipment;

public sealed partial class MechSoundboardUi : UIFragment
{
    private MechSoundboardUiFragment? _fragment;
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
        if (state is not MechSoundboardUiState soundboardState)
            return;

        _fragment?.UpdateContents(soundboardState);
    }

    private MechSoundboardUiFragment EnsureFragment()
    {
        if (_fragment != null)
            return _fragment;

        _fragment = new MechSoundboardUiFragment();
        _fragment.OnPlayAction += OnPlayAction;
        return _fragment;
    }

    private void OnPlayAction(int sound)
    {
        if (_userInterface == null || _fragmentOwner == null)
            return;

        _userInterface.SendMessage(new MechSoundboardPlayMessage(IoCManager.Resolve<IEntityManager>().GetNetEntity(_fragmentOwner.Value), sound));
    }
}
