using System;
using System.Net;

namespace Webserver
{
    class Program
    {
        static void Main(string[] args)
        {
            Controller server = new Controller();
            IPAddress ip;
            ip = IPAddress.Parse("127.0.0.1");//localhost
            server.start(ip,80,1, "C:/Users/user/source/repos/Webserver/Webserver/");//start
        }
    }
}
