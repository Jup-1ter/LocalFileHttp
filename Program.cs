using System.Net;
using System.Text;
using MimeTypes;

string prefix = "http://localhost:80/file/";
using (HttpListener listener = new())
{
    listener.Prefixes.Add(prefix);
    listener.Start();

    while (true)
    {
        HttpListenerContext context = await listener.GetContextAsync();
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        if (request.HttpMethod == "GET" && (request.RemoteEndPoint.Address.Equals(IPAddress.Loopback) || request.RemoteEndPoint.Address.Equals(IPAddress.IPv6Loopback)))
        {
            string? filePath = request.QueryString["p"];
            if (!string.IsNullOrEmpty(filePath))
            {
                if (File.Exists(filePath))
                {
                    try
                    {
                        using FileStream fileStream = new(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                        response.AddHeader("Content-Disposition", $"inline");
                        response.ContentType = MimeTypeMap.GetMimeType(filePath);
                        response.ContentLength64 = fileStream.Length;
                        await fileStream.CopyToAsync(response.OutputStream);
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Error reading file: " + ex.Message));
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.NotFound;
                    await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("File not found"));
                }
            }
            else
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Missin 'p' parameter"));
            }
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.NotFound;
            await response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes("Invalid request type"));
        }

        response.OutputStream.Close();
    }
}