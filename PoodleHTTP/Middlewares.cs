﻿using PSMultiServer.PoodleHTTP.Addons.PlayStationHome.HELLFIREGAMES;
using PSMultiServer.PoodleHTTP.Addons.PlayStationHome.NDREAMS;
using PSMultiServer.PoodleHTTP.Addons.PlayStationHome.OHS;
using PSMultiServer.PoodleHTTP.Addons.PlayStationHome.UFC;
using PSMultiServer.PoodleHTTP.Addons.PlayStationHome.VEEMEE;
using Org.BouncyCastle.Asn1.Ocsp;
using System.Net;
using System.Text;
using System.Web;
using System;
using System.Net.Http;

namespace PSMultiServer.PoodleHTTP
{
    public delegate Task Middleware<in T>(T ctx, Func<Task> next);

    public static class Middlewares
    {
        public static Middleware<T> Empty<T>() => (_, next) => next();

        public static async Task Log(Context ctx, Func<Task> next)
        {
            HttpListenerRequest request = ctx.Request;
            HttpListenerResponse response = ctx.Response;

            ServerConfiguration.LogInfo($"[In] {request.HttpMethod} {request.RawUrl}");
            await next();
            ServerConfiguration.LogInfo($"[Out] {request.HttpMethod} [{response.StatusCode}] {request.RawUrl}");
        }

        public static async Task Execute(Context ctx, Func<Task> next)
        {
            using HttpListenerResponse response = ctx.Response;
            try
            {
                await next();
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 64)
            {
                // Unfortunately, some client side implementation of HTTP (like RPCS3) freeze the interface at regular interval.
                // This will cause server to throw error 64 (network interface not openned anymore)
                // In that case, we send internalservererror so client try again.

                response.StatusCode = (int)HttpStatusCode.InternalServerError;
            }
            catch (Exception ex)
            {
                ServerConfiguration.LogError($"Unexpected exception occurred - {ex}", ex);
                response.StatusCode = ex switch
                {
                    FileNotFoundException => 404,
                    DirectoryNotFoundException => 404,
                    UnauthorizedAccessException => 403,
                    _ => 500,
                };
            }
        }

        public static Middleware<Context> NotFound()
        {
            return async (ctx, _) =>
            {
                if (ctx.Request.UserAgent.Contains("PSHome")) // Home doesn't like data when something is not found.
                {
                    await ctx.Response.Error(false, 404);
                }
                else
                {
                    await ctx.Response.Error(true, 404);
                }
            };
        }

