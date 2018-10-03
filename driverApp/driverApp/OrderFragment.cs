using Android.Support.V4.App;
using Android.Views;
using Android.OS;
using Android.Widget;
using Android.Content;
using Android.Support.Design.Widget;
using Newtonsoft.Json;
using System;

namespace Cheesesquare
{
    public class OrderFragment : Fragment
    {
        public OrderFragment(){ }

        public static string Departure;
        public static string Departure_type_point;
        public static string Arrival;
        public static string Arrival_type_point;
        public static string num;
        public static string comments;
        public static bool isOrder=false;
        public static bool isInfoOrder = false;
        public static EditText EditComments;
        public static TextView TextPrice;
        public static Button setOrder;
        public static TextView TextYardage;
        public static CheckBox companion;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var view = inflater.Inflate(Resource.Layout.order, container, false);

            setOrder = view.FindViewById<Button>(Resource.Id.SendOrder);
            ProgressBar progressBar = view.FindViewById<ProgressBar>(Resource.Id.progressBar);
            var EditDeparture = view.FindViewById<EditText>(Resource.Id.EditDeparture);
            var EditArrival = view.FindViewById<EditText>(Resource.Id.EditArrival);
            EditComments = view.FindViewById<EditText>(Resource.Id.comment);
            TextPrice = view.FindViewById<TextView>(Resource.Id.TextPrice);
            TextYardage = view.FindViewById<TextView>(Resource.Id.TextYardage);
            var TextState = view.FindViewById<TextView>(Resource.Id.TextState);
            companion = view.FindViewById<CheckBox>(Resource.Id.companion);

            setOrder.Enabled = false;

            if (isInfoOrder == true)
            {
                TextPrice.Text = Order.price + " РУБ";
                setOrder.Enabled = true;
                TextYardage.Text = "Растояние " +Order.yardage + " км";
            }

            Response res = ConWithServ.getStateOrder();
            if(res.cod=="8")
            {
                isOrder = true;
                try
                {
                    OrderFromHistory o = new OrderFromHistory();
                    o = JsonConvert.DeserializeObject<OrderFromHistory>(res.argument[0]);
                    Departure = o.dep.Trim();
                    Arrival = o.arr.Trim();
                    Order.id = o.id.Trim();
                    TextPrice.Text = o.price + " РУБ";
                    setOrder.Enabled = true;
                    TextYardage.Text = "";
                    setOrder.Text = "Отменить";
                    isOrder = true;
                    isInfoOrder = false;
                    companion.Enabled = false;
                    EditComments.Enabled = false;
                    EditArrival.Enabled = false;
                    EditDeparture.Enabled = false;
                }
                catch(Exception e){
                    isOrder = false;
                }
            }

            if (OrderFragment.Departure != "" && OrderFragment.Departure!=null) EditDeparture.Text = OrderFragment.Departure;
            else EditDeparture.Text = "";

            if (OrderFragment.Arrival != "" && OrderFragment.Arrival != null) EditArrival.Text = OrderFragment.Arrival;
            else EditArrival.Text = "";

            progressBar.Visibility = ViewStates.Gone;

            EditDeparture.Click += delegate
            {
                AddressActivity.from = true;
                Intent intent = new Intent(this.Activity, typeof(AddressActivity));
                intent.SetFlags(ActivityFlags.NoHistory);
                StartActivity(intent);
            };

