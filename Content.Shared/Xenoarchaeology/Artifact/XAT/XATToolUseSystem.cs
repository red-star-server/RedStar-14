using Content.Shared._RedStar.Skills; // RS14
using Content.Shared.Interaction;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;
using Content.Shared.Xenoarchaeology.Artifact.Components;
using Content.Shared.Xenoarchaeology.Artifact.XAT.Components;
using Robust.Shared.Prototypes; // RS14
using Robust.Shared.Random; // RS14

namespace Content.Shared.Xenoarchaeology.Artifact.XAT;

/// <summary>
/// This handles <see cref="XATToolUseComponent"/>
/// </summary>
public sealed class XATToolUseSystem : BaseXATSystem<XATToolUseComponent>
{
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedSkillsSystem _skills = default!; // RS14
    [Dependency] private readonly IRobustRandom _random = default!; // RS14

    // RS14-start
    private const float ArtifactToolDelayModifierWithoutSkill = 1.8f;
    private const float ArtifactToolFailureChanceWithoutSkill = 0.35f;
    private static readonly ProtoId<SkillPrototype> ArtifactsSkill = "Artifacts";
    // RS14-end

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        XATSubscribeDirectEvent<InteractUsingEvent>(OnInteractUsing);
        XATSubscribeDirectEvent<XATToolUseDoAfterEvent>(OnToolUseComplete);
    }

    private void OnToolUseComplete(Entity<XenoArtifactComponent> artifact, Entity<XATToolUseComponent, XenoArtifactNodeComponent> node, ref XATToolUseDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (GetEntity(args.Node) != node.Owner)
            return;

        // RS14-start
        if (!_skills.HasSkill(args.User, ArtifactsSkill)
            && _random.Prob(ArtifactToolFailureChanceWithoutSkill))
        {
            args.Handled = true;
            return;
        }
        // RS14-end

        Trigger(artifact, node);
        args.Handled = true;
    }

    private void OnInteractUsing(Entity<XenoArtifactComponent> artifact, Entity<XATToolUseComponent, XenoArtifactNodeComponent> node, ref InteractUsingEvent args)
    {
        if (!TryComp<ToolComponent>(args.Used, out var tool))
            return;

        var toolUseTriggerComponent = node.Comp1;
        // RS14-start
        var delay = toolUseTriggerComponent.Delay;
        if (!_skills.HasSkill(args.User, ArtifactsSkill))
            delay *= ArtifactToolDelayModifierWithoutSkill;
        // RS14-end

        args.Handled = _tool.UseTool(args.Used,
            args.User,
            artifact,
            delay, // RS14
            toolUseTriggerComponent.RequiredTool,
            new XATToolUseDoAfterEvent(GetNetEntity(node)),
            fuel: toolUseTriggerComponent.Fuel,
            tool);
    }
}
