using System;
using System.Collections.Generic;
using System.Numerics;
using Sajuuk.ExtensionMethods;

namespace Sajuuk.Algorithms;

public static class RayCasting {
    public struct RayCastResult {
        public Vector2 CornerOfCell;
        public Vector2 RayIntersection;
    }

    /// <summary>
    /// Ray cast from an origin at an angle in radians until the provided condition is met.
    /// 0 degrees would be ray casting in the Y axis (12 o'clock).
    /// A positive angle will result in a counter clockwise rotation.
    /// </summary>
    /// <param name="origin">The origin of the ray</param>
    /// <param name="angleInRadians">The angle to ray cast</param>
    /// <param name="shouldStopRay">A function that receives the current cell corner to decide if we should stop ray casting</param>
    /// <returns>All the crossed cells and their intersection point when the ray entered the cell. The origin and the last cell are included.</returns>
    public static IEnumerable<RayCastResult> RayCast(Vector2 origin, double angleInRadians, Func<Vector2, bool> shouldStopRay) {
        var direction = origin
            .Translate(yTranslation: 1)
            .RotateAround(origin, angleInRadians);

        return RayCast(origin, direction, shouldStopRay);
    }

    /// <summary>
    /// Ray cast from an origin towards another point until the provided condition is met.
    /// </summary>
    /// <param name="origin">The origin of the ray</param>
    /// <param name="direction">A point to ray cast towards</param>
    /// <param name="shouldStopRay">A function that receives the current cell corner to decide if we should stop ray casting</param>
    /// <returns>All the crossed cells and their intersection point when the ray entered the cell. The origin and the last cell are included.</returns>
    public static IEnumerable<RayCastResult> RayCast(Vector2 origin, Vector2 direction, Func<Vector2, bool> shouldStopRay) {
        var delta = Vector2.Normalize(direction - origin);

        var dxdy = delta.Y == 0 ? 0 : delta.X / delta.Y;
        var dydx = delta.X == 0 ? 0 : delta.Y / delta.X;

        // If delta X is 0, we set the rayLength to a big one so that the Y ray is always chosen (we're moving straight up or straight down)
        var rayLengthWhenMovingInX = float.MaxValue;
        if (delta.X != 0) {
            rayLengthWhenMovingInX = (float)Math.Sqrt(1 + dydx * dydx);
        }

        // If delta Y is 0, we set the rayLength to a big one so that the X ray is always chosen (we're moving straight left or straight right)
        var rayLengthWhenMovingInY = float.MaxValue;
        if (delta.Y != 0) {
            rayLengthWhenMovingInY = (float)Math.Sqrt(1 + dxdy * dxdy);
        }

        // Edge case, if deltaX is 0, stepXDistance can be 0, making the first ray 0, thus it's going to be picked
        // We want to avoid that so we set it to 1
        var stepX = 1;
        var stepXDistance = 1f;
        if (delta.X > 0) {
            // Moving right
            stepX = 1;
            stepXDistance = (float)Math.Floor(origin.X + 1) - origin.X;
        }
        else if (delta.X < 0) {
            // Moving left
            stepX = -1;
            stepXDistance = origin.X - (float)Math.Floor(origin.X);
        }

        // Edge case, if deltaY is 0, stepYDistance can be 0, making the first ray 0, thus it's going to be picked
        // We want to avoid that so we set it to 1
        var stepY = 1;
        var stepYDistance = 1f;
        if (delta.Y > 0) {
            // Moving up
            stepY = 1;
            stepYDistance = (float)Math.Floor(origin.Y + 1) - origin.Y;
        }
        else if (delta.Y < 0) {
            // Moving down
            stepY = -1;
            stepYDistance = origin.Y - (float)Math.Floor(origin.Y);
        }

        var lastIntersection = origin;
        var currentCell = origin.AsWorldGridCorner();
        var xRayLength = stepXDistance * rayLengthWhenMovingInX;
        var yRayLength = stepYDistance * rayLengthWhenMovingInY;

        yield return new RayCastResult
        {
            CornerOfCell = new Vector2(currentCell.X, currentCell.Y),
            RayIntersection = lastIntersection,
        };

        while (!shouldStopRay(currentCell)) {
            if (xRayLength < yRayLength) {
                // Step in X, reduce Y ray
                yRayLength -= xRayLength;

                // Move to the cell on the left or right
                currentCell.X += stepX;
                lastIntersection += delta * xRayLength;

                // Due to float imprecision, we get some rounding errors
                // We can round X to the closest int to eliminate the error because we know we stepped in X (int, float)
                lastIntersection.X = (float)Math.Round(lastIntersection.X);

                // Reset X ray
                xRayLength = rayLengthWhenMovingInX;
            }
            else if (yRayLength < xRayLength) {
                // Step in Y, reduce X ray
                xRayLength -= yRayLength;

                // Move to the cell on the bottom or top
                currentCell.Y += stepY;
                lastIntersection += delta * yRayLength;

                // Due to float imprecision, we get some rounding errors
                // We can round Y to the closest int to eliminate the error because we know we stepped in Y (float, int)
                lastIntersection.Y = (float)Math.Round(lastIntersection.Y);

                // Reset Y ray
                yRayLength = rayLengthWhenMovingInY;
            }
            else {
                // Both rays are the same, means we landed exactly on a corner
                currentCell.X += stepX; // Move to the left/right
                currentCell.Y += stepY; // And up/down
                lastIntersection += delta * yRayLength; // xRayLength and yRayLength are the same, doesn't matter which one we pick

                // Due to float imprecision, we get some rounding errors
                // We can round to the closest int to eliminate the error because we know we are a at corner (int, int)
                lastIntersection.X = (float)Math.Round(lastIntersection.X);
                lastIntersection.Y = (float)Math.Round(lastIntersection.Y);

                // Reset all rays
                xRayLength = rayLengthWhenMovingInX;
                yRayLength = rayLengthWhenMovingInY;
            }

            yield return new RayCastResult
            {
                CornerOfCell = new Vector2(currentCell.X, currentCell.Y),
                RayIntersection = lastIntersection,
            };
        }
    }
}
