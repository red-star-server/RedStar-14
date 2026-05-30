// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Linq;
using Robust.Client.Graphics;
using Robust.Client.Utility;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;
using Content.Shared._RedStar.Skills;

namespace Content.Client._RedStar.Skills.Ui;

public sealed partial class SkillDisplayEntry : Control
{
    public SkillDisplayEntry(
        SkillPrototype skill,
        bool? hasSkill = null,
        Action<SkillPrototype>? onToggle = null,
        bool allowRevoke = true)
    {
        RobustXamlLoader.Load(this);

        MouseFilter = MouseFilterMode.Pass;

        var colorPanel = FindControl<PanelContainer>("ColorPanel");
        var iconPanel = FindControl<PanelContainer>("IconPanel");
        var skillIcon = FindControl<TextureRect>("SkillIcon");
        var skillNameLabel = FindControl<Label>("SkillNameLabel");
        var toggleButton = FindControl<Button>("ToggleButton");

        colorPanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = skill.Color,
            ContentMarginTopOverride = 0,
            ContentMarginBottomOverride = 0,
            ContentMarginLeftOverride = 0,
            ContentMarginRightOverride = 0
        };

        iconPanel.PanelOverride = new StyleBoxFlat
        {
            BackgroundColor = Color.FromHex("#24262B"),
            BorderColor = skill.Color.WithAlpha(0.85f),
            BorderThickness = new Thickness(1)
        };

        if (skill.Icon is { } icon)
        {
            skillIcon.Texture = icon.Frame0();
        }
        else
        {
            iconPanel.Visible = false;
        }

        skillNameLabel.Text = Loc.GetString($"skill-{skill.ID.ToLower()}");
        var description = Loc.GetString($"skill-{skill.ID.ToLower()}-desc");
        var tooltip = GetSkillTooltip(skill, description);

        ToolTip = tooltip;
        skillNameLabel.ToolTip = tooltip;
        iconPanel.ToolTip = tooltip;

        if (hasSkill == null || onToggle == null)
            return;

        if (hasSkill.Value && !allowRevoke)
            return;

        toggleButton.Visible = true;
        toggleButton.Text = hasSkill.Value ? "-" : "+";
        toggleButton.ToolTip = Loc.GetString(hasSkill.Value
            ? "admin-skills-revoke"
            : allowRevoke
                ? "admin-skills-grant"
                : "teach-skills-start");
        toggleButton.OnPressed += _ => onToggle(skill);
    }

    private static string GetSkillTooltip(SkillPrototype skill, string description)
    {
        if (skill.LearningPrerequisites.Count == 0)
            return description;

        var prerequisites = string.Join(", ", skill.LearningPrerequisites
            .Select(prerequisite => Loc.GetString($"skill-{prerequisite.ToString().ToLower()}")));

        return $"{description}\n{Loc.GetString("skill-learning-prerequisites-tooltip", ("skills", prerequisites))}";
    }
}
