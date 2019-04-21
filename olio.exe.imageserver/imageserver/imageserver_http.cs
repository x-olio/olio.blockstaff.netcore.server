using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLIO.ImageServer
{
    partial class ImageServer
    {
        async Task http_GetRaw(HttpContext context)
        {
            var formdat = await OLIO.http.server.FormData.FromRequest(context.Request);
            var id = Tool.HexDecode(formdat.mapParams["id"]);
            var data = db_GetRaw(id);
            if (data == null)
            {
                context.Response.StatusCode = 404;
            }
            else
            {
                await context.Response.Body.WriteAsync(data);
            }
        }
        async Task http_UploadRaw(HttpContext context)
        {
            var formdat = await OLIO.http.server.FormData.FromRequest(context.Request);
            byte[] data = null;
            foreach (var f in formdat.mapFiles.Values)
            {
                data = f;
                break;
            }
            var user = formdat.mapParams["user"];
            var token = Tool.HexDecode(formdat.mapParams["token"]);
            var b = CheckUserLogin(user, token);
            if (!b)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("login fail");
                return;
            }
            //Save this 
            var key = db_SaveRaw(data);
            await context.Response.Body.WriteAsync(key);
        }
        async Task http_GetUserNamedAsset(HttpContext context)
        {
            var formdat = await OLIO.http.server.FormData.FromRequest(context.Request);
            string user = null;
            string key = null;
            string format = null;
            try
            {
                user = formdat.mapParams["user"];
                key = formdat.mapParams["key"];
                format = formdat.mapParams["format"].ToLower();
                if (user == null || key == null)
                    throw new Exception();
            }
            catch
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("need params user & key");
                return;
            }
            var data = db_GetNamedAsset(user, key);
            if (data == null)
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("not found asset");
            }
            else
            {
                if (format == "hexstr")
                {
                    var txt = Tool.HexEncode(data);
                    context.Response.ContentType = "text/plain";


                    {
                        await context.Response.WriteAsync(txt);
                    }
                    return;
                }
                else if (format == "string")
                {
                    context.Response.ContentType = "text/plain";

                    var txt = System.Text.Encoding.UTF8.GetString(data);
                    await context.Response.WriteAsync(txt);
                    return;

                }
                else if (format == "image")
                {
                    context.Response.ContentType = "image/png";
                    await context.Response.Body.WriteAsync(data);

                }
                else
                {
                    context.Response.ContentType = "application/octet-stream";
                    await context.Response.Body.WriteAsync(data);
                }
            }
        }
    }

}