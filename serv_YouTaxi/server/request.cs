using System.Collections.Generic;

namespace YouTaxi_server
{
    public class Request
    {
        public string who { get; set; }
        public string command { get; set; }
        public List<string> parameters { get; set; }
    }

    public class Response
    {
        public string status { get; set; }
        public string cod { get; set; }
        public List<string> argument { get; set; }
    }

    public class Order
    {
        public string id { get; set; }
        public string dep { get; set; }
        public string arr { get; set; }
        public string date { get; set; }
        public string price { get; set; }
        public string yardage { get; set; }
        public string move { get; set; }
    }

    public class Distance
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class Duration
    {
        public string text { get; set; }
        public int value { get; set; }
    }

    public class Element
    {
        public Distance distance { get; set; }
        public Duration duration { get; set; }
        public string status { get; set; }
    }

    public class Row
    {
        public List<Element> elements { get; set; }
    }

    public class GoogleDistance
    {
        public List<string> destination_addresses { get; set; }
        public List<string> origin_addresses { get; set; }
        public List<Row> rows { get; set; }
        public string status { get; set; }
    }
}
