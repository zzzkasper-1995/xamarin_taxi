using System;
using Android.Views;
using Android.OS;
using Android.Support.V7.Widget;
using System.Collections.Generic;
using Android.Widget;
using Android.Content;
using Android.Util;
using Android.Runtime;
using Newtonsoft.Json;
using Android.Support.Design.Widget;
using Android.Graphics;

namespace Cheesesquare
{
    public class OrderListFragment : Android.Support.V4.App.Fragment
    {
        public RecyclerView rv;

        public OrderListFragment()
        {
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var v = inflater.Inflate(Resource.Layout.order_list_fragment, container, false);
            rv = v.JavaCast<RecyclerView>();
            setupRecyclerView();
            return rv;
        }

        public void setupRecyclerView()
        {
            rv.SetLayoutManager(new LinearLayoutManager(rv.Context));
            List<string> orders = ConWithServ.getOrder().argument;
            rv.SetAdapter (new SimpleStringRecyclerViewAdapter (Activity, orders));
        }

        public class SimpleStringRecyclerViewAdapter : RecyclerView.Adapter
        {

            TypedValue typedValue = new TypedValue();
            int background;
            List<string> values;
            Android.App.Activity parent;

            public class ViewHolder : RecyclerView.ViewHolder
            {
                public View View { get; set; }
                public TextView TextView { get; set; }
                public TextView TextView1 { get; set; }
                public EventHandler ClickHandler { get; set; }

                public ViewHolder(View view) : base(view)
                {
                    View = view;
                    TextView = view.FindViewById<TextView>(Android.Resource.Id.Text1);
                }

                public override string ToString()
                {
                    return base.ToString() + " '" + TextView.Text;
                }
            }

            public String GetValueAt(int position)
            {
                return values[position];
            }

            public SimpleStringRecyclerViewAdapter(Android.App.Activity context, List<String> items)
            {
                parent = context;
                context.Theme.ResolveAttribute(Resource.Attribute.selectableItemBackground, typedValue, true);
                background = typedValue.ResourceId;
                values = items;
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
            {
                var view = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.list_item, parent, false);
                view.SetBackgroundResource(background);
                return new ViewHolder(view);
            }

            public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
            {
                var h = holder as ViewHolder;
                OrderFromHistory o = new OrderFromHistory();
                o = JsonConvert.DeserializeObject<OrderFromHistory>(values[position]);
                if (o == null) h.TextView.Text = "Сейчас свободных заказов к сожалению нет, подождите немного";
                else
                {
                    if (Convert.ToInt32(o.price) > 60) { h.TextView.SetBackgroundColor(Color.Coral);}
                    if (Convert.ToInt32(o.price) > 90) { h.TextView.SetBackgroundColor(Color.Gold); }
                    if (Convert.ToInt32(o.price) > 150) { h.TextView.SetBackgroundColor(Color.ForestGreen); }
                    h.TextView.Text = "От " + o.dep.Trim() + "\nДо " + o.arr.Trim() + "\nЦена поездки: " + o.price.Trim()+ " руб";

                    if (h.ClickHandler != null)
                        h.View.Click -= h.ClickHandler;

                    h.ClickHandler = new EventHandler((sender, e) =>
                    {
                        bool b = false;
                        Snackbar.Make(h.TextView, "Вы хотите принять этот заказ", Snackbar.LengthLong).SetAction("OK",
                            (v) =>
                            {
                                b = true;
                                Response res1 = ConWithServ.getState();
                                res1 = ConWithServ.takeOrder(o.id);
                                if (res1.cod == "12")
                                {
                                    OrderFragment.isOrder = true;
                                    try
                                    {
                                        Order.dep = o.dep;
                                        Order.arr = o.arr;
                                        Order.id = o.id;
                                        Order.price = o.price;

                                        var context = h.View.Context;
                                        var intent = new Intent(context, typeof(BaseActivity));
                                        context.StartActivity(intent);
                                    }
                                    catch (Exception exc) { OrderFragment.isOrder = false; }
                                }
                            }).Show();
                    });

                    h.View.Click += h.ClickHandler;
                }
            }

            public override int ItemCount { get { return values.Count; } }
        }
    }
}



