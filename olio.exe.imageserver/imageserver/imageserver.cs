using System;
using System.Collections.Generic;
using System.Text;

namespace OLIO.ImageServer
{
    class ImageServer
    {
        OLIO.Log.ILogger logger;
        public void Start(OLIO.Log.ILogger logger)
        {
            this.logger = logger;

            var configstr = System.IO.File.ReadAllText("config.json");
            var config = Config.Parse(configstr);


            OLIO.http.server.httpserver server = new http.server.httpserver();
            server.SetHttpAction("/", async (context) =>
            {
                byte[] writedata = System.Text.Encoding.UTF8.GetBytes("pleaseuse json rpc 2.0");
                await context.Response.Body.WriteAsync(writedata);
            });
            server.SetJsonRPCFail("/rpc", async (jobject, errmsg) =>
             {
                 OLIO.http.server.JSONRPCController.ErrorObject errobj = new http.server.JSONRPCController.ErrorObject();
                 errobj.message = "error json rpc";
                 return errobj;

             });
            server.AddJsonRPC("/rpc", "help", async (jobject) =>
              {
                  Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();
                  return obj;
              });
            server.Start(config.ServerPort);
            logger.Info("http server on=" + config.ServerPort);

        }
    }
}
