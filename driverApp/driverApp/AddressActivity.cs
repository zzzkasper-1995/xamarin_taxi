using Android.App;
using Android.Content;
using Android.Widget;
using Android.OS;
using Android.Support.V7.App;
using Android.Support.Design.Widget;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Newtonsoft.Json;
using System;

namespace Cheesesquare
{
    [Activity(Label = "Адрес")]
    public class AddressActivity : AppCompatActivity
    {
        public static string dep1;
        public static string arr1;
        public static bool from;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            string[] citycenter = new string[] { "50.230348, 136.901146", "48.467397, 135.120993", "0,0", "50.560574, 137.032776" };

            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.address);

            var spinner = FindViewById<Spinner>(Resource.Id.spinner);
            var autocompleteTextView = FindViewById<AutoCompleteTextView>(Resource.Id.AutoCompleteInput);
            var LinearLayout = FindViewById<LinearLayout>(Resource.Id.LinearLayout);
            var ButtonOk = FindViewById<Button>(Resource.Id.Ok);

            string firstItem = spinner.SelectedItem.ToString();
            spinner.ItemSelected += (s, e) => {

                if (firstItem.Equals(spinner.SelectedItem.ToString()))
                {
                    Snackbar.Make(LinearLayout, "Укажите куда подъехать", Snackbar.LengthLong);
                    if (autocompleteTextView.Text == "" || firstItem.Equals(spinner.SelectedItem.ToString())) ButtonOk.Enabled = false;
                    else ButtonOk.Enabled = true;
                }
                else
                {
                    if (from == true) OrderFragment.Departure_type_point = spinner.SelectedItem.ToString();
                    else OrderFragment.Arrival_type_point = spinner.SelectedItem.ToString();
                    if (autocompleteTextView.Text == "" || firstItem.Equals(spinner.SelectedItem.ToString())) ButtonOk.Enabled = false;
                    else ButtonOk.Enabled = true;
                }
            };

            RootObject results;
            ArrayAdapter autoCompleteAdapter;

            if (from == true)
            {
                if (OrderFragment.Departure != "")
                    autocompleteTextView.Text = OrderFragment.Departure;
                autocompleteTextView.Hint = "Адрес отправления";
            }
            else
            {
                if (OrderFragment.Arrival != "")
                    autocompleteTextView.Text = OrderFragment.Arrival;
                autocompleteTextView.Hint = "Адрес прибытия";
            }

            autocompleteTextView.AfterTextChanged += delegate
              {
                  if (autocompleteTextView.Text == "" || firstItem.Equals(spinner.SelectedItem.ToString())) ButtonOk.Enabled = false;
                  else ButtonOk.Enabled = true;

                  string site = "";
                  try
                  {
                      string point = autocompleteTextView.Text;
                      if (user.city == "1")
                          site = "https://maps.googleapis.com/maps/api/place/autocomplete/json?input=" + point + "&location=" + citycenter[0] + "&language=ru&radius=20000&strictbounds&key=AIzaSyCraPc_A9hC65AQ2GjVBBxtZwvWMUGUPqc";
                      if (user.city == "2")
                          site = "https://maps.googleapis.com/maps/api/place/autocomplete/json?input=" + point + "&location=" + citycenter[1] + "&language=ru&radius=20000&strictbounds&key=AIzaSyCraPc_A9hC65AQ2GjVBBxtZwvWMUGUPqc";
                      if (user.city == "3")
                          site = "https://maps.googleapis.com/maps/api/place/autocomplete/json?input=" + point + "&location=" + citycenter[2] + "&language=ru&radius=20000&strictbounds&key=AIzaSyCraPc_A9hC65AQ2GjVBBxtZwvWMUGUPqc";
                      if (user.city == "4")
                          site = "https://maps.googleapis.com/maps/api/place/autocomplete/json?input=" + point + "&location=" + citycenter[3] + "&language=ru&radius=20000&strictbounds&key=AIzaSyCraPc_A9hC65AQ2GjVBBxtZwvWMUGUPqc";

                      System.Net.WebClient web = new System.Net.WebClient();
                      web.Encoding = Encoding.UTF8;
                      string json = web.DownloadString(site);
                      results = JsonConvert.DeserializeObject<RootObject>(json);
                      List<string> mas_address = new List<string>();
                      if (results.status == "OK")
                      {
                          for (int i = 0; i < results.predictions.Count; i++)
                              mas_address.Add(results.predictions[i].structured_formatting.main_text);
                          autoCompleteAdapter = new ArrayAdapter(this, Android.Resource.Layout.SimpleDropDownItem1Line, mas_address);
                          autocompleteTextView.Adapter = autoCompleteAdapter;
                      }
                  }
                  catch (Exception e)//(WebException) 
                  { }
              };

            ButtonOk.Click += delegate
              {
                  if (from == true)
                      OrderFragment.Departure = autocompleteTextView.Text;
                  else
                      OrderFragment.Arrival = autocompleteTextView.Text;

                  Response resp = new Response();
                  if (OrderFragment.Departure!="" && OrderFragment.Arrival!="" && OrderFragment.Departure != null && OrderFragment.Arrival != null)
                  {
                      resp = ConWithServ.newOrder("info", OrderFragment.Departure, OrderFragment.Arrival, "2", OrderFragment.EditComments.Text, Convert.ToString(OrderFragment.companion.Checked));
                      if (resp.cod == "13") { OrderFragment.isInfoOrder = true; Order.price = resp.argument[1] ; Order.yardage = "" + resp.argument[2] + ""; }
                  }
                  else { Order.price = "ОТ 55 РУБ"; Order.yardage = ""; }

                  Intent intent = new Intent(this, typeof(BaseActivity));
                  StartActivity(intent);
              };
        }
    }

    public class MatchedSubstring
    {
        public int length { get; set; }
        public int offset { get; set; }
    }

    public class MainTextMatchedSubstring
    {
        public int length { get; set; }
        public int offset { get; set; }
    }

    public class StructuredFormatting
    {
        public string main_text { get; set; }
        public List<MainTextMatchedSubstring> main_text_matched_substrings { get; set; }
        public string secondary_text { get; set; }
    }

    public class Term
    {
        public int offset { get; set; }
        public string value { get; set; }
    }

    public class Prediction
    {
        public string description { get; set; }
        public string id { get; set; }
        public List<MatchedSubstring> matched_substrings { get; set; }
        public string place_id { get; set; }
        public string reference { get; set; }
        public StructuredFormatting structured_formatting { get; set; }
        public List<Term> terms { get; set; }
        public List<string> types { get; set; }
    }

    public class RootObject
    {
        public List<Prediction> predictions { get; set; }
        public string status { get; set; }
    }
}
