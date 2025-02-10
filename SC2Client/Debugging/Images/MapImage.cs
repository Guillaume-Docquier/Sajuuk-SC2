using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Numerics;
using SC2Client.ExtensionMethods;

namespace SC2Client.Debugging.Images;

/// <summary>
/// Creates an image that represents the map
/// This only works on Windows because we use System.Drawing.Bitmap
/// </summary>
[SuppressMessage("Interoperability", "CA1416:Validate platform compatibility")]
public class MapImage : IMapImage {
    private readonly ILogger _logger;
    private readonly Bitmap _image;

    /// <summary>
    /// Creates an image that can be used to represent an SC2 map.
    /// The map image is black by default.
    /// </summary>
    /// <param name="logger"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public MapImage(ILogger logger, int width, int height) {
        _logger = logger;

        _image = new Bitmap(width, height);
        for (var x = 0; x < _image.Width; x++) {
            for (var y = 0; y < _image.Height; y++) {
                _image.SetPixel(x, y, Color.Black);
            }
        }
    }

    public IMapImage SetCellsColor(IEnumerable<Vector2> cells, Color color) {
        foreach (var cell in cells) {
            SetCellColor(cell, color);
        }

        return this;
    }

    public IMapImage SetCellColor(Vector2 cell, Color color) {
        var adjustedCell = cell.AsWorldGridCorner();

        return SetCellColor((int)adjustedCell.X, (int)adjustedCell.Y, color);
    }

    public IMapImage SetCellColor(int x, int y, Color color) {
        _image.SetPixel(x, y, color);

        return this;
    }

    public void Save(string fileName, int upscalingFactor = 4) {
        var scaledImage = ScaleImage(_image, upscalingFactor);
        // SC2 (0, 0) is bottom left, but bitmap (0, 0) is top left, so we flip to end up with the correct image orientation
        scaledImage.RotateFlip(RotateFlipType.RotateNoneFlipY);

        var fileNameWithExtension = $"{fileName}.{FileExtensions.Png}";
        Directory.CreateDirectory(Path.GetDirectoryName(fileNameWithExtension)!);
        scaledImage.Save(fileNameWithExtension);
        _logger.Success($"Map image saved to {fileNameWithExtension}");
    }

    /// <summary>
    /// Scales the image so that it is bigger.
    /// This method only works on windows.
    /// </summary>
    /// <param name="originalImage">The original image.</param>
    /// <param name="scalingFactor">A scaling multiplier to indicate how much to scale the image.</param>
    /// <returns>The new, scaled, image.</returns>
    private static Bitmap ScaleImage(Bitmap originalImage, int scalingFactor) {
        var scaledWidth = originalImage.Width * scalingFactor;
        var scaledHeight = originalImage.Height * scalingFactor;
        var scaledImage = new Bitmap(scaledWidth, scaledHeight);

        for (var x = 0; x < originalImage.Width; x++) {
            for (var y = 0; y < originalImage.Height; y++) {
                for (var virtualX = 0; virtualX < scalingFactor; virtualX++) {
                    for (var virtualY = 0; virtualY < scalingFactor; virtualY++) {
                        scaledImage.SetPixel(
                            x * scalingFactor + virtualX,
                            y * scalingFactor + virtualY,
                            originalImage.GetPixel(x, y)
                        );
                    }
                }
            }
        }

        return scaledImage;
    }
}
