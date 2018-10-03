using System;
using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Plugin.Connectivity;
using Plugin.Connectivity.Abstractions;
using Android.Preferences;
using Android.Support.Design.Widget;
using Android.Views.InputMethods;

namespace Cheesesquare
{
    [Activity(Label = "YouTaxi", MainLauncher = true, Icon = "@drawable/icon")]
    public class AuthorizationActivity : AppCompatActivity
    {
        public static bool Internet_connection = false;
        public static bool new_user = false;
        public static ISharedPreferencesEditor mPrefsEditor;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            ISharedPreferences mSharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            mPrefsEditor = mSharedPrefs.Edit();
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.authorization);
            var num = FindViewById<EditText>(Resource.Id.num);
            var pas = FindViewById<EditText>(Resource.Id.pas);
            var GetSMS = FindViewById<Button>(Resource.Id.GetSMS);
            var LinearLayout = FindViewById<LinearLayout>(Resource.Id.LinearLayout);
            var ProgressB = FindViewById<ProgressBar>(Resource.Id.ProgressB);
            ProgressB.Visibility = Android.Views.ViewStates.Invisible;
            GetSMS.Enabled = false;
            pas.Enabled = false;

            CheckConnection();

            num.AfterTextChanged += delegate
            {
                pas.Text = "";
                if (num.Text.Length == 11)
                {
                    GetSMS.Enabled = true;
                    pas.Enabled = true;
                }
                else
                {
                    GetSMS.Enabled = false;
                    pas.Enabled = false;
                }
            };

            num.AfterTextChanged += delegate
            {
                if (num.Length() == 11)
                {
                    InputMethodManager imm = (InputMethodManager)GetSystemService(Context.InputMethodService);
                    imm.HideSoftInputFromWindow(num.WindowToken, 0);
                }
            };

            if (BaseActivity.run==false)
            //пытаемся считать пароль и номер телефона из хеша
            if (mSharedPrefs.GetString("key", "") != "") //если есть сохраненный логин и пароль то
                if (mSharedPrefs.GetString("login", "") != "")
                {
                    try
                    {
                        ConWithServ.Initialized_con();
                        user.number = mSharedPrefs.GetString("login", "");
                        user.cod = mSharedPrefs.GetString("key", "");
                        Response ans = ConWithServ.hello(user.number, user.cod);

                        if (ans.cod == "102") MessageBox("Ошибка", "Пользователь с номером " + user.number + " заблокирован", "Ок");
                        else
                        if (ans.cod != "101")
                            if (ans.cod == "1")
                            {
                                if (ans.argument[0] == "3")//если город не задан то открыть окно регистрации для ввода города
                                {
                                    Intent intent = new Intent(this, typeof(RegActivity));
                                    //intent.SetFlags(ActivityFlags.NoHistory);
                                    StartActivity(intent);
                                }
                                else//иначе открываем основное окно программы
                                {
                                    user.city = ans.argument[0];
                                    Intent intent = new Intent(this, typeof(BaseActivity));
                                    //intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);// данные флаги не дают перейти на эту активити нажатием кнопки "назад"
                                    //intent.SetFlags(ActivityFlags.NoHistory);
                                    StartActivity(intent);
                                }
                            }
                    }
                    catch (Exception exp) { MessageBox("Ошибка", "Не удается связаться с сервером, пожалуйста попробуйте позже", "Ок"); }
                }

