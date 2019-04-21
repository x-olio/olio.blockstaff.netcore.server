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
            //config api

            server.SetHttpAction("/getraw", http_GetRaw);
            server.SetHttpAction("/uploadraw", http_UploadRaw);
            server.SetHttpAction("/getuserasset", http_GetUserNamedAsset);
            server.AddJsonRPC("/rpc", "help", rpc_Help);
            server.AddJsonRPC("/rpc", "user_new", rpc_UserNew);
            server.AddJsonRPC("/rpc", "user_login", rpc_UserLogin);
            server.AddJsonRPC("/rpc", "user_setnamedasset", rpc_SetUserNamedAsset);
            server.AddJsonRPC("/rpc", "user_listnamedasset", rpc_ListUserNamedAsset);
            server.Start(config.ServerPort);
            logger.Info("http server on=" + config.ServerPort);


            logger.Warn("Init DB");
            //config database
            var dbpath = System.IO.Path.GetFullPath(config.DBPath);
            this.db = new LightDB();
            var firstTask = new WriteTask();
            firstTask.CreateTable(new TableInfo(tableid_User, "user", "", DBValue.Type.String));
            firstTask.CreateTable(new TableInfo(tableid_RawAsset, "rawasset", "", DBValue.Type.Bytes));
            this.db.Open(dbpath, new DBCreateOption() { MagicStr = "imageserver", FirstTask = firstTask });
            logger.Info("dbpath=" + dbpath);
            using (var snap = db.UseSnapShot())
            {
                logger.Info("cur height=" + snap.DataHeight);
            }

        }
    }

}