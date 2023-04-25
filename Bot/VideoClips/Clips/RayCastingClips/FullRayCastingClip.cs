using System.Linq;
using System.Numerics;
using Bot.Algorithms;
using Bot.ExtensionMethods;
using Bot.MapKnowledge;
using Bot.Utils;
using Bot.VideoClips.Manim.Animations;

namespace Bot.VideoClips.Clips.RayCastingClips;

public class FullRayCastingClip : Clip {
    private readonly IMapAnalyzer _mapAnalyzer;

    public FullRayCastingClip(IMapAnalyzer mapAnalyzer, Vector2 sceneLocation, int pauseAtEndOfClipDurationSeconds = 5): base(pauseAtEndOfClipDurationSeconds) {
        _mapAnalyzer = mapAnalyzer;

        var centerCameraAnimation = new CenterCameraAnimation(sceneLocation, startFrame: 0).WithDurationInSeconds(1);
        AddAnimation(centerCameraAnimation);

        CastAllRays(sceneLocation, centerCameraAnimation.AnimationEndFrame);
    }

    private void CastAllRays(Vector2 origin, int startAt) {
        var sphereDrawingAnimation = new SphereDrawingAnimation(_mapAnalyzer.WithWorldHeight(origin), 0.5f, ColorService.Instance.PointColor, startAt).WithDurationInSeconds(0.5f);
        AddAnimation(sphereDrawingAnimation);

        for (var angle = 0; angle < 360; angle += 2) {
            var rayCastResults = RayCasting.RayCast(origin, MathUtils.DegToRad(angle), cell => !_mapAnalyzer.IsWalkable(cell)).ToList();

            var previousIntersection = rayCastResults[0].RayIntersection;
            var previousAnimationEnd = sphereDrawingAnimation.AnimationEndFrame + (int)TimeUtils.SecsToFrames((float)angle / 360 * 2);
            foreach (var rayCastResult in rayCastResults.Skip(1)) {
                var rayEnd = rayCastResult.RayIntersection;
                var lineDrawingAnimation = new LineDrawingAnimation(_mapAnalyzer.WithWorldHeight(previousIntersection), _mapAnalyzer.WithWorldHeight(rayEnd), ColorService.Instance.RayColor, previousAnimationEnd, thickness: 1)
                    .WithConstantRate(4);

                AddAnimation(lineDrawingAnimation);

                if (!_mapAnalyzer.IsWalkable(rayCastResult.CornerOfCell)) {
                    var squareAnimation = new CellDrawingAnimation(_mapAnalyzer, _mapAnalyzer.WithWorldHeight(rayCastResult.CornerOfCell.AsWorldGridCenter()), lineDrawingAnimation.AnimationEndFrame)
                        .WithDurationInSeconds(0.5f);

                    AddAnimation(squareAnimation);
                }

                previousIntersection = rayCastResult.RayIntersection;
                previousAnimationEnd = lineDrawingAnimation.AnimationEndFrame;
            }
        }
    }
}
