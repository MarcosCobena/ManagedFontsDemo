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
        private Font _font;

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
            _inputTextBox.TextInput += UpdateRenderImage;

            _outputImage = this.FindControl<Image>("OutputImage");
        }

        private void UpdateFont(object sender, SelectionChangedEventArgs e)
        {
            var dropDown = sender as DropDown;
            var fontFamily = dropDown.SelectedItem as FontFamily;
            _font = fontFamily.CreateFont(DefaultFontSize);

            PopulateGlyphs();
            UpdateRenderImage(_inputTextBox, null);
        }

        private void PopulateFonts()
        {
            Task.Run(() =>
            {
                var fonts = new FontCollection();

                var fontPaths = Directory.EnumerateFiles("/Library/Fonts", "*.ttf");

                foreach (var item in fontPaths)
                {
                    try
                    {
                        var fontFamily = fonts.Install(item);
                    }
                    catch (InvalidFontFileException)
                    {
                        Trace.WriteLine($"Ops, I couldn't load {item}");
                    }
                }

                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _fontsDropDown.Items = fonts.Families;
                    var firstFontFamily = fonts.Families.FirstOrDefault();
                    _fontsDropDown.SelectedItem = firstFontFamily;
                });
            });
        }

        private void PopulateGlyphs()
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

                foreach (var item in _asciiChars)
                {
                    var safeFileName = $"{(byte)item}.png";
                    var outputPath = Path.Combine(outputDirectory, safeFileName);
                    FontRasterizer.RenderText(_font, item.ToString(), outputPath, Rgba32.White);
                    var bitmap = new Bitmap(outputPath);
                    var glyph = Tuple.Create(item, bitmap);
                    glyphs.Add(glyph);
                }

                Dispatcher.UIThread.InvokeAsync(() => _glyphsListBox.Items = glyphs);
            });
        }

        private void UpdateRenderImage(object sender, EventArgs e)
        {
            var textBox = sender as TextBox;
            var text = textBox.Text;

            Task.Run(() =>
            {
                FontRasterizer.RenderText(_font, text, _renderOutputPath);

                Dispatcher.UIThread.InvokeAsync(() => _outputImage.Source = new Bitmap(_renderOutputPath));
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private static string GetASCIIChars() => 
            string.Concat(Enumerable.Range(0, 256).Select(index => (char)index));
    }
}