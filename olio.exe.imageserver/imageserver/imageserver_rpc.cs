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
    }

}