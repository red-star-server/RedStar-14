// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using System.Linq;
using Content.Shared.Construction;
using Content.Shared.Construction.Components;
using JetBrains.Annotations;
using Robust.Server.Containers;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Server.Construction.Completions;

/// <summary>
/// Converts a finished entity back into its part assembly host and restores the completed parts.
/// </summary>
[UsedImplicitly, DataDefinition]
public sealed partial class PartDisassemble : IGraphAction
{
    public void PerformAction(EntityUid uid, EntityUid? userUid, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent(uid, out PartDisassemblyComponent? disassembly))
            return;

        var container = entityManager.System<ContainerSystem>();
        var proto = IoCManager.Resolve<IPrototypeManager>();
        var xform = entityManager.GetComponent<TransformComponent>(uid);
        var host = entityManager.SpawnEntity(disassembly.AssemblyPrototype, xform.Coordinates);

        if (entityManager.TryGetComponent(host, out PartAssemblyComponent? assembly))
            RestoreAssemblyParts(uid, host, assembly, disassembly, entityManager, container, proto, xform);

        foreach (var existingContainer in container.GetAllContainers(uid))
        {
            container.EmptyContainer(existingContainer, true);
        }

        var entChangeEv = new ConstructionChangeEntityEvent(host, uid);
        entityManager.EventBus.RaiseLocalEvent(uid, entChangeEv);
        entityManager.EventBus.RaiseLocalEvent(host, entChangeEv, broadcast: true);
        entityManager.QueueDeleteEntity(uid);
    }

    private static void RestoreAssemblyParts(
        EntityUid uid,
        EntityUid host,
        PartAssemblyComponent assembly,
        PartDisassemblyComponent disassembly,
        IEntityManager entityManager,
        ContainerSystem container,
        IPrototypeManager proto,
        TransformComponent xform)
    {
        var assemblyId = disassembly.AssemblyId ?? assembly.Parts.Keys.FirstOrDefault();
        if (assemblyId == null || !assembly.Parts.TryGetValue(assemblyId, out var parts))
            return;

        assembly.CurrentAssembly = assemblyId;
        assembly.PartsContainer = container.EnsureContainer<Container>(host, assembly.ContainerId);

        foreach (var partId in parts)
        {
            if (!proto.TryIndex<EntityPrototype>(partId, out _))
            {
                Logger.Warning($"Unable to restore part assembly part '{partId}' for entity {uid}.");
                continue;
            }

            var part = entityManager.SpawnEntity(partId, xform.Coordinates);
            container.Insert(part, assembly.PartsContainer);
        }

        var ev = new PartAssemblyPartInsertedEvent();
        entityManager.EventBus.RaiseLocalEvent(host, ev);
    }
}
