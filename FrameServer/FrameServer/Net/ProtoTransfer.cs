using System;
using System.IO;
using PBMessage;
using ProtoBuf.Meta;
using FrameServer;

namespace  Network
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

        public static Point3D Get(GMPoint3D gmpoint)
        {
            Point3D point = new Point3D(gmpoint.x, gmpoint.y, gmpoint.z);
            return point;
        }

        public static GMPoint3D Get(Point3D point)
        {
            GMPoint3D gmpoint = new GMPoint3D();
            gmpoint.x = point.x;
            gmpoint.y = point.y;
            gmpoint.z = point.z;
            return gmpoint;
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
            o.type = info.type;
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
