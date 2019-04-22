using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace OLIO.ImageServer
{
    class Config
    {
        public int ServerPort;
        public int ServerPortHttps = 0;
        public string PFXPath = null;
        public string PFXPassword = null;

        public string DBPath;
        static string[] GetStringArrayFromJson(JArray jarray)
        {
            var strs = new string[jarray.Count];
            for (var i = 0; i < jarray.Count; i++)
            {
                strs[i] = (string)jarray[i];
            }
            return strs;
        }
        private Config()
        {

        }
        public static Config Parse(string txt)
        {
            var jobj = JObject.Parse(txt);
            Config c = new Config();
            c.ServerPort = (int)jobj["ServerPort"];
            c.DBPath = (string)jobj["DBPath"];
            if(jobj.ContainsKey("ServerPortHttps"))
            {
                c.ServerPortHttps = (int)jobj["ServerPortHttps"];
                c.PFXPath = (string)jobj["PFXPath"];
                c.PFXPassword = (string)jobj["PFXPassword"];
            }
            return c;
        }
    }
}