            //Если пользователь ввел 5 символов пароля То пытаемся авторизовать его
            pas.AfterTextChanged += delegate
            {
                Response ans;
                if (pas.Text.Length == 5)
                {
                    user.cod = pas.Text;
                    if (new_user) { user.city = "3"; ans = ConWithServ.newPerson(num.Text, user.city, user.cod); } //если был новый пользователь
                    else ans = ConWithServ.getNewPas(user.number, user.cod);//если старый

                    if (ans.cod == "3" || ans.cod == "16")
                    {
                        user.city = "3";
                        user.name = "";
                        user.surname = "";
                        ans = ConWithServ.hello(num.Text, pas.Text);

                        if (ans.cod == "102") MessageBox("Ошибка", "Пользователь с номером " + user.number + " заблокирован", "Ок");
                        else
                            if (ans.cod == "101") MessageBox("Ошибка", "Неверный номер или пароль", "Ок");
                        else
                        {
                            if (ans.cod == "1")
                            {
                                if (ans.argument[0] == "3")//если город не задан то открыть окно регистрации для ввода города
                                {
                                    Intent intent = new Intent(this, typeof(RegActivity));
                                    //intent.SetFlags(ActivityFlags.NoHistory);
                                    StartActivity(intent);
                                }
                                else//иначе открываем основное окно программы
                                {
                                    user.number = num.Text;
                                    user.city = ans.argument[0];
                                    mPrefsEditor.PutString("key", user.cod);
                                    mPrefsEditor.Commit();
                                    mPrefsEditor.PutString("login", user.number);
                                    mPrefsEditor.Commit();
                                    Intent intent = new Intent(this, typeof(BaseActivity));
                                    //intent.SetFlags(ActivityFlags.NewTask | ActivityFlags.ClearTask);// данные флаги не дают перейти на эту активити нажатием кнопки "назад"
                                    //intent.SetFlags(ActivityFlags.NoHistory);
                                    StartActivity(intent);
                                }
                            }
                        }
                    }
                    else
                    if (ans.cod == "103" || ans.cod == "127")
                    {
                        ProgressB.Visibility = Android.Views.ViewStates.Invisible;
                        MessageBox("Ошибка", "Неверный пароль", "Ок");
                    }
                }
            };

            //получаем пароль с сервера
            GetSMS.Click += delegate
              {
                  user.number = num.Text;
                  bool isConn = false;//наличие связи с сервером

                  //проверяем подключились ли мы к серверу
                  if (ConWithServ.readerStream == null || ConWithServ.writerStream == null)
                  {
                      if (ConWithServ.Initialized_con())
                      {
                          isConn = true;
                      }
                  }
                  else isConn = true;

                  if (isConn)
                  {
                      try
                      {
                          Response ans = ConWithServ.getNewPas(num.Text);
                          if (ans.cod == "15") //Убрать пароль из тоста!!!!!!! Если новый пароль отправлен то
                          {
                              new_user = false;
                              Snackbar.Make(LinearLayout, "СМС пароль был отправлен на ваш номер " + ans.argument[0], Snackbar.LengthLong).Show();
                          }
                          if (ans.cod == "128") //Убрать пароль из тоста!!!!!!! Если пользователь не найден то создаем нового пользователя
                          {
                              try
                              {
                                  new_user = true;
                                  user.city = "3"; //3 - значит город не выбран
                                  Response ans_newPerson = ConWithServ.newPerson(user.number, user.city);

                                  if (ans_newPerson.cod == "2") Snackbar.Make(LinearLayout, "СМС пароль был отправлен на ваш номер " + ans_newPerson.argument[0], Snackbar.LengthLong).Show();
                                  else Snackbar.Make(LinearLayout, "Не удалось отправить СМС на ваш номер", Snackbar.LengthLong).Show();
                              }
                              catch (System.NullReferenceException)
                              {
                                  MessageBox("Ошибка", "ошибка обмена данных с сервером", "Ок");
                              }
                              catch (Exception exp)
                              {
                                  MessageBox("Ошибка", exp.ToString(), "Ок");
                              }
                          }
                      }
                      catch (System.NullReferenceException)
                      {
                          MessageBox("Ошибка", "ошибка обмена данных с сервером", "Ок");
                      }
                      catch (Exception exp)
                      {
                          MessageBox("Ошибка", "Exception: " + exp, "Ок");
                      }
                  }
                  else
                  {
                      MessageBox("Ошибка", "Не удается связаться с сервером, пожалуйста попробуйте позже", "Повторить");
                  }
              };

            //срабатывает при изменении состояния подключения
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

        //показываем всплывающее сообщение
        public void MessageBox(string title, string text, string text_button)
        {
            Android.Support.V7.App.AlertDialog.Builder alert = new Android.Support.V7.App.AlertDialog.Builder(this);
            alert.SetTitle(title);
            alert.SetMessage(text);
            alert.SetPositiveButton(text_button, (senderAlert, args) => { CheckConnection(); });
            Dialog dialog = alert.Create();
            dialog.Show();
        }
    }
}
