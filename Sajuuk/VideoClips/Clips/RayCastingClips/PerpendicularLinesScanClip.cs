﻿using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Sajuuk.Algorithms;
using Sajuuk.Debugging.GraphicalDebugging;
using Sajuuk.GameSense;
using Sajuuk.Utils;
using Sajuuk.VideoClips.Manim.Animations;
using SC2APIProtocol;

namespace Sajuuk.VideoClips.Clips.RayCastingClips;

public class PerpendicularLinesScanClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IAnimationFactory _animationFactory;

    public PerpendicularLinesScanClip(
        ITerrainTracker terrainTracker,
        IAnimationFactory animationFactory,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds
    ) : base(animationFactory, pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _animationFactory = animationFactory;

        var centerCameraAnimation = _animationFactory.CreateCenterCameraAnimation(sceneLocation, startFrame: 0)
            .WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        var locationAnimation = _animationFactory.CreateCellDrawingAnimation(_terrainTracker.WithWorldHeight(sceneLocation), centerCameraAnimation.AnimationEndFrame)
            .WithDurationInSeconds(1);
        AddAnimation(locationAnimation);

        var pauseAnimation = _animationFactory.CreatePauseAnimation(locationAnimation.AnimationEndFrame)
            .WithDurationInSeconds(2);
        AddAnimation(pauseAnimation);

        ScanTerrainWithLines(sceneLocation, pauseAnimation.AnimationEndFrame);
    }

    private void ScanTerrainWithLines(Vector2 location, int startFrame) {
        var previousAnimationEndFrame = startFrame;
        for (var angle = 0; angle <= 180; angle += 1) {
            previousAnimationEndFrame = DrawCross(location, angle, previousAnimationEndFrame);
        }
    }

    private int DrawCross(Vector2 origin, double angle, int startFrame) {
        DrawRaySegments(RayCasting.RayCast(origin, MathUtils.DegToRad(angle), cell => !_terrainTracker.IsWalkable(cell)).ToList(), startFrame, Colors.BrightGreen);
        DrawRaySegments(RayCasting.RayCast(origin, MathUtils.DegToRad(angle + 90), cell => !_terrainTracker.IsWalkable(cell)).ToList(), startFrame, Colors.Red);
        DrawRaySegments(RayCasting.RayCast(origin, MathUtils.DegToRad(angle + 180), cell => !_terrainTracker.IsWalkable(cell)).ToList(), startFrame, Colors.BrightGreen);

        return DrawRaySegments(RayCasting.RayCast(origin, MathUtils.DegToRad(angle + 270), cell => !_terrainTracker.IsWalkable(cell)).ToList(), startFrame, Colors.Red);
    }

    private int DrawRaySegments(List<RayCasting.RayCastResult> rayCastResults, int startFrame, Color color) {
        var previousRayEnd = rayCastResults[0].RayIntersection;
        foreach (var rayCastResult in rayCastResults) {
            DrawRay(previousRayEnd, rayCastResult.RayIntersection, startFrame, color);
            previousRayEnd = rayCastResult.RayIntersection;
        }

        return startFrame + 1; // Yeah I'm lazy at this point
    }

    private int DrawRay(Vector2 origin, Vector2 destination, int startFrame, Color color) {
        var lineDrawingAnimation = _animationFactory.CreateLineDrawingAnimation(_terrainTracker.WithWorldHeight(origin), _terrainTracker.WithWorldHeight(destination), color, startFrame)
            .WithPostAnimationDurationInFrames(1);

        AddAnimation(lineDrawingAnimation);

        return lineDrawingAnimation.PostAnimationEndFrame;
    }
}
