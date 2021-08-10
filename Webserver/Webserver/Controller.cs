//Made by Oliver
// 
//use:
//http://127.0.0.1/ || http://127.0.0.1/index.html
//
//info:
//Webserver that returns html thru http
//
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Webserver
{
    class Controller
    {
        public bool active = false;//Help us to get the state
        private int timeout = 8;
        private Encoding charEncoder = Encoding.UTF8; //String encoder
        private Socket serversocket; // server socket
        private string contentPath; // Root path



        private Dictionary<string, string> dict = new Dictionary<string, string>()//type of files we allow
        {
            { "htm", "text/html" },
            { "html", "text/html" },
            { "xml", "text/xml" },
            { "txt", "text/plain" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "jpg", "image/jpg" },
            { "jpeg", "image/jpeg" },
            { "zip", "application/zip"}
        };

        public bool start(IPAddress ipAddress,int port, int maxNoFCon,string contentPath)
        {
            if (active) return false; // if it is active dont try to start again

            try
            {

                serversocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);//initializes a new instance of socket and fills it with the "rules" or settings that we want
                serversocket.Bind(new IPEndPoint(ipAddress, port));//Associate the new socket with our local endpoint
                serversocket.Listen(maxNoFCon);//Socket goes into listening state
                serversocket.ReceiveTimeout = timeout;
                serversocket.SendTimeout = timeout;
                active = true;
                this.contentPath = contentPath;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }

            Thread requestListener = new Thread(() =>//New thread
            {
                while (active)
                {
                    Socket clientSocket;//clientSocket is now a socket our client socket
                    try
                    {
                        clientSocket = serversocket.Accept();//returns socket for a new connection

                        Thread requestHandler = new Thread(() =>
                        {
                            clientSocket.ReceiveTimeout = timeout;
                            clientSocket.SendTimeout = timeout;
                            try
                            {
                                handleTheRequest(clientSocket);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e);
                                try
                                {
                                    clientSocket.Close();//Close client connection if request failed
                                }
                                catch (Exception er)
                                {
                                    Console.WriteLine(er);
                                }
                            }
                        });
                        requestHandler.Start();//starts the new thread
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            });
            requestListener.Start();

            return true;
        }
        public void stop()//Stops the server and resets it
        {
            if (active)
            {
                active = false;
                try
                {
                    serversocket.Close();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                serversocket = null;
            }
        }
        private void handleTheRequest(Socket clientSocket)
        {
            byte[] buffer = new byte[10240];//Deciding we  wanna recive a buffer with the size of 10kb
            int receivedBCount = clientSocket.Receive(buffer);
            string strReceived = charEncoder.GetString(buffer, 0, receivedBCount);//encode to UTF8

            string httpMethod = strReceived.Substring(0, strReceived.IndexOf(" ")); // cuts out the Method in the string

            int start = strReceived.IndexOf(httpMethod) + httpMethod.Length + 1;//Get the start index of the value by adding one to httpmethod as the string goes something like  "get value"
            int length = strReceived.LastIndexOf("HTTP") - start - 1; //Gets lenght of the whole value
            string requestedUrl = strReceived.Substring(start, length); //Gets the whole value 

            string requestedFile;
            if (httpMethod.Equals("GET") ||
                    httpMethod.Equals("POST"))
                requestedFile = requestedUrl.Split('?')[0];
            else
            {
                Console.WriteLine("Error: line 128");
                return;
            }
            requestedFile = requestedFile.Replace("/", @"/").Replace("\\..", "");
            start = requestedFile.LastIndexOf('.') + 1;
            if (start > 0)
            {
                length = requestedFile.Length - start;
                string extension = requestedFile.Substring(start, length);
                if (dict.ContainsKey(extension))// Check if we support this extension
                {
                    if(File.Exists(contentPath + requestedFile))
                    {
                        sendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile), dict[extension]);
                    }
                    else
                    {
                        Console.WriteLine("Error: line 145");
                    }
                }
            }
            else//Open default file as index.html
            {
                if(requestedFile.Substring(length - 1, 1) != @"\")
                {
                    requestedFile += @"\";
                }
                if(File.Exists(contentPath + requestedFile + "\\index.html")){
                    sendOkResponse(clientSocket, File.ReadAllBytes(contentPath + requestedFile + "\\index.html"),"text/html");
                }
                else
                {
                    Console.WriteLine("Error: line 160");
                }

            }
        }
        private void sendOkResponse(Socket clientSocket, byte[] bContent, string contentType)
        {
            sendResponse(clientSocket, bContent, "200 ok", contentType);
        }
        private void sendResponse(Socket clientSocket, byte[] bContent, string responseCode, string contentType)//Returns whatever
        {
            try
            {
                byte[] bheader = charEncoder.GetBytes("HTTP/1.1 " + responseCode + "\r\n"
                          + "Server: Atasoy Simple Web Server\r\n"
                          + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                          + "Connection: close\r\n"
                          + "Content-Type: " + contentType + "\r\n\r\n");
                //First we send the header and then the content and then we close the connection to the client
                clientSocket.Send(bheader);
                clientSocket.Send(bContent);
                clientSocket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

    }
}
