// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Client.UserInterface.Fragments;
using Content.Shared.Mech;
using Robust.Client.UserInterface;

namespace Content.Client.Mech.Ui.Equipment;

public sealed partial class MechGeneratorUi : UIFragment
{
    [NonSerialized]
    private MechGeneratorUiFragment? _fragment;
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
        if (state is not MechGeneratorUiState generatorState)
            return;

        _fragment?.UpdateContents(generatorState);
    }

    private MechGeneratorUiFragment EnsureFragment()
    {
        if (_fragment != null)
            return _fragment;

        _fragment = new MechGeneratorUiFragment();
        _fragment.OnEjectFuelAction += OnEjectFuelAction;
        return _fragment;
    }

    private void OnEjectFuelAction()
    {
        if (_userInterface == null || _fragmentOwner == null)
            return;

        var entManager = IoCManager.Resolve<IEntityManager>();
        _userInterface.SendMessage(new MechGeneratorEjectFuelMessage(entManager.GetNetEntity(_fragmentOwner.Value)));
    }
}
