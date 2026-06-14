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

    public override Control GetUIFragmentRoot()
    {
        return EnsureFragment();
    }

    public override void Setup(BoundUserInterface userInterface, EntityUid? fragmentOwner)
    {
        var fragment = EnsureFragment();

        if (fragmentOwner == null)
            return;

        fragment.OnEjectFuelAction += () =>
        {
            var entManager = IoCManager.Resolve<IEntityManager>();
            userInterface.SendMessage(new MechGeneratorEjectFuelMessage(entManager.GetNetEntity(fragmentOwner.Value)));
        };
    }

    public override void UpdateState(BoundUserInterfaceState state)
    {
        if (state is not MechGeneratorUiState generatorState)
            return;

        _fragment?.UpdateContents(generatorState);
    }

    private MechGeneratorUiFragment EnsureFragment()
    {
        return _fragment ??= new MechGeneratorUiFragment();
    }
}
