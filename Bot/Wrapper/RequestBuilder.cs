using System.Collections.Generic;
using SC2APIProtocol;

namespace Bot.Wrapper;

public static class RequestBuilder {
    public static Request ActionRequest(IEnumerable<Action> actions) {
        var actionRequest = new Request
        {
            Action = new RequestAction()
        };
        actionRequest.Action.Actions.AddRange(actions);

        return actionRequest;
    }

    public static Request StepRequest(uint stepSize) {
        return new Request
        {
            Step = new RequestStep
            {
                Count = stepSize
            }
        };
    }

    public static Request DebugRequest(IEnumerable<DebugText> debugTexts, IEnumerable<DebugSphere> debugSpheres, IEnumerable<DebugBox> debugBoxes, IEnumerable<DebugLine> debugLines) {
        return new Request
        {
            Debug = new RequestDebug
            {
                Debug =
                {
                    new DebugCommand
                    {
                        Draw = new DebugDraw
                        {
                            Text = { debugTexts },
                            Spheres = { debugSpheres },
                            Boxes = { debugBoxes },
                            Lines = { debugLines },
                        },
                    },
                }
            }
        };
    }
}
