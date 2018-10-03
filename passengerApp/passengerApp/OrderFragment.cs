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
        public OrderFragment() { }

        public static string Departure;
        public static string Departure_type_point;
        public static string Arrival;
        public static string Arrival_type_point;
        public static string num;
        public static string comments;
        public static bool isOrder = false;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            base.OnCreateView(inflater, container, savedInstanceState);

            var view = inflater.Inflate(Resource.Layout.order, container, false);

            var lcomment = view.FindViewById<TextInputLayout>(Resource.Id.lcomment);
            var lDeparture = view.FindViewById<TextInputLayout>(Resource.Id.lDeparture);
            var lArrivalt = view.FindViewById<TextInputLayout>(Resource.Id.lArrival);
            var lButtons = view.FindViewById<LinearLayout>(Resource.Id.lButtons);
            lcomment.Visibility = Android.Views.ViewStates.Gone;
            lDeparture.Visibility = Android.Views.ViewStates.Gone;
            lArrivalt.Visibility = Android.Views.ViewStates.Gone;
            lButtons.Visibility = Android.Views.ViewStates.Gone;
            var KillOrder = view.FindViewById<Button>(Resource.Id.KillOrder);
            KillOrder.Enabled = false;
            var SendMove = view.FindViewById<Button>(Resource.Id.SendMove);
            SendMove.Enabled = false;
            var EditDeparture = view.FindViewById<EditText>(Resource.Id.EditDeparture);
            EditDeparture.Enabled = false;
            var EditArrival = view.FindViewById<EditText>(Resource.Id.EditArrival);
            EditArrival.Enabled = false;
            var EditComments = view.FindViewById<EditText>(Resource.Id.comment);
            EditComments.Enabled = false;
            var TextPrice = view.FindViewById<TextView>(Resource.Id.TextPrice);
            TextPrice.Text = "";
            var TextWar = view.FindViewById<TextView>(Resource.Id.TextWar);
            TextWar.Text = "Сейчас у вас нет активных заказов";
            TextPrice.Gravity = Android.Views.GravityFlags.Center;
            var TextYardage = view.FindViewById<TextView>(Resource.Id.TextYardage); 
            var TextState = view.FindViewById<TextView>(Resource.Id.TextState); 

            Response res = ConWithServ.getState();
            if (res.cod == "10")
            {
                isOrder = true;
                try
                {
                    OrderFromHistory o = new OrderFromHistory();
                    o = JsonConvert.DeserializeObject<OrderFromHistory>(res.argument[0]);
                    Departure = o.dep.Trim();
                    lcomment.Visibility = Android.Views.ViewStates.Visible;
                    lDeparture.Visibility = Android.Views.ViewStates.Visible;
                    lArrivalt.Visibility = Android.Views.ViewStates.Visible;
                    lButtons.Visibility = Android.Views.ViewStates.Visible;
                    if (o.move.Trim() == "был назначен водитель") TextState.Text ="Поторопитесь, пассажир очень надеется на вас" ;
                    if (o.move.Trim() == "Ожидаю пассажира") TextState.Text = "Подождите, пассажир скоро выйдет к вам";
                    if (o.move.Trim() == "Пассажир сел в машину, еду к цели") TextState.Text = "Пассажир прямо рядом с тобой";
                    Arrival = o.arr.Trim();
                    Order.id = o.id.Trim();
                    TextPrice.Text = o.price + " РУБ";
                    KillOrder.Enabled = true;
                    KillOrder.Text = "Отказаться";
                    TextYardage.Text = "";
                    SendMove.Enabled = true;
                    SendMove.Text = "Ожидаю пассажира";
                    TextWar.Visibility = Android.Views.ViewStates.Gone;
                }
                catch (Exception e) { isOrder = false;
                    Departure = "";
                    Arrival = "";
                    Order.id = "";
                    TextPrice.Text = "";
                    KillOrder.Enabled = false;
                    KillOrder.Text = "";
                    TextYardage.Text = "";
                    SendMove.Text = "";
                    TextWar.Visibility = Android.Views.ViewStates.Visible;
                    lcomment.Visibility = Android.Views.ViewStates.Gone;
                    lDeparture.Visibility = Android.Views.ViewStates.Gone;
                    lArrivalt.Visibility = Android.Views.ViewStates.Gone;
                    lButtons.Visibility = Android.Views.ViewStates.Gone;
                }
            }

            if (OrderFragment.Departure != "" && OrderFragment.Departure != null) EditDeparture.Text = OrderFragment.Departure;
            else EditDeparture.Text = "";

            if (OrderFragment.Arrival != "" && OrderFragment.Arrival != null) EditArrival.Text = OrderFragment.Arrival;
            else EditArrival.Text = "";

            KillOrder.Click += delegate
            {
                Response res1 = new Response();
                res1 = ConWithServ.killOrder(Order.id);
                if (res1.cod == "14")
                {
                    isOrder = false;
                    try
                    {
                        Departure = "";
                        Arrival = "";
                        Order.id = "";
                        TextPrice.Text = "";
                        KillOrder.Enabled = false;
                        KillOrder.Text = "";
                        TextYardage.Text = "";
                        SendMove.Text = "";
                        TextWar.Visibility = Android.Views.ViewStates.Visible;
                        lcomment.Visibility = Android.Views.ViewStates.Gone;
                        lDeparture.Visibility = Android.Views.ViewStates.Gone;
                        lArrivalt.Visibility = Android.Views.ViewStates.Gone;
                        lButtons.Visibility = Android.Views.ViewStates.Gone;
                    }
                    catch (Exception e) { isOrder = false; }
                }
            };

            SendMove.Click += delegate
            {
                Response res1 = ConWithServ.getState();
                OrderFromHistory o = new OrderFromHistory();
                o = JsonConvert.DeserializeObject<OrderFromHistory>(res1.argument[0]);
                try
                {
                    if (o.move.Trim() == "был назначен водитель")
                    {
                        TextState.Text = "Подождите, пассажир скоро выйдет к вам";
                        SendMove.Text = "Еду на место назначения";
                        ConWithServ.changeOrder(Order.id, "Ожидаю пассажира");
                    }

                    if (o.move.Trim() == "Ожидаю пассажира")
                    {
                        TextState.Text = "Пассажир прямо рядом с тобой";
                        SendMove.Text = "Заказ выполнен";
                        ConWithServ.changeOrder(Order.id, "Пассажир сел в машину, еду к цели");
                    }
                    if (o.move.Trim() == "Пассажир сел в машину, еду к цели")
                    {
                        res1 = ConWithServ.changeOrder(Order.id, "Заказ выполнен");
                        res1 = ConWithServ.getState();
                        if (res1.cod == "10")
                        {
                            isOrder = false;
                            try
                            {
                                Departure = "";
                                Arrival = "";
                                Order.id = "";
                                TextPrice.Text = "";
                                KillOrder.Enabled = false;
                                KillOrder.Text = "";
                                TextYardage.Text = "";
                                SendMove.Text = "";
                                TextWar.Visibility = Android.Views.ViewStates.Visible;
                                lcomment.Visibility = Android.Views.ViewStates.Gone;
                                lDeparture.Visibility = Android.Views.ViewStates.Gone;
                                lArrivalt.Visibility = Android.Views.ViewStates.Gone;
                                lButtons.Visibility = Android.Views.ViewStates.Gone;
                            }
                            catch (Exception e) { isOrder = false;
                                Departure = "";
                                Arrival = "";
                                Order.id = "";
                                TextPrice.Text = "";
                                KillOrder.Enabled = false;
                                KillOrder.Text = "";
                                TextYardage.Text = "";
                                SendMove.Text = "";
                                TextWar.Visibility = Android.Views.ViewStates.Visible;
                                lcomment.Visibility = Android.Views.ViewStates.Gone;
                                lDeparture.Visibility = Android.Views.ViewStates.Gone;
                                lArrivalt.Visibility = Android.Views.ViewStates.Gone;
                                lButtons.Visibility = Android.Views.ViewStates.Gone;
                            }
                        }
                    }
                }
                catch (Exception e) {}
            };

            return view;
        }
    }
}