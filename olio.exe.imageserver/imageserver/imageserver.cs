using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OLIO.ImageServer
{
    class ImageServer
    {
        OLIO.Log.ILogger logger;
        OLIO.LightDB db;
        static readonly byte[] tableid_User = new byte[] { 0x01, 0x01 };
        static readonly byte[] tableid_RawAsset = new byte[] { 0x01, 0x00 };
        static byte[] GetUserNamedAssetTableID(string user)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(user);
            return tableid_User.Concat(bytes).ToArray();
        }
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
            server.SetHttpAction("/getusernamedasset", http_GetUserNamedAsset);
            server.AddJsonRPC("/rpc", "help", rpc_Help);
            server.AddJsonRPC("/rpc", "user_new", rpc_UserNew);
            server.AddJsonRPC("/rpc", "user_login", rpc_UserLogin);
            server.AddJsonRPC("/rpc", "user_namedasset", rpc_UserNamedAsset);

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
        byte[] db_GetRaw(byte[] id)
        {
            using (var snap = this.db.UseSnapShot())
            {
                var dbv = snap.GetValue(tableid_RawAsset, id);
                if (dbv == null)
                    return null;
                if (dbv.type == DBValue.Type.Deleted)
                    return null;
                return dbv.value;
            }
        }
        byte[] db_SaveRaw(byte[] asset)
        {
            var writetask = new WriteTask();
            byte[] key = Tool.Sha256(asset);
            writetask.Put(tableid_RawAsset, key, DBValue.FromValue(DBValue.Type.Bytes, asset));
            this.db.Write(writetask);
            return key;
        }
        bool db_UserNew(string id, byte[] passhash)
        {
            var key = System.Text.Encoding.UTF8.GetBytes(id);
            using (var snap = this.db.UseSnapShot())
            {
                var data = snap.GetValue(tableid_User, key);
                if (data != null)
                    return false;
                var writetask = new WriteTask();
                writetask.Put(tableid_User, key, DBValue.FromValue(DBValue.Type.Bytes, passhash));
                this.db.Write(writetask);
                return true;
            }
        }
        bool db_SetNamedAsset(string user, string key, byte[] data)
        {
            var tableid = GetUserNamedAssetTableID(user);
            using (var snap = this.db.UseSnapShot())
            {
                var writetask = new WriteTask();

                var table = snap.GetTableInfo(tableid);
                if (table == null)
                {
                    writetask.CreateTable(new TableInfo(tableid, "userasset_" + user, "", DBValue.Type.String));
                }
                writetask.Put(tableid, System.Text.Encoding.UTF8.GetBytes(key), DBValue.FromValue(DBValue.Type.Bytes, data));

                this.db.Write(writetask);
                return true;
            }
        }
        byte[] db_GetNamedAsset(string user, string key)
        {
            var tableid = GetUserNamedAssetTableID(user);
            using (var snap = this.db.UseSnapShot())
            {
                var dbv = snap.GetValue(tableid, System.Text.Encoding.UTF8.GetBytes(key));
                return dbv.value;
            }
        }
        System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> tokens =
        new System.Collections.Concurrent.ConcurrentDictionary<string, byte[]>();
        byte[] UserLogin(string id, byte[] passhash)
        {
            using (var snap = db.UseSnapShot())
            {
                var user = snap.GetValue(tableid_User, System.Text.Encoding.UTF8.GetBytes(id));
                if (user == null || user.value == null || user.value.Length == 0)
                    return null;
                if (Tool.BytesEqual(user.value, passhash))
                {
                    tokens[id] = Tool.RanToken(id);
                    return tokens[id];
                }
                else
                {
                    return null;
                }
            }
        }
        bool CheckUserLogin(string id, byte[] token)
        {
            var getb = tokens.TryGetValue(id, out byte[] outv);
            if (!getb)
                return false;
            return Tool.BytesEqual(outv, token);
        }
        async Task<JObject> rpc_Help(JObject requestobj)
        {
            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();
            obj["msg"] = "this is help method";
            return obj;
        }
        async Task<JObject> rpc_UserNew(JObject requestobj)
        {
            string user = null;
            byte[] passhash = null;

            bool paramfail = false;
            try
            {
                user = (string)requestobj["params"][0];
                if(user==null)
                    throw new Exception("error param:user"); 
            }
            catch
            {
                paramfail = true;
            }

            try
            {
                passhash = Tool.HexDecode((string)requestobj["params"][1]);
            }
            catch
            {
                paramfail = true;
            }

            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();

            if (paramfail)
            {
                obj["result"] = false;
                obj["msg"] = "param fail,need string 'user',hexstr 'passhash'";
                return obj;
            }

            var b = db_UserNew(user, passhash);
            obj["result"] = b;
            return obj;
        }
        async Task<JObject> rpc_UserLogin(JObject requestobj)
        {
            var user = (string)requestobj["user"];
            var passhash = Tool.HexDecode((string)requestobj["passhash"]);
            var token = UserLogin(user, passhash);
            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();
            obj["token"] = token;
            return obj;
        }
        async Task<JObject> rpc_UserNamedAsset(JObject requestobj)
        {
            var user = (string)requestobj["user"];
            var token = Tool.HexDecode((string)requestobj["token"]);
            var blogin = CheckUserLogin(user, token);
            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();

            if (blogin == false)
            {
                obj["result"] = false;
                obj["msg"] = "login fail.";
                return obj;
            }
            var key = (string)requestobj["key"];
            var value = Tool.HexDecode((string)requestobj["value"]);
            var b = db_SetNamedAsset(user, key, value);
            obj["result"] = b;
            return obj;

        }
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
            var user = formdat.mapParams["user"];
            var key = formdat.mapParams["key"];
            var data = db_GetNamedAsset(user, key);
            if (data == null)
            {
                context.Response.StatusCode = 404;
            }
            else
            {
                await context.Response.Body.WriteAsync(data);
            }
        }
    }

}