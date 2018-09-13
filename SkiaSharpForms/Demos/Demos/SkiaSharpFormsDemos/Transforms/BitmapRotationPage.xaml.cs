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
    public partial class BitmapRotationPage : ContentPage
    {
        // Bitmap and matrix for display
        SKBitmap bitmap;
        SKMatrix matrix = SKMatrix.MakeIdentity();

        // Touch information
        Dictionary<long, SKPoint> touchDictionary = new Dictionary<long, SKPoint>();

        public BitmapRotationPage()
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
                    if (!touchDictionary.ContainsKey(args.Id))
                    {
                        // Invert the matrix
                        if (matrix.TryInvert(out SKMatrix inverseMatrix))
                        {
                            // Transform the point using the inverted matrix
                            SKPoint transformedPoint = inverseMatrix.MapPoint(point);

                            // Check if it's in the untransformed bitmap rectangle
                            SKRect rect = new SKRect(0, 0, bitmap.Width, bitmap.Height);

                            if (rect.Contains(transformedPoint))
                            {
                                touchDictionary.Add(args.Id, point);
                            }
                        }
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
                        // Double-finger rotate, scale, and drag
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

                            // Find angles from pivot point to touch points
                            float oldAngle = (float)Math.Atan2(oldVector.Y, oldVector.X);
                            float newAngle = (float)Math.Atan2(newVector.Y, newVector.X);

                            // Calculate rotation matrix
                            float angle = newAngle - oldAngle;
                            SKMatrix touchMatrix = SKMatrix.MakeRotation(angle, pivotPoint.X, pivotPoint.Y);

                            // Effectively rotate the old vector
                            float magnitudeRatio = Magnitude(oldVector) / Magnitude(newVector);
                            oldVector.X = magnitudeRatio * newVector.X;
                            oldVector.Y = magnitudeRatio * newVector.Y;

                            // Isotropic scaling!
                            float scale = Magnitude(newVector) / Magnitude(oldVector);

                            if (!float.IsNaN(scale) && !float.IsInfinity(scale))
                            {
                                SKMatrix.PostConcat(ref touchMatrix,
                                    SKMatrix.MakeScale(scale, scale, pivotPoint.X, pivotPoint.Y));

                                SKMatrix.PostConcat(ref matrix, touchMatrix);
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

        float Magnitude(SKPoint point)
        {
            return (float)Math.Sqrt(Math.Pow(point.X, 2) + Math.Pow(point.Y, 2));
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
