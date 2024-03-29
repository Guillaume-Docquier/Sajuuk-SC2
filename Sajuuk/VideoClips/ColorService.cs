﻿using Sajuuk.Debugging.GraphicalDebugging;
using SC2APIProtocol;

namespace Sajuuk.VideoClips;

public class ColorService {
    public static readonly ColorService Instance = new ColorService();

    public Color PointColor { get; private set;} = Colors.Purple;
    public Color RayColor { get; private set;} = Colors.Green;
    public Color UnwalkableCellColor { get; private set;} = Colors.LightRed;
    public Color WalkableCellColor { get; private set;} = Colors.CornflowerBlue;

    private ColorService() {}

    public static void SetMap(string mapName) {
        switch (mapName) {
            case Maps.Stargazers:
                Instance.PointColor = Colors.Purple;
                Instance.RayColor = Colors.Green;
                Instance.UnwalkableCellColor = Colors.LightRed;
                Instance.WalkableCellColor = Colors.CornflowerBlue;
                break;
            case Maps.CosmicSapphire:
                Instance.PointColor = Colors.Magenta;
                Instance.RayColor = Colors.Green;
                Instance.UnwalkableCellColor = Colors.Red;
                Instance.WalkableCellColor =  Colors.Blue;
                break;
            case Maps.Hardwire:
                Instance.PointColor = Colors.Magenta;
                Instance.RayColor = Colors.Green;
                Instance.UnwalkableCellColor = Colors.BrightRed;
                Instance.WalkableCellColor =  Colors.BrightBlue;
                break;
        }
    }
}
