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

namespace Cheesesquare
{
    public class HistoryFragment : Android.Support.V4.App.Fragment
    {
        public HistoryFragment()
        {
        }

        public override View OnCreateView (LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var v = inflater.Inflate (Resource.Layout.history_fragment, container, false);
            var rv = v.JavaCast<RecyclerView> ();
            setupRecyclerView(rv);
            return rv;
        }

        void setupRecyclerView (RecyclerView recyclerView) 
        {
            recyclerView.SetLayoutManager (new LinearLayoutManager (recyclerView.Context));
            recyclerView.SetAdapter (new SimpleStringRecyclerViewAdapter (Activity, ConWithServ.getHistory().argument));
        }

        public class SimpleStringRecyclerViewAdapter : RecyclerView.Adapter 
        {
            
            TypedValue typedValue = new TypedValue ();
            int background;
            List<string> values;
            Android.App.Activity parent;

            public class ViewHolder : RecyclerView.ViewHolder 
            {
                public View View { get;set; }
                public TextView TextView { get; set; }
                public TextView TextView1 { get; set; }
                public EventHandler ClickHandler { get; set; }

                public ViewHolder (View view) : base (view) 
                {
                    View = view;
                    TextView = view.FindViewById<TextView> (Android.Resource.Id.Text1);
                }

                public override string ToString () 
                {
                    return base.ToString () + " '" + TextView.Text;
                }
            }

            public String GetValueAt (int position) 
            {
                return values[position];
            }

            public SimpleStringRecyclerViewAdapter (Android.App.Activity context, List<String> items) 
            {
                parent = context;
                context.Theme.ResolveAttribute (Resource.Attribute.selectableItemBackground, typedValue, true);
                background = typedValue.ResourceId;
                values = items;
            }

            public override RecyclerView.ViewHolder OnCreateViewHolder (ViewGroup parent, int viewType) 
            {                
                var view = LayoutInflater.From (parent.Context).Inflate(Resource.Layout.list_item, parent, false);
                view.SetBackgroundResource (background);
                return new ViewHolder(view);
            }

            public override void OnBindViewHolder (RecyclerView.ViewHolder holder, int position) 
            {
                var h = holder as ViewHolder;
                OrderFromHistory o = new OrderFromHistory();
                o= JsonConvert.DeserializeObject<OrderFromHistory>(values[position]);
                if (o == null || o.dep == "") { h.TextView.Text = "Вы еще не совершили ниодной поездки"; }
                else h.TextView.Text ="От "+ o.dep.Trim() + "\nдо " + o.arr.Trim() + "\nцена поездки: " + o.price.Trim()+ " руб.";
            }
                          
            public override int ItemCount { get { return values.Count; } }
        }
    }
}

