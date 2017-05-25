using Android.App;
using Android.Widget;
using Android.OS;
using System;
using Android.Content.PM;
using Android.Provider;
using Android.Content;
using System.Collections.Generic;
using Java.IO;
using Android.Runtime;
using Android.Graphics;
using Android;
using System.Threading.Tasks;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using static Android.Provider.MediaStore;
using System.IO;
using Android.Database;

namespace CameraApp
{

    [Activity(Label = "CameraApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
        ImageView _imageView;
        int CAMERA_CODE = 1000, CAMERA_REQUEST = 1001, INTERNET_REQUEST = 1002, SD_REQUEST = 1003;
        Stream inputStream;
        //private VisionServiceClient visionClient;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            if (CheckSelfPermission(Manifest.Permission.Camera) == Permission.Denied)
            {
                RequestPermissions(new string[] { Manifest.Permission.Camera }, CAMERA_REQUEST);
            }
            if (CheckSelfPermission(Manifest.Permission.Internet) == Permission.Denied)
            {
                RequestPermissions(new string[] { Manifest.Permission.Internet }, INTERNET_REQUEST);
            }
            //if (CheckSelfPermission(Manifest.Permission.ReadExternalStorage) == Permission.Denied ||
            //    CheckSelfPermission(Manifest.Permission.WriteExternalStorage) == Permission.Denied)
            //{
            //    RequestPermissions(new string[] { Manifest.Permission.ReadExternalStorage, Manifest.Permission.WriteExternalStorage }, SD_REQUEST);
            //}
            if (IsThereAnAppToTakePictures())
            {
                Button button = FindViewById<Button>(Resource.Id.button1);
                var button2 = FindViewById<Button>(Resource.Id.button2);
                _imageView = FindViewById<ImageView>(Resource.Id.imageView1);
                button.Click += TakeAPicture;
                button2.Click += Button2_Click;
            }
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            new DetectTask(this).Execute(inputStream);

        }
        private void TakeAPicture(object sender, EventArgs e)
        {
            var intent = new Intent(MediaStore.ActionImageCapture);
            StartActivityForResult(intent, CAMERA_CODE);
        }

        private bool IsThereAnAppToTakePictures()
        {
            var intent = new Intent(MediaStore.ActionImageCapture);
            IList<ResolveInfo> availableActivities = PackageManager.QueryIntentActivities(intent, PackageInfoFlags.MatchDefaultOnly);
            return availableActivities != null && availableActivities.Count > 0;
        }
        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            //base.OnActivityResult(requestCode, resultCode, data);
            if (requestCode == CAMERA_CODE && resultCode == Android.App.Result.Ok)
            {
                var photo = data.Extras.Get("data") as Bitmap;
                byte[] bitmapdata;
                using (var stream = new MemoryStream())
                {
                    int _width = 1920;
                    int _height = 1080;
                    if (photo.Width < photo.Height)
                    {
                        _width += _height;//1920+1080
                        _height = _width - _height;//(1920+1080)-1080
                        _width = _width - _height;//(1920+1080)-(1920+1080)-1080
                    }
                    var resized = Bitmap.CreateScaledBitmap(photo, _width, _height, true);
                    resized.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
                    bitmapdata = stream.ToArray();
                    _imageView.SetImageBitmap(photo);
                }
                var _path = getRealPathFromURI(data.Data);
                if (_path.Contains(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath))
                {

                }
                //if (System.IO.File.Exists(_path))
                //{
                //    System.IO.File.Delete(_path);
                //}

                inputStream = new MemoryStream(bitmapdata);
            }

        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Permission[] grantResults)
        {
            //base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            if (requestCode == CAMERA_REQUEST)
            {
                if (grantResults[0] == Permission.Granted)
                    Toast.MakeText(this, "Permission Granted", ToastLength.Short).Show();
                else
                    Toast.MakeText(this, "Permission Deind", ToastLength.Long).Show();

            }

        }

        public String getRealPathFromURI(Android.Net.Uri contentUri)
        {
            var mediaStoreImagesMediaData = "_data";
            string[] projection = { mediaStoreImagesMediaData };
            Android.Database.ICursor cursor = this.ContentResolver.Query(contentUri, projection,
                                                                null, null, null);
            int columnIndex = cursor.GetColumnIndexOrThrow(mediaStoreImagesMediaData);
            cursor.MoveToFirst();
            return cursor.GetString(columnIndex);
        }

    }
    class DetectTask : AsyncTask<Stream, string, string>
    {
        private MainActivity mainActivity;
        private ProgressDialog pd = new ProgressDialog(Application.Context);
        public DetectTask(MainActivity activity)
        {
            this.mainActivity = activity;
        }
        byte[] GetImageAsByteArray(Stream imageSteam)
        {
            BinaryReader binaryReader = new BinaryReader(imageSteam);
            return binaryReader.ReadBytes((int)imageSteam.Length);
        }
        protected override string RunInBackground(params Stream[] @params)
        {
            PublishProgress("Detecting.....");
            string responseContent;
            responseContent = GetImageDescription(@params[0]).Result;
            return responseContent;
        }

        private async Task<string> GetImageDescription(Stream stream)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "69f70079e3bc40709f0f41a8cf7e89b5");

            // Request parameters and URI string.
            string queryString = "returnFaceId=true&returnFaceLandmarks=false&returnFaceAttributes=age,gender,glasses,smile";
            string uri = "https://westcentralus.api.cognitive.microsoft.com/face/v1.0/detect?" + queryString;
            HttpResponseMessage response;


            // Request body. Try this sample with a locally stored JPEG image.
            byte[] byteData = GetImageAsByteArray(stream);
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);
                return await response.Content.ReadAsStringAsync();
            }

        }




        protected override void OnPreExecute()
        {
            pd.Window.SetType(Android.Views.WindowManagerTypes.SystemAlert);
            pd.Show();
        }
        protected override void OnProgressUpdate(params string[] values)
        {
            pd.SetMessage(values[0]);
        }
        protected override async void OnPostExecute(string result)
        {
            try
            {
                var faces = await Task.Run(() => JsonConvert.DeserializeObject<List<FaceModel>>(result));
                var txt = mainActivity.FindViewById<TextView>(Resource.Id.txtDescription);
                if (faces != null)
                    txt.Text = $"Gender:{faces.First().faceAttributes.Gender} " +
                        $"Age:{faces.First().faceAttributes.Age} " +
                        $"Glasses:{faces.First().faceAttributes.Glasses } " +
                        $"Smile {faces.First().faceAttributes.Smile}";
                pd.Dismiss();
            }
            catch (Exception ex)
            {
                pd.Dismiss();
                Toast.MakeText(mainActivity, "Not find faces.", ToastLength.Long).Show();
            }


        }

    }
}

