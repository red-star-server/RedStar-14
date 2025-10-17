using System.Linq;
using Content.Server.Administration.Managers;
using Content.Shared._RedStar.CCVar;
using Content.Shared._RedStar.Skills;
using Content.Shared.Administration;
using Content.Shared.Database;
using Content.Shared.Implants;
using Content.Shared.Mind;
using Content.Shared.Tag;
using Content.Shared.Verbs;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._RedStar.Skills;

public sealed partial class SkillsSystem : SharedSkillsSystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IAdminManager _admin = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    private static readonly ProtoId<TagPrototype> SkillsTag = "Skills";
    private bool _skillsEnabled = true;

    public override void Initialize()
    {
        base.Initialize();

        _skillsEnabled = _cfg.GetCVar(RedStarSkillsCVars.SkillsEnabled);
        Subs.CVar(_cfg, RedStarSkillsCVars.SkillsEnabled, value => _skillsEnabled = value);

        SubscribeLocalEvent<ImplantImplantedEvent>(OnImplantImplanted);
        SubscribeLocalEvent<GetVerbsEvent<Verb>>(OnGetVerbs);

        SubscribeNetworkEvent<AdminToggleSkillEvent>(OnAdminToggleSkill);
        SubscribeNetworkEvent<RequestPlayerSkillsEvent>(OnRequestPlayerSkills);
    }

    public override bool HasSkill(EntityUid entity, ProtoId<SkillPrototype> skill)
    {
        if (!_skillsEnabled)
            return true;

        if (!_mind.TryGetMind(entity, out _, out var mind))
            return false;

        return mind.Skills.Contains(skill);
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
}
