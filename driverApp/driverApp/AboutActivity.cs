using Android.App;
using Android.OS;
using Android.Support.V7.App;

namespace Cheesesquare
{
    [Activity(Label = "О нас", Icon = "@drawable/icon")]
    public class AboutActivity : AppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.about);
        }
    }
}