using System;
using System.IO;
using Android.Content;
using Android.OS;
using Toggl.Phoebe.Data;
using Toggl.Phoebe.Reactive;
using Activity = Android.Support.V7.App.AppCompatActivity;
using FragmentManager = Android.Support.V4.App.FragmentManager;

namespace Toggl.Joey.UI.Activities
{
    public abstract class BaseActivity : Activity
    {
        public static readonly string IntentProjectIdArgument = "project_id_param";
        public static readonly string IntentTaskIdArgument = "task_id_param";
        public static readonly string IntentWorkspaceIdArgument = "workspace_id_param";

        private const int SyncErrorMenuItemId = 0;
        protected readonly Handler Handler = new Handler();

        /// <summary>
        /// The activity that is currently in the foreground.
        /// </summary>
        public static BaseActivity CurrentActivity { get; private set; }

        protected sealed override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            CurrentActivity = this;
            OnCreateActivity(savedInstanceState);
        }

        protected virtual void OnCreateActivity(Bundle state)
        {
        }

        protected sealed override void OnResume()
        {
            base.OnResume();
            OnResumeActivity();
        }

        protected virtual void OnResumeActivity()
        {
            // Make sure that the components are initialized (and that this initialisation wouldn't cause a lag)
            var app = (AndroidApp)Application;

            if (!app.ComponentsInitialized)
            {
                Handler.PostDelayed(delegate
                {
                    app.InitializeComponents();
                }, 5000);
            }
            app.MarkLaunched();
        }

        public new FragmentManager FragmentManager
        {
            get { return SupportFragmentManager; }
        }

        public static Intent CreateDataIntent<TActivity, T> (Context context, T dataObject, string id)
        {
            var intent = new Intent(context, typeof(TActivity));

            // User json serializer for fast process?
            var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
            using(var listStream = new MemoryStream())
            {
                serializer.Serialize(listStream, dataObject);
                intent.PutExtra(id, listStream.ToArray());
            }
            return intent;
        }

        public static T GetDataFromIntent<T> (Intent intent, string id) where T : new()
        {
            // Get the person object from the intent
            T dataObject;
            if (intent.HasExtra(id))
            {
                var serializer = new System.Xml.Serialization.XmlSerializer(typeof(T));
                var byteArray = intent.GetByteArrayExtra(id);
                dataObject = (T)serializer.Deserialize(new MemoryStream(byteArray));
            }
            else
            {
                dataObject = new T();
            }
            return dataObject;
        }
    }
}