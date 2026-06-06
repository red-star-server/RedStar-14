// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Interaction.Events;
using Content.Shared.Mech.Components;

namespace Content.Shared.Mech.EntitySystems;

public abstract partial class SharedMechSystem
{
    private void InitializeRelay()
    {
        SubscribeLocalEvent<MechComponent, GettingAttackedAttemptEvent>(RelayRefToPilot);
    }

    private void RelayToPilot<T>(Entity<MechComponent> uid, T args) where T : class
    {
        // RS14-start
        if (!Vehicle.TryGetOperator(uid.Owner, out var operatorEnt))
            return;
        // RS14-end

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(operatorEnt.Value, ref ev); // RS14
    }

    private void RelayRefToPilot<T>(Entity<MechComponent> uid, ref T args) where T :struct
    {
        // RS14-start
        if (!Vehicle.TryGetOperator(uid.Owner, out var operatorEnt))
            return;
        // RS14-end

        var ev = new MechPilotRelayedEvent<T>(args);

        RaiseLocalEvent(operatorEnt.Value, ref ev); // RS14

        args = ev.Args;
    }
}

[ByRefEvent]
public record struct MechPilotRelayedEvent<TEvent>(TEvent Args)
{
    public TEvent Args = Args;
}
