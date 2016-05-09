using System;
using System.Net;
using System.Threading.Tasks;
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Widget;
using Toggl.Joey.UI.Utils;
using Toggl.Phoebe.Logging;
using XPlatUtils;

namespace Toggl.Joey.UI.Views
{
    class ProfileImageView : ImageView
    {
        private static readonly string LogTag = "ProfileImageView";
        private static readonly int RectSize = 56; //In DP!

        public ProfileImageView(Context context) : base(context)
        {
        }

        public ProfileImageView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
        }

        public ProfileImageView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
        }

        private string imageUrl;

        public string ImageUrl
        {
            get { return imageUrl; }
            set
            {
                SetImage(value);
            }
        }

        private async Task<Bitmap> GetImage(string url)
        {
            Bitmap bitmap = CachingUtil.GetBitmapFromCacheByUrl(url, Context);
            if (bitmap != null)
            {
                return bitmap;
            }

            try
            {
                var request = WebRequest.Create(url);
                var resp = await request.GetResponseAsync()
                           .ConfigureAwait(continueOnCapturedContext: false);
                var stream = resp.GetResponseStream();

                bitmap = BitmapFactory.DecodeStream(stream);
                bitmap = ScaleImage(bitmap, Resources.DisplayMetrics);
                bitmap = CropImage(bitmap);
                bitmap = MakeImageRound(bitmap);

                CachingUtil.PutBitmapToCacheByUrl(url, bitmap, Context);
                return bitmap;
            }
            catch (Exception ex)
            {
                var log = ServiceContainer.Resolve<ILogger> ();
                log.Debug(LogTag, ex, "Failed to get user profile image.");
                return null;
            }
        }
        //Scaling image so that it has at least one of the sides be RectSize
        private static Bitmap ScaleImage(Bitmap bitmap, DisplayMetrics metrics)
        {
            int rectSizePx = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, RectSize, metrics);
            float minSize = (int)Math.Min(bitmap.Width, bitmap.Height);
            var scaleFactor = rectSizePx / minSize;
            int scaledWidth = (int)Math.Floor(scaleFactor * bitmap.Width);
            int scaledHeight = (int)Math.Floor(scaleFactor * bitmap.Height);

            return Bitmap.CreateScaledBitmap(bitmap, scaledWidth, scaledHeight, false);
        }

        //Make image rectangular
        private static Bitmap CropImage(Bitmap bitmap)
        {
            if (bitmap.Width >= bitmap.Height)
            {
                bitmap = Bitmap.CreateBitmap(bitmap, bitmap.Width / 2 - bitmap.Height / 2, 0, bitmap.Height, bitmap.Height);
            }
            else
            {
                bitmap = Bitmap.CreateBitmap(bitmap, 0, bitmap.Height / 2 - bitmap.Width / 2, bitmap.Width, bitmap.Width);
            }

            return bitmap;
        }

        private static Bitmap MakeImageRound(Bitmap bitmap)
        {
            Bitmap output = Bitmap.CreateBitmap(bitmap.Width, bitmap.Height, Bitmap.Config.Argb8888);
            var canvas = new Canvas(output);

            var rect = new Rect(0, 0, bitmap.Width, bitmap.Height);
            var rectF = new RectF(rect);

            var paint = new Paint();
            paint.AntiAlias = true;
            paint.Color = Color.Black;

            canvas.DrawARGB(0, 0, 0, 0);
            canvas.DrawRoundRect(rectF, bitmap.Width, bitmap.Height, paint);

            paint.SetXfermode(new PorterDuffXfermode(PorterDuff.Mode.SrcIn));

            canvas.DrawBitmap(bitmap, rect, rect, paint);

            return output;
        }

        private async void SetImage(string imageUrl)
        {
            if (this.imageUrl == imageUrl)
            {
                return;
            }
            this.imageUrl = imageUrl;

            Bitmap bitmap = null;
            if (imageUrl != null)
            {
                bitmap = await GetImage(imageUrl);
            }

            // Protect against setting old image to the view
            if (this.imageUrl != imageUrl)
            {
                return;
            }

            if (bitmap != null)
            {
                SetImageBitmap(bitmap);
            }
            else
            {
                SetImageDrawable(null);
            }
        }
    }
}

