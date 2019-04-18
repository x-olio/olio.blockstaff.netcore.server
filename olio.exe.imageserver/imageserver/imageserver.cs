using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OLIO.ImageServer
{
    class ImageServer
    {
        OLIO.Log.ILogger logger;
        OLIO.LightDB db;
        public void Start(OLIO.Log.ILogger logger)
        {
            this.logger = logger;

            var configstr = System.IO.File.ReadAllText("config.json");
            var config = Config.Parse(configstr);


            logger.Warn("Init Http Server");

            //config http server
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
            server.AddJsonRPC("/rpc", "help", rpc_Help);

            server.Start(config.ServerPort);
            logger.Info("http server on=" + config.ServerPort);


            logger.Warn("Init DB");
            //config database
            var dbpath = System.IO.Path.GetFullPath(config.DBPath);
            this.db = new LightDB();
            this.db.Open(dbpath, new DBCreateOption() { MagicStr = "imageserver" });
            logger.Info("dbpath=" + dbpath);
            using (var snap = db.UseSnapShot())
            {
                logger.Info("cur height=" + snap.DataHeight);
            }

        }
        async Task<JObject> rpc_Help(JObject requestobj)
        {
            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();
            obj["msg"] = "this is help method";
            return obj;
        }
        async Task<JObject> rpc_UserNew(JObject requestobj)
        {
            return null;
        }
        async Task<JObject> rpc_UserLogin(JObject requestobj)
        {
            return null;
        }
        async Task<JObject> rpc_UserNamedAsset(JObject requestobj)
        {
            return null;
        }
        //全局资产不需要jsonrpc 控制，这个谁都行
        //async Task<JObject> rpc_SetGUIDAsset(JObject requestobj)
        //{

        //}
    }
}