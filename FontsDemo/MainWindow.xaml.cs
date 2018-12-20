using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using SixLabors.Fonts;
using SixLabors.Fonts.Exceptions;
using SixLabors.ImageSharp.PixelFormats;

namespace FontsDemo
{
    public class MainWindow : Window
    {
        private const int DefaultFontSize = 128;
        private const string GlyphsDirectoryName = "Glyphs";
        private const string RenderFileName = "Render.png";

        private readonly string _asciiChars;
        private readonly string _workingDirectory;
        private readonly string _renderOutputPath;

        private DropDown _fontsDropDown;
        private ListBox _glyphsListBox;
        private TextBox _inputTextBox;
        private Image _outputImage;
        private Font _fontSixLabors;
        private SharpFont.FontFace _fontSharpFont;

        public MainWindow()
        {
            var location = Assembly.GetEntryAssembly().Location;
            _workingDirectory = Path.GetDirectoryName(location);
            _renderOutputPath = Path.Combine(_workingDirectory, RenderFileName);

            InitializeComponent();
            InitializeControls();

            _asciiChars = GetASCIIChars();
            _inputTextBox.Text = "Wave Engine :-)";
            PopulateFonts();
        }

        private void InitializeControls()
        {
            _fontsDropDown = this.FindControl<DropDown>("FontsDropDown");
            _fontsDropDown.SelectionChanged += UpdateFont;

            _glyphsListBox = this.FindControl<ListBox>("GlyphsListBox");

            _inputTextBox = this.FindControl<TextBox>("InputTextBox");
            _inputTextBox.KeyDown += UpdateRenderImage;

            _outputImage = this.FindControl<Image>("OutputImage");
        }

        private void UpdateFont(object sender, SelectionChangedEventArgs e)
        {
            _glyphsListBox.Items = null;

            var dropDown = sender as DropDown;
            PopulateGlyphs(dropDown.SelectedItem as string);
            UpdateRenderImage(_inputTextBox, null);
        }

        private void PopulateFonts()
        {
            var fontPaths = Directory.EnumerateFiles("/Library/Fonts", "*.ttf");
            _fontsDropDown.Items = fontPaths;
            var firstFont = fontPaths.FirstOrDefault();
            _fontsDropDown.SelectedItem = firstFont;
        }

        private void PopulateGlyphs(string fontPath)
        {
            Task.Run(() =>
            {
                var outputDirectory = Path.Combine(_workingDirectory, GlyphsDirectoryName);

                if (Directory.Exists(outputDirectory))
                {
                    Directory.Delete(outputDirectory, true);
                }

                Directory.CreateDirectory(outputDirectory);

                var glyphs = new List<object>();
                //LoadFontThroughSixLabors(fontPath);
                LoadFontThroughSharpFont(fontPath);

                foreach (var item in _asciiChars)
                {
                    var safeFileName = $"{(byte)item}.png";
                    var outputPath = Path.Combine(outputDirectory, safeFileName);
                    //FontRasterizer.RenderTextWithSixLabors(_fontSixLabors, item.ToString(), outputPath, Rgba32.White);
                    FontRasterizer.RenderTextWithSharpFont(_fontSharpFont, item, outputPath, Rgba32.White);

                    if (File.Exists(outputPath))
                    {
                        var bitmap = new Bitmap(outputPath);
                        var glyph = Tuple.Create(item, bitmap);
                        glyphs.Add(glyph);
                    }
                }

                Dispatcher.UIThread.InvokeAsync(() => _glyphsListBox.Items = glyphs);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void LoadFontThroughSharpFont(string fontPath)
        {
            try
            {
                _fontSharpFont = new SharpFont.FontFace(File.OpenRead(fontPath));
            }
            catch (SharpFont.InvalidFontException)
            {
                Trace.WriteLine($"Ops, I couldn't load {Path.GetFileName(fontPath)}");
            }
        }

        private void LoadFontThroughSixLabors(string fontPath)
        {
            var fontCollection = new FontCollection();

            try
            {
                var fontFamily = fontCollection.Install(fontPath);
                _fontSixLabors = fontFamily.CreateFont(DefaultFontSize);
            }
            catch (InvalidFontFileException)
            {
                Trace.WriteLine($"Ops, I couldn't load {Path.GetFileName(fontPath)}");
            }
        }

        private void UpdateRenderImage(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox.Text;

            Task.Run(() =>
            {
                FontRasterizer.RenderTextWithSixLabors(_fontSixLabors, text, _renderOutputPath);

                Dispatcher.UIThread.InvokeAsync(() => _outputImage.Source = new Bitmap(_renderOutputPath));
            });
        }

        private static string GetASCIIChars() => 
            string.Concat(Enumerable.Range(0, 256).Select(index => (char)index));
    }
}