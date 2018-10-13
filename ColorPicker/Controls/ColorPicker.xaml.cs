using SkiaSharp;
using SkiaSharp.Views.Forms;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace CoBa.Xam.Controls
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class ColorPicker : ContentView
    {
        private int width;
        private int height;
        private double h;
        private double s;
        private double l;

        private SKBitmap background;

        public event EventHandler StartSelecting;
        public event EventHandler StopSelecting;
        public event EventHandler ColorChanged;

        private SKPaint solidBlack = new SKPaint()
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        private SKPaint solidInternal = new SKPaint()
        {
            Color = SKColors.Red,
            Style = SKPaintStyle.Fill,
            IsAntialias = true,
        };

        public ColorPicker()
        {
            InitializeComponent();

            // Read the background image from the resources
            Assembly assembly = typeof(ColorPicker).GetTypeInfo().Assembly;
            Debug.WriteLine(string.Join(",",assembly.GetManifestResourceNames()));
            Stream image = typeof(ColorPicker).GetTypeInfo().Assembly.GetManifestResourceStream("CoBa.Xam.spectrum.png");
            background = SKBitmap.Decode(image);

            // Tell the SKCanvas to update with 30 FPS
            Device.StartTimer(TimeSpan.FromSeconds(1f / 30), () =>
            {
                page_Canvas.InvalidateSurface();
                return true;
            });
        }

        public static readonly BindableProperty ColorProperty =
            BindableProperty.Create(nameof(Color), typeof(Color), typeof(ColorPicker), Color.Red);

        /// <summary>
        /// Gets or sets the selected color of the ColorPicker
        /// </summary>
        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => ParseHSLColor(value);
        }

        private void ParseHSLColor(Color color)
        {
            this.h = color.Hue;
            this.s = color.Saturation;
            this.l = color.Luminosity;

            SetColor();
        }

        private void ParseLocationToHSLColor(Point location)
        {
            // If the location is out of bounds, cap it to the edges
            if (location.X < 0) location.X = 0;
            if (location.X > width) location.X = width;
            if (location.Y < 0) location.Y = 0;
            if (location.Y > height) location.Y = height;

            this.h = 1.0 / width * location.X;
            this.s = 1.0;
            this.l = 1.0 - (0.5 / height * location.Y);

            SetColor();
        }

        private void SetColor()
        {
            // Set the property to the 'ouside' to the correct color
            SetValue(ColorProperty, Color.FromHsla(h, s, l));

            // Update the SKColor for the inner picker circle
            float sk_h = 360f * (float)this.h;
            float sk_s = 100f * (float)this.s;
            float sk_l = 100f * (float)this.l;

            solidInternal.Color = SKColor.FromHsl(sk_h, sk_s, sk_l);
        }

        private void SKCanvasView_Touch(object sender, SKTouchEventArgs e)
        {
            // When the color picker is touched, check if the input device is a mouse.
            // If the input device is a mouse we require a left mouse button
            if (e.DeviceType == SKTouchDeviceType.Mouse && e.MouseButton != SKMouseButton.Left)
            {
                return;
            }

            // If the current action is pressed (start of selection) trigger the event
            if (e.ActionType == SKTouchAction.Pressed)
            {
                OnTouchEnter(new Point(e.Location.X, e.Location.Y));
            }
            // If the current action is released (stop of seleting) trigger the event
            else if (e.ActionType == SKTouchAction.Released)
            {
                OnTouchLeave(new Point(e.Location.X, e.Location.Y));
            }
            // If the selector is moved from it's last position, update the color
            else if (e.ActionType == SKTouchAction.Moved)
            {
                OnTouchMove(new Point(e.Location.X, e.Location.Y));
            }

            // Tell the OS we are interested in updates of the touch input
            e.Handled = true;
        }

        private void OnTouchEnter(Point location)
        {
            ParseLocationToHSLColor(location);
            StartSelecting?.Invoke(this, EventArgs.Empty);
        }

        private void OnTouchLeave(Point location)
        {
            ParseLocationToHSLColor(location);
            StopSelecting?.Invoke(this, EventArgs.Empty);
        }

        private void OnTouchMove(Point location)
        {
            ParseLocationToHSLColor(location);
            ColorChanged?.Invoke(this, EventArgs.Empty);
        }

        private void SKCanvasView_PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            // Get the canvas to pain onto
            SKCanvas canvas = e.Surface.Canvas;

            // Clear the canvas from it's last drawing
            canvas.Clear(SKColors.Empty);

            // Get and store the width and height of the canvas in 'pixels'
            width = e.Info.Width;
            height = e.Info.Height;

            int smallest = Math.Min(width, height);

            // Using the Hue and Luminosity calculate the current position of the picker circle within the control
            float x = width * (float)h;
            float y = height * (float)(1 - (l - 0.5) * 2);

            // Cap the picker so it doesn't go outside the control
            if (x < 0) x = 0;
            if (x > width) x = width;
            if (y < 0) y = 0;
            if (y > height) y = height;

            // Draw the background bitmap (loaded from resources)
            canvas.DrawBitmap(background, new SKRect(0, 0, width, height));

            // First draw a solid black circle and over it draw a smaller circle with the current selected color
            canvas.DrawCircle(x, y, smallest / 20, solidBlack);
            canvas.DrawCircle(x, y, smallest / 32, solidInternal);
        }
    }
}