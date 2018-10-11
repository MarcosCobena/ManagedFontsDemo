using System;
using System.IO;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SixLabors.Primitives;
using SixLabors.Shapes;

namespace FontsDemo
{
    public static class FontRasterizer
    {
        private const int MinimumSide = 1;

        public static void RenderText(
            Font font, string text, string outputPath, Rgba32? backgroundColor = null)
        {
            if (font == null)
            {
                return;
            }

            var style = new RendererOptions(font, 72);
            var rawShapes = TextBuilder.GenerateGlyphs(text, new PointF(0, 0), style);
            var shapes = rawShapes.Translate(-rawShapes.Bounds.X, -rawShapes.Bounds.Y);

            const int safetyFrame = 0;
            var bounds = shapes.Bounds;
            var width = (int)Math.Ceiling(bounds.Width + safetyFrame);
            var height = (int)Math.Ceiling(bounds.Height + safetyFrame);

            if (width <= 0)
            {
                width = MinimumSide;
            }

            if (height <= 0)
            {
                height = MinimumSide;
            }

            using (var render = new Image<Rgba32>(width, height))
            {
                var actualBackgroundColor = backgroundColor ?? Rgba32.Transparent;
                render.Mutate(operation => operation.Fill(actualBackgroundColor));
                render.Mutate(operation => operation.Fill(Rgba32.Black, shapes));

                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                using (var stream = File.Create(outputPath))
                {
                    render.SaveAsPng(stream);
                }
            }
        }
    }
}
