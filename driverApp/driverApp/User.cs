namespace Cheesesquare
{
    class user
    {
        public static string number = "";
        public static string city = "3";
        public static string name = "";
        public static string surname = "";
        public static string date_burn = "";
        public static string cod = "";
    }

    class Order
    {
        public static string id="";
        public static string dep = "";
        public static string arr = "";
        public static string price = "";
        public static string yardage = "";

        public static string Yardage { get; internal set; }
    }

    public class OrderFromHistory
    {
        public string id { get; set; }
        public string dep { get; set; }
        public string arr { get; set; }
        public string date { get; set; }
        public string price { get; set; }
    }
}