// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: MIT

using Content.Shared.Construction;
using Robust.Shared.Prototypes;

namespace Content.Shared.Construction.Components;

/// <summary>
/// Allows a finished entity to be converted back into a <see cref="PartAssemblyComponent"/>
/// host populated with the parts for a completed assembly.
/// </summary>
[RegisterComponent]
[Access(typeof(PartAssemblySystem))]
public sealed partial class PartDisassemblyComponent : Component
{
    /// <summary>
    /// Part assembly host prototype to spawn when disassembling this entity.
    /// </summary>
    [DataField(required: true)]
    public EntProtoId AssemblyPrototype = default!;

    /// <summary>
    /// Assembly id inside <see cref="PartAssemblyComponent.Parts"/> to restore.
    /// If omitted, the first assembly on the spawned host is used.
    /// </summary>
    [DataField]
    public string? AssemblyId;

    /// <summary>
    /// Concrete entity prototypes restored into the assembly host.
    /// Part assembly requirements are tags, so they cannot safely be treated as prototype IDs.
    /// </summary>
    [DataField]
    public List<EntProtoId> PartPrototypes = new();
}
