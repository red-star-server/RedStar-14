// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared._RedStar.Skills;
using Content.Client._RedStar.Skills.Ui;
using Robust.Shared.Prototypes;

namespace Content.Client._RedStar.Skills;

public sealed partial class SkillsSystem : SharedSkillsSystem
{
    private SkillsWindow? _adminSkillsWindow;
    private SkillsWindow? _teachSkillsWindow;
    private NetEntity _adminSkillsTarget;
    private NetEntity _teachSkillsTarget;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<OpenAdminSkillsWindowEvent>(OnOpenAdminSkillsWindow);
        SubscribeNetworkEvent<OpenTeachSkillsWindowEvent>(OnOpenTeachSkillsWindow);
        SubscribeNetworkEvent<UpdatePlayerSkillsEvent>(OnUpdatePlayerSkills);
    }

    public override bool HasSkill(EntityUid uid, ProtoId<SkillPrototype> skill)
    {
        return _cachedPlayerSkills?.Contains(skill) ?? false;
    }

    private List<ProtoId<SkillPrototype>>? _cachedPlayerSkills;

    public void RequestPlayerSkills()
    {
        if (_cachedPlayerSkills != null)
            PlayerSkillsWindowUpdated?.Invoke(_cachedPlayerSkills);

        RaiseNetworkEvent(new RequestPlayerSkillsEvent());
    }

    public event Action<List<ProtoId<SkillPrototype>>>? PlayerSkillsWindowUpdated;

    private void OnUpdatePlayerSkills(UpdatePlayerSkillsEvent msg)
    {
        _cachedPlayerSkills = msg.Skills;
        PlayerSkillsWindowUpdated?.Invoke(msg.Skills);
    }

    private void OnOpenAdminSkillsWindow(OpenAdminSkillsWindowEvent msg)
    {
        _adminSkillsTarget = msg.Target;

        if (_adminSkillsWindow == null || _adminSkillsWindow.Disposed)
        {
            _adminSkillsWindow = new SkillsWindow(msg.Skills, showAllSkills: true, OnToggleSkill)
            {
                Title = Loc.GetString("admin-skills-window")
            };
            _adminSkillsWindow.OpenCentered();
        }
        else
        {
            _adminSkillsWindow.SetSkills(msg.Skills);
            if (!_adminSkillsWindow.IsOpen)
                _adminSkillsWindow.OpenCentered();
        }
    }

    private void OnToggleSkill(SkillPrototype skill)
    {
        var skillId = (ProtoId<SkillPrototype>) skill.ID;
        _adminSkillsWindow?.SetSkillState(skillId, !_adminSkillsWindow.HasSkill(skillId));
        RaiseNetworkEvent(new AdminToggleSkillEvent(_adminSkillsTarget, skillId));
    }

    private void OnOpenTeachSkillsWindow(OpenTeachSkillsWindowEvent msg)
    {
        _teachSkillsTarget = msg.Target;

        if (_teachSkillsWindow == null || _teachSkillsWindow.Disposed)
        {
            _teachSkillsWindow = new SkillsWindow(
                msg.TeacherSkills,
                onToggle: OnTeachSkill,
                ownedSkills: msg.TargetSkills,
                allowRevoke: false)
            {
                Title = Loc.GetString("teach-skills-window")
            };
            _teachSkillsWindow.OpenCentered();
        }
        else
        {
            _teachSkillsWindow.SetSkills(msg.TeacherSkills, msg.TargetSkills);
            if (!_teachSkillsWindow.IsOpen)
                _teachSkillsWindow.OpenCentered();
        }
    }

    private void OnTeachSkill(SkillPrototype skill)
    {
        RaiseNetworkEvent(new TeachSkillRequestEvent(_teachSkillsTarget, (ProtoId<SkillPrototype>) skill.ID));
    }
}
