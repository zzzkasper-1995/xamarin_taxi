using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Plugin.Connectivity;
using Plugin.Connectivity.Abstractions;
using Android.Content;
using Android.Views;
using Android.Support.Design.Widget;

namespace Cheesesquare
{
    [Activity(Label = "Регистрация", Icon = "@drawable/icon")]
    public class RegActivity : AppCompatActivity
    {
        public static bool Internet_connection = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.reg);
            var name = FindViewById<EditText>(Resource.Id.name);
            var surname = FindViewById<EditText>(Resource.Id.surname);
            var brand_auto = FindViewById<EditText>(Resource.Id.brand_auto);
            var state_number = FindViewById<EditText>(Resource.Id.state_number);
            var data_burn_auto = FindViewById<EditText>(Resource.Id.data_burn_auto);
            var сolor = FindViewById<EditText>(Resource.Id.Color);
            var ok = FindViewById<Android.Widget.Button>(Resource.Id.Ok);
            var spinner = FindViewById<Spinner>(Resource.Id.spinner);
            View LinearLayout = FindViewById<LinearLayout>(Resource.Id.LinearLayout);

            CheckConnection();

            string firstItem = spinner.SelectedItem.ToString();
            spinner.ItemSelected += (s, e) => {

                if (firstItem.Equals(spinner.SelectedItem.ToString()))
                {
                    Snackbar.Make(ok, "Выберите, пожалуйста, город", Snackbar.LengthLong);
                    user.city = "3";
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
                if(name.Text =="" || surname.Text=="" ||  brand_auto.Text == "" ||
                    data_burn_auto.Text == "" || сolor.Text == "" || state_number.Text == "" || user.city=="3")
                    Snackbar.Make(ok, "Заполните все поля", Snackbar.LengthLong);
                else
                {
                    user.surname = surname.Text;
                    user.name = name.Text;
                    user.number_auto = state_number.Text;
                    user.brand = brand_auto.Text;
                    user.color = сolor.Text;
                    user.date_burn = data_burn_auto.Text;

                    //отправка настроек пользователя на сервер 
                    Response ans = ConWithServ.setOption(user.surname, user.name, user.city, user.number_auto, 
                                                         user.date_burn, user.color, user.brand);
                    if(ans.cod=="18")
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
                }
            };

            CrossConnectivity.Current.ConnectivityChanged += Current_ConnectivityChanged;
        }

        // обработка изменения состояния подключения
        private void Current_ConnectivityChanged(object sender, ConnectivityChangedEventArgs e)
        {
            CheckConnection();
        }

        // получаем состояние подключения
        private void CheckConnection()
        {
            if (CrossConnectivity.Current != null &&
                CrossConnectivity.Current.ConnectionTypes != null &&
                CrossConnectivity.Current.IsConnected == true)
            {
                Internet_connection = true;
            }
            else
            {
                Internet_connection = false;
                //Всплывающее окно которое сигнализирует об отсутствие интернета
                MessageBox("Ошибка", "Отсутствует соединение с сервером. Проверьте подключение к интернету", "Повторить");
            }
        }

        public void MessageBox(string title, string text, string text_button)
        {
            Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(text);
            alert.SetPositiveButton(text_button, (senderAlert, args) => {});
            Dialog dialog = alert.Create();
            dialog.Show();
        }
    }
}
