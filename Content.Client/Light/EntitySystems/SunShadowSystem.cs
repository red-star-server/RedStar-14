// SPDX-FileCopyrightText: 2025 GoobBot <uristmchands@proton.me>
// SPDX-FileCopyrightText: 2025 metalgearsloth <31366439+metalgearsloth@users.noreply.github.com>
//
// SPDX-License-Identifier: MIT

using System.Diagnostics.Contracts;
using System.Numerics;
using Content.Client.GameTicking.Managers;
using Content.Shared.Light.Components;
using Content.Shared.Light.EntitySystems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.Light.EntitySystems;

public sealed class SunShadowSystem : SharedSunShadowSystem
{
    [Dependency] private readonly ClientGameTicker _ticker = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly MetaDataSystem _metadata = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        var mapQuery = AllEntityQuery<SunShadowCycleComponent, SunShadowComponent>();
        while (mapQuery.MoveNext(out var uid, out var cycle, out var shadow))
        {
            if (!cycle.Running || cycle.Directions.Count == 0)
                continue;

            var pausedTime = _metadata.GetPauseTime(uid);

            var time = (float)(_timing.CurTime
                .Add(cycle.Offset)
                .Subtract(_ticker.RoundStartTimeSpan)
                .Subtract(pausedTime)
                .TotalSeconds % cycle.Duration.TotalSeconds);

            var (direction, alpha) = GetShadow((uid, cycle), time);
            shadow.Direction = direction;
            shadow.Alpha = alpha;
        }
    }

    [Pure]
    public (Vector2 Direction, float Alpha) GetShadow(Entity<SunShadowCycleComponent> entity, float time)
    {
        // So essentially the values are stored as the percentages of the total duration just so it adjusts the speed
        // dynamically and we don't have to manually handle it.
        // It will lerp from each value to the next one with angle and length handled separately.
        // RS14-start
        var directions = entity.Comp.Directions;

        if (directions.Count == 0 || entity.Comp.Duration.TotalSeconds <= 0)
            return (Vector2.Zero, 0f);

        if (directions.Count == 1)
            return (directions[0].Direction, directions[0].Alpha);

        var ratio = (float)(time / entity.Comp.Duration.TotalSeconds);
        ratio %= 1f;

        if (ratio < 0f)
            ratio += 1f;

        var index = -1;

        for (var i = directions.Count - 1; i >= 0; i--)
        {
            if (ratio >= directions[i].Ratio)
            {
                index = i;
                break;
            }
        }

        // If ratio is before the first entry, interpolate from the last entry to the first entry.
        if (index == -1)
        {
            index = directions.Count - 1;
            ratio += 1f;
        }

        var dir = directions[index];
        var nextIndex = (index + 1) % directions.Count;
        var next = directions[nextIndex];

        var nextRatio = next.Ratio;

        if (nextIndex == 0)
            nextRatio += 1f;

        var range = nextRatio - dir.Ratio;

        if (range <= 0f)
            return (dir.Direction, dir.Alpha);

        var diff = (ratio - dir.Ratio) / range;
        diff = Math.Clamp(diff, 0f, 1f);
        // RS14-end
        // We lerp angle + length separately as we don't want a straight-line lerp and want the rotation to be consistent.
        var currentAngle = dir.Direction.ToAngle();
        var nextAngle = next.Direction.ToAngle();

        var angle = Angle.Lerp(currentAngle, nextAngle, diff);
        // This is to avoid getting weird issues where the angle gets pretty close but length still noticeably catches up.
        var lengthDiff = MathF.Pow(diff, 1f / 2f);
        var length = float.Lerp(dir.Direction.Length(), next.Direction.Length(), lengthDiff);

        var vector = angle.ToVec() * length;
        var alpha = float.Lerp(dir.Alpha, next.Alpha, diff);
        return (vector, alpha);
    }
}
