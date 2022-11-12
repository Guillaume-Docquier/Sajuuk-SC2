﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips;

public class ChokeWallsClip : Clip {
    public ChokeWallsClip(Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds) : base(pauseAtEndOfClipDurationSeconds) {
        var centerCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: 0)
            .WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var visibleCells = new HashSet<Vector2>();
        for (var angle = 0; angle < 360; angle += 1) {
            var rayCastResults = RayCasting.RayCast(sceneLocation, MathUtils.DegToRad(angle), cell => !MapAnalyzer.IsWalkable(cell)).ToList();
            foreach (var rayCastResult in rayCastResults) {
                visibleCells.Add(rayCastResult.CornerOfCell.AsWorldGridCenter());
            }
        }

        var pauseAnimation = new PauseAnimation(centerCameraAnimation.AnimationEndFrame).WithDurationInSeconds(2);
        AddAnimation(pauseAnimation);

        var showWallsAnimationEndFrame = ShowCells(sceneLocation, visibleCells.Where(cell => !MapAnalyzer.IsWalkable(cell)).ToList(), pauseAnimation.AnimationEndFrame);
        ShowCells(sceneLocation, visibleCells.Where(cell => MapAnalyzer.IsWalkable(cell)).ToList(), showWallsAnimationEndFrame);
    }

    private int ShowCells(Vector2 origin, List<Vector2> cells, int startAt) {
        var maxDistance = cells.Max(cell => cell.DistanceTo(origin));
        var animationTotalDuration = TimeUtils.SecsToFrames(10);

        var endFrame = startAt;
        foreach (var cell in cells) {
            var relativeDistance = cell.DistanceTo(origin) / maxDistance;
            var startFrame = startAt + (int)(relativeDistance * animationTotalDuration);

            var squareAnimation = new CellDrawingAnimation(cell.ToVector3(), startFrame).WithDurationInSeconds(1f);
            AddAnimation(squareAnimation);

            endFrame = Math.Max(endFrame, squareAnimation.AnimationEndFrame);
        }

        return endFrame;
    }
}