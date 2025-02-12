using System.Drawing;
using System.Numerics;

namespace SC2Client.Debugging.Images;

public class NoMapImageFactory : IMapImageFactory {
    private class NoMapImage : IMapImage {
        public IMapImage SetCellsColor(IEnumerable<Vector2> cell, Color color) {
            return this;
        }

        public IMapImage SetCellColor(Vector2 cell, Color color) {
            return this;
        }

        public IMapImage SetCellColor(int x, int y, Color color) {
            return this;
        }

        public void Save(string fileName, int upscalingFactor = 4) {}
    }

    public IMapImage CreateMapImageWithTerrain() {
        return new NoMapImage();
    }
}
