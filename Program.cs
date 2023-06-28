

using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System.Net.Sockets;
using System.Net;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System;


class Program
{
    static async Task Main()
    {
        TcpListener listener = new TcpListener(IPAddress.Any, 8080);
        listener.Start();
        Console.WriteLine("Waiting for requests on port 8080...");

        var connectionObservable = Observable.FromAsync(() => listener.AcceptTcpClientAsync())
            .Repeat()
            .Publish();

        using (var connectionDisposable = connectionObservable.Connect())
        {
            connectionObservable.Subscribe(async client =>
            {
                try
                {
                    await ProcessRequest(client);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error processing request: " + ex.Message);
                }
            });

            await Task.Delay(-1);
        }
    }

    static async Task ProcessRequest(TcpClient client)
    {
        NetworkStream stream = client.GetStream();
        StreamReader reader = new StreamReader(stream);
        StreamWriter writer = new StreamWriter(stream);

        try
        {
            string request = await reader.ReadLineAsync();

            if (request != null)
            {
                Console.WriteLine("Received request: " + request);

                string[] parts = Regex.Split(request, @"\s+");

                if (parts.Length == 3 && parts[0] == "GET")
                {
                    string videoId = parts[1].Substring(1);

                    Console.WriteLine("Video ID: " + videoId);
                    var youtubeService = new YouTubeService(new BaseClientService.Initializer
                    {
                        ApiKey = "AIzaSyCRnQx6YmGi6E0b9i3INE7IwoQIN79w4yk"
                    });


                    var videoRequest = youtubeService.Videos.List("statistics");
                    videoRequest.Id = videoId;

                    var videoResponse = videoRequest.Execute();

                    var video = videoResponse.Items[0];
                    var likes = video.Statistics.LikeCount;
                    var views = video.Statistics.ViewCount;
                    var udeo = (double)likes / views * 100;


                    Console.WriteLine($"Analyzing comments for video ID: {videoId}");
                    Console.WriteLine($"Broj lajkova : {likes}");
                    Console.WriteLine($"Broj pregleda : {views}");
                    Console.WriteLine($"Udeo lajkova u pregledima : {udeo:F3}");
                    StringBuilder responseBuilder = new StringBuilder();

                    responseBuilder.AppendLine("<table>");

                  
                        responseBuilder.AppendLine($"<td>Lajkovi : {likes}</td><td>Pregledi : {views}</td><td>Udeo lajkova u broju pregleda : {udeo:F3}%</td>");
                    


                    responseBuilder.AppendLine("</table>");

                    string response = responseBuilder.ToString();

                    WriteResponse(response, writer);
                }
                else
                {
                    Console.WriteLine("Bad request: " + request);
                    WriteBadRequestResponse(writer);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error processing request: " + ex.Message);
            WriteServerErrorResponse(writer);
        }
        finally
        {
            writer.Flush();
            writer.Close();
            reader.Close();
            stream.Close();
            client.Close();
        }
    }

    static void WriteResponse(string response, StreamWriter writer)
    {
        writer.WriteLine("HTTP/1.1 200 OK");
        writer.WriteLine("Content-Type: text/html; charset=UTF-8");
        writer.WriteLine("Connection: close");
        writer.WriteLine();
        writer.WriteLine("<!DOCTYPE html>");
        writer.WriteLine("<html>");
        writer.WriteLine("<head>");
        writer.WriteLine("<title>Analiza youtube videa</title>");
        writer.WriteLine("<style>");
        writer.WriteLine("table { border-collapse: collapse; width: 100%; }");
        writer.WriteLine("th, td { text-align: left; padding: 8px; }");
        writer.WriteLine("tr:nth-child(even) { background-color: #f2f2f2; }");
        writer.WriteLine("</style>");
        writer.WriteLine("</head>");
        writer.WriteLine("<body>");
        writer.WriteLine("<h1>Rezultati analize youtube video snimka</h1>");
        writer.WriteLine(response);
        writer.WriteLine("</body>");
        writer.WriteLine("</html>");
    }

    static void WriteBadRequestResponse(StreamWriter writer)
    {
        writer.WriteLine("HTTP/1.1 400 Bad Request");
        writer.WriteLine("Content-Type: text/plain; charset=UTF-8");
        writer.WriteLine("Connection: close");
        writer.WriteLine();
        writer.WriteLine("Bad Request");
    }

    static void WriteServerErrorResponse(StreamWriter writer)
    {
        writer.WriteLine("HTTP/1.1 500 Internal Server Error");
        writer.WriteLine("Content-Type: text/plain; charset=UTF-8");
        writer.WriteLine("Connection: close");
        writer.WriteLine();
        writer.WriteLine("Internal Server Error");
    }
}


