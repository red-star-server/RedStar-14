// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared._RedStar.Skills;
using Robust.Shared.Prototypes;
using Robust.Client.Graphics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._RedStar.Skills.Ui;

public sealed partial class SkillsWindow : FancyWindow
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private HashSet<ProtoId<SkillPrototype>> _visibleSkills;
    private HashSet<ProtoId<SkillPrototype>> _ownedSkills;
    private BoxContainer? _skillsList;
    private readonly bool _showAllSkills;
    private readonly Action<SkillPrototype>? _onToggle;
    private readonly bool _allowRevoke;

    private readonly Color _rowColor1 = Color.FromHex("#17191D");
    private readonly Color _rowColor2 = Color.FromHex("#1D2025");
    private int _rowCount;

    public SkillsWindow(
        IEnumerable<ProtoId<SkillPrototype>> visibleSkills,
        bool showAllSkills = false,
        Action<SkillPrototype>? onToggle = null,
        IEnumerable<ProtoId<SkillPrototype>>? ownedSkills = null,
        bool allowRevoke = true)
    {
        RobustXamlLoader.Load(this);
        IoCManager.InjectDependencies(this);

        _visibleSkills = visibleSkills.ToHashSet();
        _ownedSkills = ownedSkills?.ToHashSet() ?? _visibleSkills.ToHashSet();
        _skillsList = FindControl<BoxContainer>("SkillsList");
        _showAllSkills = showAllSkills;
        _onToggle = onToggle;
        _allowRevoke = allowRevoke;

        FindControl<PanelContainer>("ListPanel").PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#111317"),
            BorderColor = Color.FromHex("#343943"),
            BorderThickness = new Thickness(1),
            ContentMarginTopOverride = 3,
            ContentMarginBottomOverride = 3,
            ContentMarginLeftOverride = 3,
            ContentMarginRightOverride = 3
        };

        PopulateSkills();
    }

    public void SetSkills(
        IEnumerable<ProtoId<SkillPrototype>> visibleSkills,
        IEnumerable<ProtoId<SkillPrototype>>? ownedSkills = null)
    {
        _visibleSkills = visibleSkills.ToHashSet();
        _ownedSkills = ownedSkills?.ToHashSet() ?? _visibleSkills.ToHashSet();
        PopulateSkills();
    }

    public bool HasSkill(ProtoId<SkillPrototype> skill)
    {
        return _ownedSkills.Contains(skill);
    }

    public void SetSkillState(ProtoId<SkillPrototype> skill, bool hasSkill)
    {
        if (hasSkill)
            _ownedSkills.Add(skill);
        else
            _ownedSkills.Remove(skill);

        if (!_showAllSkills)
            _visibleSkills.Remove(skill);

        PopulateSkills();
    }

    private void PopulateSkills()
    {
        _skillsList?.DisposeAllChildren();
        _rowCount = 0;

        if (_skillsList == null)
            return;

        var visibleSkills = GetVisibleSkills().ToList();

        foreach (var difficulty in Enum.GetValues<SkillDifficulty>())
        {
            var skills = visibleSkills
                .Where(skill => skill.Difficulty == difficulty)
                .OrderBy(skill => Loc.GetString($"skill-{skill.ID.ToLower()}"))
                .ToArray();

            if (skills.Length == 0)
                continue;

            _skillsList.AddChild(CreateDifficultyHeader(difficulty));

            foreach (var skill in skills)
            {
                var skillId = (ProtoId<SkillPrototype>) skill.ID;
                var hasSkill = _ownedSkills.Contains(skillId);
                var entry = _onToggle == null
                    ? new SkillDisplayEntry(skill)
                    : new SkillDisplayEntry(skill, hasSkill, _onToggle, _allowRevoke);

                _skillsList.AddChild(CreateSkillRow(entry));
                _rowCount++;
            }
        }

        if (_rowCount == 0)
        {
            _skillsList.AddChild(new Label
            {
                Text = Loc.GetString("skills-current-list-empty"),
                Modulate = Color.FromHex("#AEB4BE"),
                Margin = new Thickness(6, 8)
            });
        }
    }

    private IEnumerable<SkillPrototype> GetVisibleSkills()
    {
        if (_showAllSkills)
            return _prototype.EnumeratePrototypes<SkillPrototype>();

        return _visibleSkills
            .Where(skillId => _prototype.TryIndex(skillId, out _))
            .Select(skillId => _prototype.Index(skillId));
    }

    private PanelContainer CreateSkillRow(Control entry)
    {
        var rowColor = (_rowCount % 2 == 0) ? _rowColor1 : _rowColor2;
        return new PanelContainer
        {
            PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = rowColor,
                BorderColor = Color.FromHex("#282C33"),
                BorderThickness = new Thickness(1, 0, 1, 1),
                ContentMarginTopOverride = 2,
                ContentMarginBottomOverride = 2,
                ContentMarginLeftOverride = 5,
                ContentMarginRightOverride = 6
            },
            Children = { entry }
        };
    }

    private Label CreateDifficultyHeader(SkillDifficulty difficulty)
    {
        return new Label
        {
            Text = Loc.GetString($"skill-difficulty-{difficulty.ToString().ToLower()}"),
            StyleClasses = { "LabelSubText" },
            Modulate = Color.FromHex("#AEB4BE"),
            Margin = new Thickness(4, _rowCount == 0 ? 2 : 8, 0, 3)
        };
    }
}