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
                if (dbv == null || dbv.value == null || dbv.value.Length == 0)
                    return null;
                return dbv.value;
            }
        }
        string[] db_ListNamedAsset(string user)
        {
            List<string> list = new List<string>();
            var tableid = GetUserNamedAssetTableID(user);
            using (var snap = this.db.UseSnapShot())
            {
                var keyfinder = snap.CreateKeyFinder(tableid);
                foreach (var key in keyfinder)
                {
                    list.Add(System.Text.Encoding.UTF8.GetString(key));
                }
            }
            return list.ToArray();
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
                if (user == null)
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
            bool paramfail = false;
            string user = null;
            byte[] passhash = null;
            try
            {
                user = (string)requestobj["params"][0];
                passhash = Tool.HexDecode((string)requestobj["params"][1]);
            }
            catch
            {
                paramfail = true;
            }
            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();
            if (paramfail == true)
            {
                obj["result"] = false;
                obj["msg"] = "param fail,need string 'user',hexstr 'passhash'";
                return obj;
            }
            var token = UserLogin(user, passhash);
            obj["result"] = token != null;
            if (token != null)
            {
                obj["token"] = Tool.HexEncode(token);
            }

            return obj;
        }
        async Task<JObject> rpc_SetUserNamedAsset(JObject requestobj)
        {
            string user = null;
            byte[] token = null;
            string key = null;
            byte[] data = null;
            bool paramfail = false;
            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();
            try
            {
                user = (string)requestobj["params"][0];
            }
            catch
            {
                paramfail = true;
                obj["msg"] = "error param user,need[user(string),token(hexstr),key(string),data(hexstr)]";
            }
            if (!paramfail)
                try
                {
                    token = Tool.HexDecode((string)requestobj["params"][1]);
                }
                catch
                {
                    paramfail = true;
                    obj["msg"] = "error param token,need[user(string),token(hexstr),key(string),data(hexstr)]";
                }
            if (!paramfail)

                try
                {
                    key = (string)requestobj["params"][2];
                }
                catch
                {
                    paramfail = true;
                    obj["msg"] = "error param key,need[user(string),token(hexstr),key(string),data(hexstr)]";
                }
            if (!paramfail)

                try
                {
                    data = Tool.HexDecode((string)requestobj["params"][3]);
                }
                catch
                {
                    paramfail = true;
                    obj["msg"] = "error param data,need[user(string),token(hexstr),key(string),data(hexstr)]";
                }
            if (paramfail)
            {
                obj["result"] = false;
                return obj;
            }

            var blogin = CheckUserLogin(user, token);

            if (blogin == false)
            {
                obj["result"] = false;
                obj["msg"] = "login fail.";
                return obj;
            }
            var b = db_SetNamedAsset(user, key, data);
            obj["result"] = b;
            return obj;

        }
        async Task<JObject> rpc_ListUserNamedAsset(JObject requestobj)
        {
            string user = null;
            byte[] token = null;
            bool paramfail = false;
            Newtonsoft.Json.Linq.JObject obj = new Newtonsoft.Json.Linq.JObject();
            try
            {
                user = (string)requestobj["params"][0];
            }
            catch
            {
                paramfail = true;
                obj["msg"] = "error param user,need[user(string),token(hexstr),key(string),data(hexstr)]";
            }
            if (!paramfail)
                try
                {
                    token = Tool.HexDecode((string)requestobj["params"][1]);
                }
                catch
                {
                    paramfail = true;
                    obj["msg"] = "error param token,need[user(string),token(hexstr),key(string),data(hexstr)]";
                }

            if (paramfail)
            {
                obj["result"] = false;
                return obj;
            }

            var blogin = CheckUserLogin(user, token);

            if (blogin == false)
            {
                obj["result"] = false;
                obj["msg"] = "login fail.";
                return obj;
            }
            string[] names = db_ListNamedAsset(user);
            var array = new Newtonsoft.Json.Linq.JArray();
            obj["list"] = array;
            foreach (var item in names)
            {
                array.Add(item);
            }
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


                        { await context.Response.WriteAsync(txt);
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
                else if(format=="image")
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