using Network;
using System;
using System.IO;
using UnityEngine;
using PBMessage;

namespace Network
{
    public static class ProtoTransfer
    {
        public static byte[] SerializeProtoBuf<T>(T data) where T : class, ProtoBuf.IExtensible
        {
            using (MemoryStream ms = new MemoryStream())
            {
                ProtoBuf.Serializer.Serialize<T>(ms, data);
                byte[] bytes = ms.ToArray();

                ms.Close();

                return bytes;
            }
        }

        public static T DeserializeProtoBuf<T>(MessageBuffer buffer) where T : class, ProtoBuf.IExtensible
        {
            return DeserializeProtoBuf<T>(buffer.body());
        }

        public static T DeserializeProtoBuf<T>(byte[] data) where T : class, ProtoBuf.IExtensible
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                T t = ProtoBuf.Serializer.Deserialize<T>(ms);
                return t;
            }
        }

        public static object DeserializeProtoBuf(byte[] data, Type type)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                return ProtoBuf.Meta.RuntimeTypeModel.Default.Deserialize(ms, null, type);
            }
        }


        public static GMPoint3D Get(Vector3 vec)
        {
            GMPoint3D point = new GMPoint3D();

            point.x = (int)(vec.x * 10000);
            point.y = (int)(vec.y * 10000);
            point.z = (int)(vec.z * 10000);

            return point;
        }

        public static Vector3 Get(GMPoint3D point)
        {
            Vector3 vec = new Vector3();
            vec.x = point.x / 10000f;
            vec.y = point.y / 10000f;
            vec.z = point.z / 10000f;
            return vec;
        }

        public static GMPlayerInfo Get(PlayerInfo info)
        {
            GMPlayerInfo o = new GMPlayerInfo();

            o.roleId = info.roleid;
            o.name = info.name;
            o.moveSpeed = info.moveSpeed;
            o.moveSpeedAddition = info.moveSpeedAddition;
            o.moveSpeedPercent = info.moveSpeedPercent;
            o.attackSpeed = info.attackSpeed;
            o.attackSpeedAddition = info.attackSpeedAddition;
            o.attackSpeedPercent = info.attackSpeedPercent;
            o.maxBlood = info.maxBlood;
            o.nowBlood = info.nowBlood;
            o.type = info.type;
            return o;
        }

        public static PlayerInfo Get(GMPlayerInfo info)
        {
            PlayerInfo o = new PlayerInfo();

            o.roleid = info.roleId;
            o.name = info.name;
            o.moveSpeed = info.moveSpeed;
            o.moveSpeedAddition = info.moveSpeedAddition;
            o.moveSpeedPercent = info.moveSpeedPercent;
            o.attackSpeed = info.attackSpeed;
            o.attackSpeedAddition = info.attackSpeedAddition;
            o.attackSpeedPercent = info.attackSpeedPercent;
            o.maxBlood = info.maxBlood;
            o.nowBlood = info.nowBlood;
            o.type =info.type;

            return o;
        }

        public static GMCommand Get(Command cmd)
        {
            GMCommand o = new GMCommand();
            o.id = cmd.id;
            o.frame = cmd.frame;
            o.type = cmd.type;
            o.frametime = cmd.time;
            o.data = cmd.data;
            return o;
        }
    }
}
