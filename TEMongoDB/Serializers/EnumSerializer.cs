using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Databases.Mongo.Serializers
{
    public class EnumSerializer<T> : IBsonSerializer<T> where T : System.Enum
    {
        #region IBsonSerializer
        public Type ValueType => typeof(T);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                int enumIntValue = context.Reader.ReadInt32();
                object enumValue = Enum.Parse(typeof(T), enumIntValue.ToString());
                return enumValue;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, object value)
        {
            try
            {
                context.Writer.WriteInt32((int)value);
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        T IBsonSerializer<T>.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                int enumIntValue = context.Reader.ReadInt32();
                T enumValue = (T)Enum.Parse(typeof(T), enumIntValue.ToString());
                return enumValue;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, T value)
        {
            try
            {
                context.Writer.WriteInt32(Convert.ToInt32(value));
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }
        #endregion
    }
}
