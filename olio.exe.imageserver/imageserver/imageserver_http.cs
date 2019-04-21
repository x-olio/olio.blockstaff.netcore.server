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
            context.Response.ContentType = "application/json;charset=UTF-8";

            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";

            var formdat = await OLIO.http.server.FormData.FromRequest(context.Request);
            byte[] id = null;
            try
            {
                id = Tool.HexDecode(formdat.mapParams["id"]);
            }
            catch
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("{\"result\":false,\"msg\":\"error param.\"}");
                return;
            }
            var data = db_GetRaw(id);
            string format = null;
            try
            {
                format = formdat.mapParams["format"].ToLower();
            }
            catch
            {

            }
            if (data == null)
            {
                context.Response.StatusCode = 404;
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
        async Task http_UploadRaw(HttpContext context)
        {
            context.Response.ContentType = "application/json;charset=UTF-8";

            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";

            var formdat = await OLIO.http.server.FormData.FromRequest(context.Request);
            byte[] data = null;
            foreach (var f in formdat.mapFiles.Values)
            {
                data = f;
                break;
            }
            if (data == null)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("{\"result\":false,\"msg\":\"error file\"}");
                return;
            }
            string user = null;
            byte[] token = null;
            try
            {
                user = formdat.mapParams["user"];
                token = Tool.HexDecode(formdat.mapParams["token"]);
            }
            catch
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("{\"result\":false,\"msg\":\"error param.\"}");
                return;

            }
            var b = CheckUserLogin(user, token);
            if (!b)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("{\"result\":false,\"msg\":\"login fail\"}");
                return;
            }
            //Save this 
            var key = db_SaveRaw(data);
            var outjson = "{\"result\":true,\"id\":\"" + Tool.HexEncode(key) + "\"}";
            await context.Response.WriteAsync(outjson);
        }
        async Task http_GetUserNamedAsset(HttpContext context)
        {
            context.Response.ContentType = "application/json;charset=UTF-8";

            context.Response.Headers["Access-Control-Allow-Origin"] = "*";
            context.Response.Headers["Access-Control-Allow-Methods"] = "GET, POST";
            context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type";
            context.Response.Headers["Access-Control-Max-Age"] = "31536000";
            var formdat = await OLIO.http.server.FormData.FromRequest(context.Request);
            string user = null;
            string key = null;
            string format = null;
            try
            {
                user = formdat.mapParams["user"];
                key = formdat.mapParams["key"];
              
                if (user == null || key == null)
                    throw new Exception();
            }
            catch
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("need params user & key");
                return;
            }
            try
            {
                format = formdat.mapParams["format"].ToLower();
            }
            catch
            {

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