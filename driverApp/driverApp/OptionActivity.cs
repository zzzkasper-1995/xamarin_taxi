using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.Design.Widget;

namespace Cheesesquare
{
    [Activity(Label = "Настройки")]
    public class OptionActivity : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.about);
            SetContentView(Resource.Layout.reg);
            var name = FindViewById<EditText>(Resource.Id.name);
            var surname = FindViewById<EditText>(Resource.Id.surname);
            var ok = FindViewById<Android.Widget.Button>(Resource.Id.Ok);
            var spinner = FindViewById<Spinner>(Resource.Id.spinner);
            View LinearLayout = FindViewById<LinearLayout>(Resource.Id.LinearLayout);
            if (user.name != null && user.name != "") name.Text = user.name;
            if (user.surname != null && user.surname != "") surname.Text = user.surname;
            //if (user.city == "4") spiner;
            //if (e.Parent.GetItemIdAtPosition(e.Position).ToString() == "2") user.city = "2";
            //if (e.Parent.GetItemIdAtPosition(e.Position).ToString() == "3") user.city = "1";

            string firstItem = spinner.SelectedItem.ToString();
            spinner.ItemSelected += (s, e) => {

                if (firstItem.Equals(spinner.SelectedItem.ToString()))
                {
                }
                else
                {
                    if (e.Parent.GetItemIdAtPosition(e.Position).ToString() == "1") user.city = "4";
                    if (e.Parent.GetItemIdAtPosition(e.Position).ToString() == "2") user.city = "2";
                    if (e.Parent.GetItemIdAtPosition(e.Position).ToString() == "3") user.city = "1";
                }
            };

            ok.Click += delegate
            {
                    //user.date_burn = date.Text.Replace("/", "-");
                    if(surname.Text!="") user.surname = surname.Text;
                    if (name.Text != "") user.name = name.Text;
                    //отправка настроек на сервер пользователя
                    Response ans = ConWithServ.setOption(user.surname, user.name, user.city);
                if (ans.cod == "18")
                {
                    AuthorizationActivity.mPrefsEditor.PutString("key", user.cod);
                    AuthorizationActivity.mPrefsEditor.Commit();
                    AuthorizationActivity.mPrefsEditor.PutString("login", user.number);
                    AuthorizationActivity.mPrefsEditor.Commit();
                    Intent intent = new Intent(this, typeof(BaseActivity));
                    //intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);// данные флаги не дают перейти на эту активити нажатием кнопки "назад"
                    //intent.SetFlags(ActivityFlags.NoHistory);
                    StartActivity(intent);
                }
                else MessageBox("Ошибка", "Жуткие бесы шалят на сервере и мешают нам запомнить вас", "Попробовать вновь");
            };
        }

        public void MessageBox(string title, string text, string text_button)
        {
            Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(text);
            alert.SetPositiveButton(text_button, (senderAlert, args) => { });
            Dialog dialog = alert.Create();
            dialog.Show();
        }
    }
}