        public static Middleware<Context> StaticRoot(string route, string rootDir, string userAgentdrm)
        {
            return async (ctx, next) =>
            {
                string userAgent = ctx.Request.Headers["User-Agent"];

                if (userAgent == null || userAgent == "")
                {
                    await next();
                    return;
                }

                if (userAgentdrm != null && !userAgent.Contains(userAgentdrm))
                {
                    await next();
                    return;
                }

                if (ctx.Request.Url != null)
                {
                    // Don't use Request.RawUrl, because it contains url parameters. (e.g. '?a=1&b=2')
                    string absolutepath = ctx.Request.Url.AbsolutePath;

                    bool handled = absolutepath.StartsWith(route);
                    if (!handled)
                    {
                        await next();
                        return;
                    }

                    bool specialrequest = true;

                    if (userAgent.Contains("PSHome")) // Home has subservers running on Port 80.
                    {
                        string requesthost = ctx.Request.Headers["Host"];

                        if (requesthost != null && requesthost == "sonyhome.thqsandbox.com")
                            await UFCClass.processrequest(ctx.Request, ctx.Response);
                        else if (absolutepath.Contains("/ohs") || requesthost == "stats.outso-srv1.com")
                            await OHSClass.processrequest(ctx.Request, ctx.Response);
                        else if ((requesthost == "away.veemee.com" || requesthost == "home.veemee.com") && absolutepath.EndsWith(".php"))
                            await VEEMEEClass.processrequest(ctx.Request, ctx.Response);
                        else if (requesthost == "game2.hellfiregames.com" && (absolutepath.EndsWith(".php") || absolutepath == "/Postcards/"))
                            await HELLFIREGAMESClass.processrequest(ctx.Request, ctx.Response);
                        else if (requesthost == "pshome.ndreams.net" && absolutepath.EndsWith(".php"))
                            await NDREAMSClass.processrequest(ctx.Request, ctx.Response);
                        else
                            specialrequest = false;
                    }
                    else if (absolutepath.EndsWith(".php"))
                    {
                        if (!Directory.Exists(Directory.GetCurrentDirectory() + "/static/PHP"))
                        {
                            byte[] fileBuffer = Encoding.UTF8.GetBytes(PreMadeWebPages.phpnotenabled);

                            ctx.Response.ContentType = "text/html";
                            ctx.Response.StatusCode = 404;

                            if (ctx.Response.OutputStream.CanWrite)
                            {
                                try
                                {
                                    ctx.Response.ContentLength64 = fileBuffer.Length;
                                    ctx.Response.OutputStream.Write(fileBuffer, 0, fileBuffer.Length);
                                    ctx.Response.OutputStream.Close();
                                }
                                catch (Exception)
                                {
                                    // Not Important.
                                }
                            }
                        }
                        else
                        {
                            byte[] fileBuffer = await FileHelper.CryptoReadAsync(Directory.GetCurrentDirectory() + $"{ServerConfiguration.HTTPStaticFolder}{absolutepath}", HTTPPrivateKey.HTTPPrivatekey); ;

                            if (Misc.FindbyteSequence(fileBuffer, new byte[] { 0x3c, 0x3f, 0x70, 0x68, 0x70 }))
                            {
                                fileBuffer = await Extensions.ProcessPhpPage(Directory.GetCurrentDirectory() + $"{ServerConfiguration.HTTPStaticFolder}{absolutepath}", ServerConfiguration.PHPVersion, ctx.Request, ctx.Response);

                                ctx.Response.ContentType = "text/html";
                                ctx.Response.StatusCode = 200;

                                if (ctx.Response.OutputStream.CanWrite)
                                {
                                    try
                                    {
                                        ctx.Response.ContentLength64 = fileBuffer.Length;
                                        ctx.Response.OutputStream.Write(fileBuffer, 0, fileBuffer.Length);
                                        ctx.Response.OutputStream.Close();
                                    }
                                    catch (Exception)
                                    {
                                        // Not Important.
                                    }
                                }
                            }
                            else
                            {
                                ctx.Response.ContentType = "text/plain";
                                ctx.Response.StatusCode = 200;

                                if (ctx.Response.OutputStream.CanWrite)
                                {
                                    try
                                    {
                                        ctx.Response.ContentLength64 = fileBuffer.Length;
                                        ctx.Response.OutputStream.Write(fileBuffer, 0, fileBuffer.Length);
                                        ctx.Response.OutputStream.Close();
                                    }
                                    catch (Exception)
                                    {
                                        // Not Important.
                                    }
                                }
                            }
                        }
                    }
                    else
                        specialrequest = false;

                    if (!specialrequest)
                    {
                        string url = ctx.Request.Url.LocalPath;

                        // Split the URL into segments
                        string[] segments = url.Trim('/').Split('/');

                        // Combine the folder segments into a directory path
                        string directoryPath = Path.Combine(Directory.GetCurrentDirectory() + $"/{ServerConfiguration.HTTPStaticFolder}", string.Join("/", segments.Take(segments.Length - 1).ToArray()));

                        // Process the request based on the HTTP method
                        string filePath = Path.Combine(Directory.GetCurrentDirectory() + $"/{ServerConfiguration.HTTPStaticFolder}", url.Substring(1));

                        switch (ctx.Request.HttpMethod)
                        {
                            case HttpMethods.Get:
                                await ReadLocalFile(filePath, ctx.Request, ctx.Response);
                                break;
                            case HttpMethods.Put:
                                await WriteLocalFile(filePath, ctx.Request);
                                ctx.Response.StatusCode = 204;
                                break;
                        }
                    }
                }
            };
        }

        private static async Task ReadLocalFile(string filePath, HttpListenerRequest request, HttpListenerResponse response)
        {
            if (File.Exists(filePath))
                if (request.Headers["Accept-Encoding"] != null && request.Headers["Accept-Encoding"].Contains("gzip"))
                    await response.FileProcess(filePath, true, request);
                else
                    await response.FileProcess(filePath, false, request);
            else
            {
                ServerConfiguration.LogWarn($"HTTP : {request.UserAgent} Demands a non-existing file: '{filePath}'.");
                if (request.UserAgent.Contains("PSHome"))
                    await response.Error(false, 404);
                await response.Error(true, 404);
            }
        }

        private static Task WriteLocalFile(string filePath, HttpListenerRequest request)
        {
            return FileHelper.WriteAsync(filePath, stream => request.InputStream.CopyToAsync(stream));
        }

        public static Middleware<T> Then<T>(this Middleware<T> middleware, Middleware<T> nextMiddleware)
        {
            return (ctx, next) => middleware(ctx, () => nextMiddleware(ctx, next));
        }

        public static Task Run<T>(this Middleware<T> middleware, T ctx)
        {
            return middleware(ctx, () =>

                Task.CompletedTask

            );
        }
    }
}
