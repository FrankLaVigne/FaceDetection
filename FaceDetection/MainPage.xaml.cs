using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.Core;
using Windows.Media.FaceAnalysis;
using Windows.Media.MediaProperties;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Shapes;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FaceDetection
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private FaceDetectionEffect _faceDetectionEffect;
        private MediaCapture _mediaCapture;
        private IMediaEncodingProperties _previewProperties;

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void btnCamera_Click(object sender, RoutedEventArgs e)
        {
            _mediaCapture = new MediaCapture();
            await _mediaCapture.InitializeAsync();

            cePreview.Source = _mediaCapture;
            await _mediaCapture.StartPreviewAsync();
            _previewProperties = _mediaCapture.VideoDeviceController.GetMediaStreamProperties(MediaStreamType.VideoPreview);
        }

        private async void btnDetectFaces_Click(object sender, RoutedEventArgs e)
        {
            var faceDetectionDefinition = new FaceDetectionEffectDefinition();
            faceDetectionDefinition.SynchronousDetectionEnabled = false;
            faceDetectionDefinition.DetectionMode = FaceDetectionMode.HighPerformance;
            _faceDetectionEffect = (FaceDetectionEffect)await _mediaCapture.AddVideoEffectAsync(faceDetectionDefinition, MediaStreamType.VideoPreview);
            _faceDetectionEffect.FaceDetected += FaceDetectionEffect_FaceDetected;
            _faceDetectionEffect.DesiredDetectionInterval = TimeSpan.FromMilliseconds(33);
            _faceDetectionEffect.Enabled = true;
        }

        private async void btnStopDetection_Click(object sender, RoutedEventArgs e)
        {
            this.cvsFaceOverlay.Children.Clear();
            _faceDetectionEffect.Enabled = false;
            _faceDetectionEffect.FaceDetected -= FaceDetectionEffect_FaceDetected;
            await _mediaCapture.ClearEffectsAsync(MediaStreamType.VideoPreview);
            _faceDetectionEffect = null;
        }   



        private async void FaceDetectionEffect_FaceDetected(FaceDetectionEffect sender, FaceDetectedEventArgs args)
        {
            var detectedFaces = args.ResultFrame.DetectedFaces;
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DrawFaceBoxes(detectedFaces);
                AnalyzeFaces();


            });

        }

        private void AnalyzeFaces()
        {



        }

        private void DrawFaceBoxes(IReadOnlyList<DetectedFace> detectedFaces)
        {
            cvsFaceOverlay.Children.Clear();

            for (int i = 0; i < detectedFaces.Count; i++)
            {
                var face = detectedFaces[i];
                var faceBounds = face.FaceBox;

                Rectangle faceRectangle = new Rectangle()
                {
                    Height = faceBounds.Height,
                    Width = faceBounds.Width
                };

                Rectangle faceHighlightRectangle = MapRectangleToDetectedFace(detectedFaces[i].FaceBox);
                faceHighlightRectangle.Stroke = new SolidColorBrush(Colors.Yellow);


                Canvas.SetLeft(faceRectangle, faceBounds.X);
                Canvas.SetTop(faceRectangle, faceBounds.Y);

                faceRectangle.StrokeThickness = 2;
                faceHighlightRectangle.StrokeThickness = 2;

                faceRectangle.Stroke = new SolidColorBrush(Colors.Yellow);
                //cvsFaceOverlay.Children.Add(faceRectangle);
                cvsFaceOverlay.Children.Add(faceHighlightRectangle);

            }

        }




        private Rectangle MapRectangleToDetectedFace(BitmapBounds detectedfaceBoxCoordinates)
        {
            var faceRectangle = new Rectangle();
            var previewStreamPropterties = _previewProperties as VideoEncodingProperties;

            double mediaStreamWidth = previewStreamPropterties.Width;
            double mediaStreamHeight = previewStreamPropterties.Height;

            var faceHighlightRect = LocatePreviewStreamCoordinates(previewStreamPropterties, this.cePreview);

            faceRectangle.Width = (detectedfaceBoxCoordinates.Width / mediaStreamWidth) * faceHighlightRect.Width;
            faceRectangle.Height = (detectedfaceBoxCoordinates.Height / mediaStreamHeight) * faceHighlightRect.Height;

            var x = (detectedfaceBoxCoordinates.X / mediaStreamWidth) * faceHighlightRect.Width;
            var y = (detectedfaceBoxCoordinates.Y / mediaStreamHeight) * faceHighlightRect.Height;

            Canvas.SetLeft(faceRectangle, x);
            Canvas.SetTop(faceRectangle, y);

            return faceRectangle;
        }

        public Rect LocatePreviewStreamCoordinates(VideoEncodingProperties previewResolution, CaptureElement previewControl)
        {
            var uiRectangle = new Rect();

            var mediaStreamWidth = previewResolution.Width;
            var mediaStreamHeight = previewResolution.Height;

            uiRectangle.Width = previewControl.ActualWidth;
            uiRectangle.Height = previewControl.ActualHeight;

            var uiRatio = previewControl.ActualWidth / previewControl.ActualHeight;
            var mediaStreamRatio = mediaStreamWidth / mediaStreamHeight;

            if (uiRatio > mediaStreamRatio)
            {
                var scaleFactor = previewControl.ActualHeight / mediaStreamHeight;
                var scaledWidth = mediaStreamWidth * scaleFactor;

                uiRectangle.X = (previewControl.ActualWidth - scaledWidth) / 2.0;
                uiRectangle.Width = scaledWidth;
            }
            else 
            {
                var scaleFactor = previewControl.ActualWidth / mediaStreamWidth;
                var scaledHeight = mediaStreamHeight * scaleFactor;
                uiRectangle.Y = (previewControl.ActualHeight - scaledHeight) / 2.0;
                uiRectangle.Height = scaledHeight;
            }

            return uiRectangle;
        }

    }
}
