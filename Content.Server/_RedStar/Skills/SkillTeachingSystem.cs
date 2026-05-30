// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Shared._RedStar.Skills;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._RedStar.Skills;

public sealed class SkillTeachingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedInteractionSystem _interaction = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;

    private const float LearningPenaltyPerKnownSkill = 0.15f;
    private const float MaxLearningPenalty = 4f;
    private readonly HashSet<TeachingRequest> _activeTeachings = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);
        SubscribeLocalEvent<SkillTeachingDoAfterEvent>(OnTeachingFinished);
        SubscribeNetworkEvent<TeachSkillRequestEvent>(OnTeachSkillRequest);
    }

    public TimeSpan GetLearningTime(EntityUid learner, SkillDifficulty difficulty)
    {
        var baseTime = SkillDifficultyLearningTime.GetLearningTime(difficulty);

        if (!_mind.TryGetMind(learner, out _, out var mind))
            return baseTime;

        var multiplier = MathF.Min(MaxLearningPenalty, 1f + mind.Skills.Count * LearningPenaltyPerKnownSkill);
        return TimeSpan.FromSeconds(baseTime.TotalSeconds * multiplier);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!_skills.IsSkillsEnabled()
            || args.User == args.Target
            || !args.CanInteract
            || !TryComp(args.User, out ActorComponent? actor))
        {
            return;
        }

        if (!_mind.TryGetMind(args.User, out _, out var teacherMind)
            || !_mind.TryGetMind(args.Target, out _, out _)
            || !HasTeachableSkill(args.Target, teacherMind.Skills))
        {
            return;
        }

        var session = actor.PlayerSession;
        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("teach-skills-verb"),
            Category = VerbCategory.Interaction,
            Icon = new SpriteSpecifier.Texture(new("/Textures/Interface/students-cap.svg.192dpi.png")),
            Act = () => SendTeachSkillsWindow(session, args.User, args.Target),
            Impact = LogImpact.Low
        });
    }

    private void OnTeachSkillRequest(TeachSkillRequestEvent msg, EntitySessionEventArgs args)
    {
        if (!_skills.IsSkillsEnabled())
            return;

        if (!_prototype.TryIndex(msg.Skill, out var skill))
            return;

        if (args.SenderSession.AttachedEntity is not { } teacher
            || !TryGetEntity(msg.Target, out var target)
            || target.Value == teacher
            || HasActiveTeaching(teacher, target.Value, msg.Skill))
        {
            return;
        }

        if (!CanTeachSkill(teacher, target.Value, msg.Skill))
        {
            PopupTeachingFailure(teacher, target.Value, msg.Skill);
            return;
        }

        var doAfter = new DoAfterArgs(
            EntityManager,
            teacher,
            GetLearningTime(target.Value, skill.Difficulty),
            new SkillTeachingDoAfterEvent(GetNetEntity(target.Value), msg.Skill),
            null,
            target: target.Value,
            showTo: target.Value)
        {
            BreakOnDamage = true,
            Broadcast = true,
            CancelDuplicate = false,
            DistanceThreshold = SharedInteractionSystem.InteractionRange,
            DuplicateCondition = DuplicateConditions.SameEvent | DuplicateConditions.SameTarget,
            NeedHand = false,
            RequireCanInteract = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfter))
            return;

        _activeTeachings.Add(new TeachingRequest(teacher, GetNetEntity(target.Value), msg.Skill));
    }

    private void OnTeachingFinished(SkillTeachingDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
        {
            RemoveActiveTeaching(args.User, args.TargetEntity, args.Skill);

            return;
        }

        args.Handled = true;

        RemoveActiveTeaching(args.User, args.TargetEntity, args.Skill);

        if (!_skills.IsSkillsEnabled()
            || !TryGetEntity(args.TargetEntity, out var target))
        {
            return;
        }

        if (!CanTeachSkill(args.User, target.Value, args.Skill))
        {
            PopupTeachingFailure(args.User, target.Value, args.Skill);

            return;
        }

        _skills.GrantSkill(target.Value, args.Skill);

        if (TryComp(args.User, out ActorComponent? actor))
            SendTeachSkillsWindow(actor.PlayerSession, args.User, target.Value);
    }

    private void SendTeachSkillsWindow(ICommonSession session, EntityUid teacher, EntityUid target)
    {
        if (!_mind.TryGetMind(teacher, out _, out var teacherMind)
            || !_mind.TryGetMind(target, out _, out var targetMind))
        {
            return;
        }

        RaiseNetworkEvent(new OpenTeachSkillsWindowEvent(
            GetNetEntity(target),
            teacherMind.Skills.Where(skill => _skills.CanLearnSkill(target, skill)).ToList(),
            targetMind.Skills.ToList()), session);
    }

    private bool CanTeachSkill(EntityUid teacher, EntityUid target, ProtoId<SkillPrototype> skill)
    {
        if (!_prototype.HasIndex(skill)
            || !_interaction.InRangeUnobstructed(teacher, target)
            || !_mind.TryGetMind(teacher, out _, out var teacherMind)
            || !_mind.TryGetMind(target, out _, out _))
        {
            return false;
        }

        return teacherMind.Skills.Contains(skill) && _skills.CanLearnSkill(target, skill);
    }

    private bool HasActiveTeaching(EntityUid teacher, EntityUid target, ProtoId<SkillPrototype> skill)
    {
        return _activeTeachings.Contains(new TeachingRequest(teacher, GetNetEntity(target), skill));
    }

    private void PopupTeachingFailure(EntityUid teacher, EntityUid target, ProtoId<SkillPrototype> skill)
    {
        if (_skills.HasSkill(target, skill))
        {
            _popup.PopupEntity(Loc.GetString("skill-teaching-target-already-known"), teacher, teacher);
            return;
        }

        if (_skills.TryGetMissingLearningPrerequisites(target, skill, out var missingPrerequisites))
        {
            _popup.PopupEntity(
                Loc.GetString("skill-teaching-missing-prerequisites", ("skills", _skills.GetSkillNames(missingPrerequisites))),
                teacher,
                teacher);
        }
    }

    private void RemoveActiveTeaching(EntityUid teacher, NetEntity target, ProtoId<SkillPrototype> skill)
    {
        _activeTeachings.Remove(new TeachingRequest(teacher, target, skill));
    }

    private bool HasTeachableSkill(
        EntityUid target,
        IReadOnlySet<ProtoId<SkillPrototype>> teacherSkills)
    {
        return teacherSkills.Any(skill => _skills.CanLearnSkill(target, skill));
    }

    private readonly record struct TeachingRequest(
        EntityUid Teacher,
        NetEntity Target,
        ProtoId<SkillPrototype> Skill);
}
