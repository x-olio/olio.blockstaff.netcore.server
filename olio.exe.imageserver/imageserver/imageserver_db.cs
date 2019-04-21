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
        static readonly byte[] tableid_User = new byte[] { 0x01, 0x01 };
        static readonly byte[] tableid_RawAsset = new byte[] { 0x01, 0x00 };
        static byte[] GetUserNamedAssetTableID(string user)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(user);
            return tableid_User.Concat(bytes).ToArray();
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
    }

}