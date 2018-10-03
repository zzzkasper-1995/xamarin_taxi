using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.OS;
using Android.Support.V7.App;
using V7Toolbar = Android.Support.V7.Widget.Toolbar;
using V4Fragment = Android.Support.V4.App.Fragment;
using V4FragmentManager = Android.Support.V4.App.FragmentManager;
using Android.Support.V4.Widget;
using Android.Support.Design.Widget;
using System.Collections.Generic;
using Android.Preferences;
using Android.Widget;
using Plugin.Connectivity;
using Plugin.Connectivity.Abstractions;
using Android.Graphics;

namespace Cheesesquare
{
    [Activity(Label = "YouTaxi")]
    public class BaseActivity: AppCompatActivity
    {
        DrawerLayout drawerLayout;
        public static bool Internet_connection = false;
        public static bool state_user = false;
        public static bool run = false;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            ISharedPreferences mSharedPrefs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor mPrefsEditor = mSharedPrefs.Edit();

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<V7Toolbar>(Resource.Id.toolbar);
            toolbar.SetTitleTextColor(Color.Black);
            toolbar.SetSubtitleTextColor(Color.Black);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawer_layout);

            NavigationView navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            if (navigationView != null) setupDrawerContent(navigationView);
            navigationView.NavigationItemSelected += NavigationView_NavigationItemSelected;

            var viewPager = FindViewById<Android.Support.V4.View.ViewPager>(Resource.Id.viewpager);
            if (viewPager != null) setupViewPager(viewPager);

            var tabLayout = FindViewById<TabLayout>(Resource.Id.tabs);
            tabLayout.SetTabTextColors(Color.Gray, Color.Black);
            tabLayout.SetupWithViewPager(viewPager);

            run = true;

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

        void NavigationView_NavigationItemSelected(object sender, NavigationView.NavigationItemSelectedEventArgs e)
        {
            var nav_home = FindViewById<V7Toolbar>(Resource.Id.nav_home);
            var nav_option = FindViewById<V7Toolbar>(Resource.Id.nav_option);
            var nav_discussion = FindViewById<V7Toolbar>(Resource.Id.nav_discussion);
            var nav_about = FindViewById<V7Toolbar>(Resource.Id.nav_about);
            var nav_exit = FindViewById<V7Toolbar>(Resource.Id.nav_exit);
            var nav_header_text = FindViewById<TextView>(Resource.Id.nav_header_text);
            if (user.number != null && user.number != "") if (nav_header_text != null) nav_header_text.Text = user.number;
            switch (e.MenuItem.ItemId)
            {
                case (Resource.Id.nav_home):
                    {
                        Intent intent = new Intent(this, typeof(BaseActivity));
                        intent.SetFlags(ActivityFlags.NoHistory);
                        StartActivity(intent);
                    }
                    break;
                case (Resource.Id.nav_option):
                    {
                        Intent intent = new Intent(this, typeof(OptionActivity));
                        intent.SetFlags(ActivityFlags.NoHistory);
                        StartActivity(intent);
                    }
                    break;
                case (Resource.Id.nav_discussion):
                    {
                        Intent intent = new Intent(this, typeof(BackLineActivity));
                        intent.SetFlags(ActivityFlags.NoHistory);
                        StartActivity(intent);
                    }
                    break;
                case (Resource.Id.nav_about):
                    {
                        Intent intent = new Intent(this, typeof(AboutActivity));
                        intent.SetFlags(ActivityFlags.NoHistory);
                        StartActivity(intent);
                    }
                    break;
                case (Resource.Id.nav_exit):
                    {
                        Intent intent = new Intent(this, typeof(AuthorizationActivity));
                        intent.SetFlags(ActivityFlags.NoHistory);
                        StartActivity(intent);
                    }
                    break;
            }
            drawerLayout.CloseDrawers();
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            var nav_header_text = FindViewById<TextView>(Resource.Id.nav_header_text);
            if (user.number != null && user.number != "") if (nav_header_text != null) nav_header_text.Text = user.number;
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    drawerLayout.OpenDrawer(Android.Support.V4.View.GravityCompat.Start);
                    nav_header_text = FindViewById<TextView>(Resource.Id.nav_header_text);
                    if (user.number != null && user.number != "") if (nav_header_text != null) nav_header_text.Text = user.number;
                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        void setupViewPager(Android.Support.V4.View.ViewPager viewPager)
        {
            var adapter = new Adapter(SupportFragmentManager);
            adapter.AddFragment(new OrderFragment(), "Заказ");
            adapter.AddFragment(new HistoryFragment(), "История");
            viewPager.Adapter = adapter;
        }

        void setupDrawerContent(NavigationView navigationView)
        {
            navigationView.NavigationItemSelected += (sender, e) =>
            {
                e.MenuItem.SetChecked(true);
                drawerLayout.CloseDrawers();
            };
        }

        class Adapter : Android.Support.V4.App.FragmentPagerAdapter
        {
            List<V4Fragment> fragments = new List<V4Fragment>();
            List<string> fragmentTitles = new List<string>();

            public Adapter(V4FragmentManager fm) : base(fm)
            {
            }

            public void AddFragment(V4Fragment fragment, String title)
            {
                fragments.Add(fragment);
                fragmentTitles.Add(title);
            }

            public override V4Fragment GetItem(int position)
            {
                return fragments[position];
            }

            public override int Count
            {
                get { return fragments.Count; }
            }

            public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
            {
                return new Java.Lang.String(fragmentTitles[position]);
            }

        }
    }

    public class ClickListener : Java.Lang.Object, View.IOnClickListener
    {
        public ClickListener(Action<View> handler)
        {
            Handler = handler;
        }

        public Action<View> Handler { get; set; }

        public void OnClick(View v)
        {
            var h = Handler;
            if (h != null)
                h(v);
        }
    }
}