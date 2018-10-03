using Android.App;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Widget;

namespace Cheesesquare
{
    [Activity(Label = "Обратная связь", Icon = "@drawable/icon")]
    public class BackLineActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.backline);
            var linearLayout = FindViewById<LinearLayout>(Resource.Id.LinearLayout);
            var text = FindViewById<EditText>(Resource.Id.text);
            var Ok = FindViewById<Button>(Resource.Id.Ok);

            Ok.Click += delegate
              {
                  string txt = text.Text;
                  text.Text = "";
                  Snackbar.Make(linearLayout, "Спасибо за отзыв", Snackbar.LengthLong);
              };
        }
    }
}