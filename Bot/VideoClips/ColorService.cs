using Bot.Debugging.GraphicalDebugging;
using SC2APIProtocol;

namespace Bot.VideoClips;

public class ColorService {
    public static readonly ColorService Instance = new ColorService();

    public Color PointColor { get; private set;} = Colors.Purple;
    public Color RayColor { get; private set;} = Colors.Green;
    public Color UnwalkableCellColor { get; private set;} = Colors.LightRed;
    public Color WalkableCellColor { get; private set;} = Colors.CornflowerBlue;

    private ColorService() {}

    public static void SetMap(string mapName) {
        switch (mapName) {
            case Maps.Season_2022_4.FileNames.Stargazers:
                Instance.PointColor = Colors.Purple;
                Instance.RayColor = Colors.Green;
                Instance.UnwalkableCellColor = Colors.LightRed;
                Instance.WalkableCellColor = Colors.CornflowerBlue;
                break;
            case Maps.Season_2022_4.FileNames.CosmicSapphire:
                Instance.PointColor = Colors.Magenta;
                Instance.RayColor = Colors.Green;
                Instance.UnwalkableCellColor = Colors.Red;
                Instance.WalkableCellColor =  Colors.Blue;
                break;
            case Maps.Season_2022_4.FileNames.Hardwire:
                Instance.PointColor = Colors.Magenta;
                Instance.RayColor = Colors.Green;
                Instance.UnwalkableCellColor = Colors.BrightRed;
                Instance.WalkableCellColor =  Colors.BrightBlue;
                break;
        }
    }
}
