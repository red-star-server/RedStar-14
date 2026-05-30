// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.Antag.Components;
using Content.Server.Ghost.Roles.Components;
using Content.Shared._RedStar.CCVar;
using Content.Shared._RedStar.Skills;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Roles;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Player;

namespace Content.Server._RedStar.Skills;

public sealed partial class SkillsSystem : SharedSkillsSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private static readonly ProtoId<TagPrototype> SkillsTag = "Skills";
    private bool _skillsEnabled = true;
    private int _skillsMinimumPlayers = 10;
    private readonly HashSet<ProtoId<SkillPrototype>> _invalidSkillLogs = new();

    public override void Initialize()
    {
        base.Initialize();

        _skillsEnabled = _cfg.GetCVar(RedStarSkillsCVars.SkillsEnabled);
        Subs.CVar(_cfg, RedStarSkillsCVars.SkillsEnabled, value =>
        {
            _skillsEnabled = value;
            BroadcastSkillsState();
        });
        _skillsMinimumPlayers = _cfg.GetCVar(RedStarSkillsCVars.SkillsMinimumPlayers);
        Subs.CVar(_cfg, RedStarSkillsCVars.SkillsMinimumPlayers, value =>
        {
            _skillsMinimumPlayers = value;
            BroadcastSkillsState();
        });

        _prototype.PrototypesReloaded += OnPrototypesReloaded;
        _player.PlayerStatusChanged += OnPlayerStatusChanged;
        ValidateSkillReferences();

        SubscribeLocalEvent<ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);

        SubscribeNetworkEvent<AdminToggleSkillEvent>(OnAdminToggleSkill);
        SubscribeNetworkEvent<RequestPlayerSkillsEvent>(OnRequestPlayerSkills);
        SubscribeNetworkEvent<RequestSkillsStateEvent>(OnRequestSkillsState);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _prototype.PrototypesReloaded -= OnPrototypesReloaded;
        _player.PlayerStatusChanged -= OnPlayerStatusChanged;
    }

    public override bool HasSkill(EntityUid entity, ProtoId<SkillPrototype> skill)
    {
        if (!IsSkillsEnabled())
            return true;

        if (!_prototype.HasIndex(skill))
        {
            if (_invalidSkillLogs.Add(skill))
                Log.Error($"Unknown skill prototype '{skill}' checked on {ToPrettyString(entity)}.");

            return false;
        }

        if (!_mind.TryGetMind(entity, out _, out var mind))
            return false;

        return mind.Skills.Contains(skill);
    }

    public bool IsSkillsEnabled()
    {
        return _skillsEnabled && _player.PlayerCount >= _skillsMinimumPlayers;
    }

    public bool CanLearnSkill(EntityUid entity, ProtoId<SkillPrototype> skill)
    {
        if (!_prototype.TryIndex(skill, out var skillPrototype))
            return false;

        if (!_mind.TryGetMind(entity, out _, out var mind))
            return false;

        if (mind.Skills.Contains(skill))
            return false;

        return skillPrototype.LearningPrerequisites.All(prerequisite => mind.Skills.Contains(prerequisite));
    }

    public bool TryGetMissingLearningPrerequisites(
        EntityUid entity,
        ProtoId<SkillPrototype> skill,
        out List<ProtoId<SkillPrototype>> missingPrerequisites)
    {
        missingPrerequisites = new List<ProtoId<SkillPrototype>>();

        if (!_prototype.TryIndex(skill, out var skillPrototype))
            return false;

        if (!_mind.TryGetMind(entity, out _, out var mind))
            return false;

        missingPrerequisites = skillPrototype.LearningPrerequisites
            .Where(prerequisite => !mind.Skills.Contains(prerequisite))
            .ToList();

        return missingPrerequisites.Count > 0;
    }

    public string GetSkillNames(IEnumerable<ProtoId<SkillPrototype>> skills)
    {
        return string.Join(", ", skills.Select(GetSkillName));
    }

    public string GetSkillName(ProtoId<SkillPrototype> skill)
    {
        return _prototype.TryIndex(skill, out var skillPrototype)
            ? Loc.GetString($"skill-{skillPrototype.ID.ToLower()}")
            : skill.ToString();
    }

    private void OnImplantImplanted(ref ImplantImplantedEvent ev)
    {
        if (ev.Implanted is null)
            return;

        if (!_tag.HasTag(ev.Implant, SkillsTag))
            return;

        GrantAllSkills(ev.Implanted.Value);
    }

    private void OnGetVerbs(GetVerbsEvent<Verb> args)
    {
        if (!TryComp(args.User, out ActorComponent? actor))
            return;

        var session = actor.PlayerSession;
        if (_admin.HasAdminFlag(session, AdminFlags.Admin))
        {
            args.Verbs.Add(new Verb
            {
                Text = Loc.GetString("admin-skills-verb"),
                Category = VerbCategory.Admin,
                Act = () => SendAdminSkillsWindow(session, args.Target),
                Impact = LogImpact.Low
            });
        }
    }

    private void OnAdminToggleSkill(AdminToggleSkillEvent msg, EntitySessionEventArgs args)
    {
        if (!_admin.HasAdminFlag(args.SenderSession, AdminFlags.Admin))
            return;

        if (!_prototype.HasIndex(msg.Skill))
            return;

        if (!TryGetEntity(msg.Target, out var target))
            return;

        if (!_mind.TryGetMind(target.Value, out _, out var mind))
            return;

        if (mind.Skills.Contains(msg.Skill))
            RevokeSkill(target.Value, msg.Skill);
        else
            GrantSkill(target.Value, msg.Skill);

        SendAdminSkillsWindow(args.SenderSession, target.Value);
    }

    private void OnRequestPlayerSkills(RequestPlayerSkillsEvent msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is not { } entity)
            return;

        SendPlayerSkillsUpdate(args.SenderSession, entity);
    }

    private void OnRequestSkillsState(RequestSkillsStateEvent msg, EntitySessionEventArgs args)
    {
        SendSkillsStateUpdate(args.SenderSession);
    }

    private bool TryGetMindSkills(EntityUid entity, out List<ProtoId<SkillPrototype>> skills)
    {
        skills = new List<ProtoId<SkillPrototype>>();
        if (!_mind.TryGetMind(entity, out _, out var mind))
            return false;

        skills = mind.Skills.ToList();
        return true;
    }

    private void SendPlayerSkillsUpdate(ICommonSession session, EntityUid entity)
    {
        if (!TryGetMindSkills(entity, out var skills))
            return;

        RaiseNetworkEvent(new UpdatePlayerSkillsEvent(skills), session);
    }

    private void SendSkillsStateUpdate(ICommonSession session)
    {
        RaiseNetworkEvent(new UpdateSkillsStateEvent(IsSkillsEnabled()), session);
    }

    private void BroadcastSkillsState()
    {
        var enabled = IsSkillsEnabled();
        var ev = new UpdateSkillsStateEvent(enabled);

        foreach (var session in _player.Sessions)
        {
            RaiseNetworkEvent(ev, session);
        }
    }

    private void SendAdminSkillsWindow(ICommonSession session, EntityUid target)
    {
        if (!TryGetMindSkills(target, out var skills))
            return;

        RaiseNetworkEvent(new OpenAdminSkillsWindowEvent(GetNetEntity(target), skills), session);
    }

    public void GrantAllSkills(EntityUid entity, bool clearSkills = false)
    {
        GrantSkill(entity, _prototype.EnumeratePrototypes<SkillPrototype>()
            .Select(skill => (ProtoId<SkillPrototype>) skill.ID)
            .ToHashSet(), clearSkills);
    }

    public void GrantSkill(EntityUid entity, HashSet<ProtoId<SkillPrototype>> skills, bool clearSkills = false)
    {
        if (!_mind.TryGetMind(entity, out var mindId, out var mind))
            return;

        var validSkills = skills.Where(skill => _prototype.HasIndex(skill)).ToHashSet();
        if (validSkills.Count == 0 && !clearSkills)
            return;

        var changed = false;

        if (clearSkills)
        {
            changed = mind.Skills.Count > 0;
            mind.Skills.Clear();
        }

        changed = validSkills.Aggregate(changed, (current, skill) => current | mind.Skills.Add(skill));

        if (!changed) return;

        foreach (var skill in validSkills.Where(s => mind.Skills.Contains(s)))
        {
            RaiseLocalEvent(new SkillGrantedEvent { Entity = entity, Skill = skill });
        }
        Dirty(mindId, mind);
        if (TryComp(entity, out ActorComponent? actor))
            SendPlayerSkillsUpdate(actor.PlayerSession, entity);
    }

    public void GrantSkill(EntityUid entity, bool clearSkills = false, params ProtoId<SkillPrototype>[] skills)
    {
        GrantSkill(entity, new HashSet<ProtoId<SkillPrototype>>(skills), clearSkills);
    }

    public void GrantSkill(EntityUid entity, ProtoId<SkillPrototype> skill, bool clearSkills = false)
    {
        GrantSkill(entity, new HashSet<ProtoId<SkillPrototype>> { skill }, clearSkills);
    }

    public void ClearSkills(EntityUid entity)
    {
        if (!_mind.TryGetMind(entity, out var mindId, out var mind)
            || mind.Skills.Count == 0)
        {
            return;
        }

        mind.Skills.Clear();
        Dirty(mindId, mind);

        if (TryComp(entity, out ActorComponent? actor))
            SendPlayerSkillsUpdate(actor.PlayerSession, entity);
    }

    private void RevokeSkill(EntityUid entity, HashSet<ProtoId<SkillPrototype>> skills)
    {
        if (!_mind.TryGetMind(entity, out var mindId, out var mind))
            return;

        var validSkills = skills.Where(skill => _prototype.HasIndex(skill)).ToHashSet();
        if (validSkills.Count == 0)
            return;

        var changed = validSkills.Aggregate(false, (current, skill) => current | mind.Skills.Remove(skill));

        if (!changed) return;

        foreach (var skill in validSkills.Where(s => !mind.Skills.Contains(s)))
        {
            RaiseLocalEvent(new SkillRevokedEvent { Entity = entity, Skill = skill });
        }
        Dirty(mindId, mind);
        if (TryComp(entity, out ActorComponent? actor))
            SendPlayerSkillsUpdate(actor.PlayerSession, entity);
    }

    private void RevokeSkill(EntityUid entity, ProtoId<SkillPrototype> skill)
    {
        RevokeSkill(entity, new HashSet<ProtoId<SkillPrototype>> { skill });
    }

    private void OnPlayerStatusChanged(object? sender, SessionStatusEventArgs args)
    {
        BroadcastSkillsState();
    }

    private void OnPrototypesReloaded(PrototypesReloadedEventArgs args)
    {
        _invalidSkillLogs.Clear();
        ValidateSkillReferences();
    }

    private void ValidateSkillReferences()
    {
        foreach (var skill in _prototype.EnumeratePrototypes<SkillPrototype>())
        {
            ValidateSkillSet(skill.LearningPrerequisites, $"skill prototype '{skill.ID}' learning prerequisites");
        }

        ValidateSkillPrerequisiteCycles();

        foreach (var job in _prototype.EnumeratePrototypes<JobPrototype>())
        {
            ValidateSkillSet(job.Skills, $"job prototype '{job.ID}'");
        }

        foreach (var entity in _prototype.EnumeratePrototypes<EntityPrototype>())
        {
            foreach (var (_, registration) in entity.Components)
            {
                switch (registration.Component)
                {
                    case SkillLearningBookComponent book:
                        ValidateSkillReference(book.Skill, $"skill book prototype '{entity.ID}'");
                        break;
                    case GhostRoleComponent ghostRole:
                        ValidateSkillSet(ghostRole.Skills, $"ghost role prototype '{entity.ID}'");
                        break;
                    case AntagSelectionComponent antag:
                        foreach (var definition in antag.Definitions)
                        {
                            ValidateSkillSet(definition.Skills, $"antag selection prototype '{entity.ID}'");
                        }

                        break;
                }
            }
        }
    }

    private void ValidateSkillSet(IEnumerable<ProtoId<SkillPrototype>> skills, string source)
    {
        foreach (var skill in skills)
        {
            ValidateSkillReference(skill, source);
        }
    }

    private void ValidateSkillReference(ProtoId<SkillPrototype> skill, string source)
    {
        if (_prototype.HasIndex(skill))
            return;

        Log.Error($"Unknown skill prototype '{skill}' referenced by {source}.");
    }

    private void ValidateSkillPrerequisiteCycles()
    {
        var visited = new HashSet<ProtoId<SkillPrototype>>();
        var visiting = new HashSet<ProtoId<SkillPrototype>>();
        var path = new Stack<ProtoId<SkillPrototype>>();

        foreach (var skill in _prototype.EnumeratePrototypes<SkillPrototype>())
        {
            VisitSkillPrerequisite((ProtoId<SkillPrototype>) skill.ID, visited, visiting, path);
        }
    }

    private void VisitSkillPrerequisite(
        ProtoId<SkillPrototype> skill,
        HashSet<ProtoId<SkillPrototype>> visited,
        HashSet<ProtoId<SkillPrototype>> visiting,
        Stack<ProtoId<SkillPrototype>> path)
    {
        if (visited.Contains(skill))
            return;

        if (!_prototype.TryIndex(skill, out var skillPrototype))
            return;

        if (!visiting.Add(skill))
        {
            Log.Error($"Circular skill learning prerequisite detected: {string.Join(" -> ", path.Reverse())} -> {skill}.");
            return;
        }

        path.Push(skill);

        foreach (var prerequisite in skillPrototype.LearningPrerequisites)
        {
            VisitSkillPrerequisite(prerequisite, visited, visiting, path);
        }

        path.Pop();
        visiting.Remove(skill);
        visited.Add(skill);
    }
}
