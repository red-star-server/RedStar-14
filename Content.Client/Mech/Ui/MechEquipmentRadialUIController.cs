// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Client.UserInterface.Controls;
using Content.Shared.Mech;
using Content.Shared.Mech.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controllers;
using Robust.Shared.Utility;

namespace Content.Client.Mech.Ui;

[UsedImplicitly]
public sealed class MechEquipmentRadialUIController : UIController
{
    [Dependency] private readonly IEntityManager _entManager = default!;

    private SimpleRadialMenu? _menu;

    public void OpenRadialMenu(Entity<MechComponent> mech)
    {
        CloseMenu();

        _menu = new SimpleRadialMenu();
        _menu.SetButtons(ConvertToButtons(mech.Comp));
        _menu.OnClose += OnMenuClosed;
        _menu.OpenCentered();
    }

    private List<RadialMenuOption> ConvertToButtons(MechComponent mechComp)
    {
        var options = new List<RadialMenuOption>
        {
            new RadialMenuActionOption<string>(_ =>
                {
                    _entManager.RaisePredictiveEvent(new RequestMechEquipmentSelectEvent(null));
                },
                "no_equipment")
            {
                ToolTip = Loc.GetString("mech-radial-no-equipment"),
                Sprite = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/Actions/actions_mecha.rsi/mech_cycle_equip_off.png"))
            }
        };

        foreach (var equipment in mechComp.EquipmentContainer.ContainedEntities)
        {
            if (!_entManager.TryGetComponent<MetaDataComponent>(equipment, out var metaData))
                continue;

            var equipmentEntity = equipment;
            var sprite = metaData.EntityPrototype != null
                ? new SpriteSpecifier.EntityPrototype(metaData.EntityPrototype.ID)
                : null;

            options.Add(new RadialMenuActionOption<string>(_ =>
                {
                    _entManager.RaisePredictiveEvent(new RequestMechEquipmentSelectEvent(_entManager.GetNetEntity(equipmentEntity)));
                },
                metaData.EntityName)
            {
                ToolTip = metaData.EntityName,
                Sprite = sprite
            });
        }

        return options;
    }

    private void CloseMenu()
    {
        if (_menu == null)
            return;

        _menu.Close();
        _menu = null;
    }

    private void OnMenuClosed()
    {
        if (_menu != null)
            _menu.OnClose -= OnMenuClosed;

        _menu = null;
    }
}
