using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Interfaces.Models.Identifiers;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util;

namespace TraceabilityEngine.Databases.Mongo.Serializers
{
    public class PGLNSerializer : IBsonSerializer<IPGLN>
    {
        #region IBsonSerializer
        public Type ValueType => typeof(IPGLN);

        public object Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                string pglnStr = context.Reader.ReadString();
                IPGLN pgln = IdentifierFactory.ParsePGLN(pglnStr);
                return pgln;
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
                BsonSerializer.Serialize(context.Writer, value.ToString());
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        IPGLN IBsonSerializer<IPGLN>.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                string pglnStr = context.Reader.ReadString();
                IPGLN pgln = IdentifierFactory.ParsePGLN(pglnStr);
                return pgln;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IPGLN value)
        {
            try
            {
                BsonSerializer.Serialize(context.Writer, value.ToString());
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