            setOrder.Click += delegate
              {
                  if (!isOrder)
                  {
                      Response resp = new Response();
                      if (EditDeparture.Text != "" && EditArrival.Text != "" && Arrival_type_point != null && Arrival_type_point != "" && Departure_type_point != null && Departure_type_point != "")
                      {
                          resp = ConWithServ.newOrder("ok", EditDeparture.Text, EditArrival.Text, "2", EditComments.Text, Convert.ToString(companion.Checked));
                          if (resp.cod == "6")
                          {
                              Order.dep = EditDeparture.Text;
                              Order.arr = EditArrival.Text;
                              Order.price = resp.argument[1];
                              Order.id = resp.argument[0];
                              TextPrice.Text = resp.argument[1] + "РУБ";
                              setOrder.Enabled = true;
                              TextYardage.Text = "Растояние " + resp.argument[2] + " км";
                              Snackbar.Make(setOrder, "Ваш заказ принят! Идет поиск автомобиля", Snackbar.LengthLong).Show();
                              //progressBar.Visibility = Android.Views.ViewStates.Visible;
                              TextState.Visibility = Android.Views.ViewStates.Visible;
                              setOrder.Text = "Отменить";
                              isOrder = true;
                              isInfoOrder=false;
                              companion.Enabled = false;
                              EditComments.Enabled = false;
                              EditArrival.Enabled = false;
                              EditDeparture.Enabled = false;
                          }
                      }
                      else { setOrder.Enabled = false; TextYardage.Text = ""; }
                  }
                  else
                  { 
                      Response resp = new Response();
                      resp = ConWithServ.killOrder(Order.id);
                      if (resp.cod == "7")
                      {
                          TextPrice.Text = "ОТ 55 РУБ";
                          TextYardage.Text = "";
                          Snackbar.Make(setOrder, "Ваш заказ отменен!", Snackbar.LengthLong).Show();
                          progressBar.Visibility = Android.Views.ViewStates.Invisible;
                          TextState.Visibility = Android.Views.ViewStates.Invisible;
                          setOrder.Text = "Заказать";
                          isOrder = false;
                          isInfoOrder = false;
                          Order.arr = ""; Arrival = ""; EditArrival.Text = "";
                          Order.dep = ""; Departure = ""; EditDeparture.Text = "";
                          Order.id = "";
                          Order.price = "";
                          Order.yardage = "";
                          comments = ""; EditComments.Text = "";
                          companion.Enabled = true;
                          EditComments.Enabled = true;
                          EditArrival.Enabled = true;
                          EditDeparture.Enabled = true;
                      }
                  }
              };

            companion.CheckedChange += delegate
              {
                  Response resp = ConWithServ.newOrder("info", OrderFragment.Departure, OrderFragment.Arrival, "2", OrderFragment.EditComments.Text, Convert.ToString(OrderFragment.companion.Checked));
                  if (resp.cod == "13")
                  {
                      OrderFragment.isInfoOrder = true; Order.price = resp.argument[1]; Order.yardage = "" + resp.argument[2] + "";
                      TextPrice.Text = Order.price + " РУБ";
                      setOrder.Enabled = true;
                      TextYardage.Text = "Растояние " + Order.yardage + " км";
                      if (OrderFragment.Departure != "" && OrderFragment.Departure != null) EditDeparture.Text = OrderFragment.Departure;
                      else EditDeparture.Text = "";

                      if (OrderFragment.Arrival != "" && OrderFragment.Arrival != null) EditArrival.Text = OrderFragment.Arrival;
                      else EditArrival.Text = "";
                  }
              };

            EditArrival.Touch += (s, e) =>
            {
                var handled = false;
                if (e.Event.Action == MotionEventActions.Down)
                {
                    AddressActivity.from = false;
                    Intent intent = new Intent(this.Activity, typeof(AddressActivity));
                    intent.SetFlags(ActivityFlags.NoHistory);
                    StartActivity(intent);
                    handled = true;
                }
                e.Handled = handled;
            };

            EditDeparture.Touch += (s, e) =>
            {
                var handled = false;
                if (e.Event.Action == MotionEventActions.Down)
                {
                    AddressActivity.from = true;
                    Intent intent = new Intent(this.Activity, typeof(AddressActivity));
                    intent.SetFlags(ActivityFlags.NoHistory);
                    StartActivity(intent);
                    handled = true;
                }
                e.Handled = handled;
            };

            return view;
        }
    }
}