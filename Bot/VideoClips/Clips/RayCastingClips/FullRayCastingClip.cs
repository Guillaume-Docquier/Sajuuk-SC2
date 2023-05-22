using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.Debugging.GraphicalDebugging;
using Bot.ExtensionMethods;
using Bot.GameSense;
using Bot.Requests;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class FullRayCastingClip : Clip {
    private readonly ITerrainTracker _terrainTracker;
    private readonly IGraphicalDebugger _graphicalDebugger;

    public FullRayCastingClip(
        ITerrainTracker terrainTracker,
        IGraphicalDebugger graphicalDebugger,
        IController controller,
        IRequestBuilder requestBuilder,
        IRequestService requestService,
        Vector2 sceneLocation,
        int pauseAtEndOfClipDurationSeconds = 5
    ) : base(pauseAtEndOfClipDurationSeconds) {
        _terrainTracker = terrainTracker;
        _graphicalDebugger = graphicalDebugger;

        var centerCameraAnimation = new CenterCameraAnimation(controller, requestBuilder, requestService, sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        CastAllRays(sceneLocation, centerCameraAnimation.AnimationEndFrame);
    }

    private void CastAllRays(Vector2 origin, int startAt) {
        var sphereDrawingAnimation = new SphereDrawingAnimation(_graphicalDebugger, _terrainTracker.WithWorldHeight(origin), 0.5f, ColorService.Instance.PointColor, startAt).WithDurationInSeconds(0.5f);
        AddAnimation(sphereDrawingAnimation);

        for (var angle = 0; angle < 360; angle += 2) {
            var rayCastResults = RayCasting.RayCast(origin, MathUtils.DegToRad(angle), cell => !_terrainTracker.IsWalkable(cell)).ToList();

            var previousIntersection = rayCastResults[0].RayIntersection;
            var previousAnimationEnd = sphereDrawingAnimation.AnimationEndFrame + (int)TimeUtils.SecsToFrames((float)angle / 360 * 2);
            foreach (var rayCastResult in rayCastResults.Skip(1)) {
                var rayEnd = rayCastResult.RayIntersection;
                var lineDrawingAnimation = new LineDrawingAnimation(_graphicalDebugger, _terrainTracker.WithWorldHeight(previousIntersection), _terrainTracker.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, previousAnimationEnd, thickness: 1)
                    .WithConstantRate(4);

                AddAnimation(lineDrawingAnimation);

                if (!_terrainTracker.IsWalkable(rayCastResult.CornerOfCell)) {
                    var squareAnimation = new CellDrawingAnimation(_terrainTracker, _graphicalDebugger, _terrainTracker.WithWorldHeight(rayCastResult.CornerOfCell.AsWorldGridCenter()), lineDrawingAnimation.AnimationEndFrame)
                        .WithDurationInSeconds(0.5f);

                    AddAnimation(squareAnimation);
                }

                previousIntersection = rayCastResult.RayIntersection;
                previousAnimationEnd = lineDrawingAnimation.AnimationEndFrame;
            }
        }
    }
}
