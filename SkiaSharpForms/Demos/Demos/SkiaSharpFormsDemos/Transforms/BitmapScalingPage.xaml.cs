using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

using Xamarin.Forms;

using SkiaSharp;
using SkiaSharp.Views.Forms;

using TouchTracking;

namespace SkiaSharpFormsDemos.Transforms
{
    public partial class BitmapScalingPage : ContentPage
    {
        // Bitmap and matrix for display
        SKBitmap bitmap;
        SKMatrix matrix = SKMatrix.MakeIdentity();

        // Touch information
        Dictionary<long, SKPoint> touchDictionary = new Dictionary<long, SKPoint>();

        public BitmapScalingPage()
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
                    if (rect.Contains(point) && !touchDictionary.ContainsKey(args.Id))
                    {
                        touchDictionary.Add(args.Id, point);
                    }
                    break;

                case TouchActionType.Moved:
                    if (touchDictionary.ContainsKey(args.Id))
                    {
                        // Single-finger drag
                        if (touchDictionary.Count == 1)
                        {
                            SKPoint prevPoint = touchDictionary[args.Id];

                            // Adjust the matrix for the new position
                            matrix.TransX += point.X - prevPoint.X;
                            matrix.TransY += point.Y - prevPoint.Y;
                            canvasView.InvalidateSurface();
                        }
                        // Double-finger scale and drag
                        else if (touchDictionary.Count >= 2)
                        {
                            // Copy two dictionary keys into array
                            long[] keys = new long[touchDictionary.Count];
                            touchDictionary.Keys.CopyTo(keys, 0);

                            // Find index non-moving (pivot) finger
                            int pivotIndex = (keys[0] == args.Id) ? 1 : 0;

                            // Get the three points in the transform
                            SKPoint pivotPoint = touchDictionary[keys[pivotIndex]];
                            SKPoint prevPoint = touchDictionary[args.Id];
                            SKPoint newPoint = point;

                            // Calculate two vectors
                            SKPoint oldVector = prevPoint - pivotPoint;
                            SKPoint newVector = newPoint - pivotPoint;

                            // Scaling factors are ratios of those
                            float scaleX = newVector.X / oldVector.X;
                            float scaleY = newVector.Y / oldVector.Y;

                            if (!float.IsNaN(scaleX) && !float.IsInfinity(scaleX) &&
                                !float.IsNaN(scaleY) && !float.IsInfinity(scaleY))
                            {
                                // If smething bad hasn't happened, calculate a scale and translation matrix
                                SKMatrix scaleMatrix = 
                                    SKMatrix.MakeScale(scaleX, scaleY, pivotPoint.X, pivotPoint.Y);

                                SKMatrix.PostConcat(ref matrix, scaleMatrix);
                                canvasView.InvalidateSurface();
                            }
                        }

                        // Store the new point in the dictionary
                        touchDictionary[args.Id] = point;
                    }

                    break;

                case TouchActionType.Released:
                case TouchActionType.Cancelled:
                    if (touchDictionary.ContainsKey(args.Id))
                    {
                        touchDictionary.Remove(args.Id);
                    }
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
