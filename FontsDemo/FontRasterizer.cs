using System;
using System.IO;
using System.Runtime.InteropServices;
using SharpFont;
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
        private const int DPI = 72;
        private const int MinimumSide = 1;

        public static void RenderTextWithSixLabors(
            Font font, string text, string outputPath, Rgba32? backgroundColor = null)
        {
            if (font == null)
            {
                return;
            }

            var style = new RendererOptions(font, DPI);
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
                SaveImage(outputPath, render);
            }
        }

        public static unsafe void RenderTextWithSharpFont(
            FontFace font, char @char, string outputPath, Rgba32? backgroundColor = null)
        {
            if (font == null)
            {
                return;
            }

            var glyph = font.GetGlyph(@char, DPI);

            if (glyph == null)
            {
                return;
            }

            var surface = new Surface
            {
                Bits = Marshal.AllocHGlobal(glyph.RenderWidth * glyph.RenderHeight),
                Width = glyph.RenderWidth,
                Height = glyph.RenderHeight,
                Pitch = glyph.RenderWidth
            };

            var stuff = (byte*)surface.Bits;
            var surfaceArea = surface.Width * surface.Height;

            for (var i = 0; i < surfaceArea; i++)
            {
                *stuff++ = 0;
            }

            glyph.RenderTo(surface);

            var outputImage = Translate(surface);
            SaveImage(outputPath, outputImage);
        }

        private static void SaveImage(string outputPath, Image<Rgba32> image)
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }

            using (var stream = File.Create(outputPath))
            {
                image.SaveAsPng(stream);
            }
        }

        private static Image<Rgba32> Translate(Surface surface)
        {
            if (surface.Width == 0 || surface.Height == 0)
            {
                return null;
            }

            var width = surface.Width;
            var height = surface.Height;
            var length = width * height;
            var data = new byte[length];
            Marshal.Copy(surface.Bits, data, 0, length);
            var pixels = new byte[length * 4];

            for (var i = 0; i < length; i++)
            {
                var c = data[i];

                var index = 0;
                pixels[index++] = c;
                pixels[index++] = c;
                pixels[index++] = c;
                // TODO alpha channel
            }

            var image = Image.LoadPixelData<Rgba32>(pixels, width, height);
            Marshal.FreeHGlobal(surface.Bits); //Give the memory back!

            return image;
        }
    }
}
