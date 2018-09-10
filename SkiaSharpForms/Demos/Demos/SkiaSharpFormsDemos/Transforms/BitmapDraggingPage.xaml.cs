using System;
using System.IO;
using System.Reflection;

using Xamarin.Forms;

using SkiaSharp;
using SkiaSharp.Views.Forms;

using TouchTracking;

namespace SkiaSharpFormsDemos.Transforms
{
    public partial class BitmapDraggingPage : ContentPage
    {
        // Bitmap and matrix for display
        SKBitmap bitmap;
        SKMatrix matrix = SKMatrix.MakeIdentity();

        // Touch information
        long touchId = -1;
        SKPoint previousPoint;

        public BitmapDraggingPage()
        {
            InitializeComponent();

            string resourceID = "SkiaSharpFormsDemos.Media.SeatedMonkey.jpg";
            Assembly assembly = GetType().GetTypeInfo().Assembly;

            using (Stream stream = assembly.GetManifestResourceStream(resourceID))
            {
                bitmap = SKBitmap.Decode(stream);
            }
        }

        void OnTouchEffectAction(object sender, TouchActionEventArgs args)
        {
            // Convert Xamarin.Forms point to pixels
            Point pt = args.Location;
            SKPoint point = 
                new SKPoint((float)(canvasView.CanvasSize.Width * pt.X / canvasView.Width),
                            (float)(canvasView.CanvasSize.Height * pt.Y / canvasView.Height));

            switch (args.Type)
            {
                case TouchActionType.Pressed:
                    // Find transformed bitmap rectangle
                    SKRect rect = new SKRect(0, 0, bitmap.Width, bitmap.Height);
                    rect = matrix.MapRect(rect);

                    // Determine if the touch was within that rectangle
                    if (rect.Contains(point))
                    {
                        touchId = args.Id;
                        previousPoint = point;
                    }
                    break;

                case TouchActionType.Moved:
                    if (touchId == args.Id)
                    {
                        // Adjust the matrix for the new position
                        matrix.TransX += point.X - previousPoint.X;
                        matrix.TransY += point.Y - previousPoint.Y;
                        previousPoint = point;
                        canvasView.InvalidateSurface();
                    }
                    break;

                case TouchActionType.Released:
                case TouchActionType.Cancelled:
                    touchId = -1;
                    break;
            }
        }

        void OnCanvasViewPaintSurface(object sender, SKPaintSurfaceEventArgs args)
        {
            SKImageInfo info = args.Info;
            SKSurface surface = args.Surface;
            SKCanvas canvas = surface.Canvas;

            canvas.Clear();

            // Display the bitmap
            canvas.SetMatrix(matrix);
            canvas.DrawBitmap(bitmap, new SKPoint());
        }
    }
}
