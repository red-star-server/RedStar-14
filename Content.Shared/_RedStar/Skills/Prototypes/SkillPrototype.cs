// SPDX-FileCopyrightText: 2026 RedStar Contributors
//
// SPDX-License-Identifier: AGPL-3.0-or-later

using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RedStar.Skills;

[Prototype("skill")]
public sealed partial class SkillPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField("color", required: true)]
    public Color Color { get; private set; } = Color.White;

    [DataField]
    public SpriteSpecifier? Icon { get; private set; }

    [DataField]
    public SkillDifficulty Difficulty { get; private set; } = SkillDifficulty.Easy;

    [DataField]
    public HashSet<ProtoId<SkillPrototype>> LearningPrerequisites { get; private set; } = new();
}

public enum SkillDifficulty : byte
{
    Easy,
    Medium,
    Hard
}

public static class SkillDifficultyLearningTime
{
    public static TimeSpan GetLearningTime(SkillDifficulty difficulty)
    {
        return difficulty switch
        {
            SkillDifficulty.Easy => TimeSpan.FromMinutes(1),
            SkillDifficulty.Medium => TimeSpan.FromMinutes(2.5),
            SkillDifficulty.Hard => TimeSpan.FromMinutes(5),
            _ => TimeSpan.FromMinutes(1)
        };
    }
}
