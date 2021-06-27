using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Util;
using TraceabilityEngine.Util.Interfaces;
using TraceabilityEngine.Util.Security;

namespace TraceabilityEngine.Databases.Mongo.Serializers
{
    public class DIDSerializer : IBsonSerializer<IDID>
    {
        public Type ValueType => typeof(IDID);

        public IDID Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                IDID did = null;
                context.Reader.ReadStartDocument();
                if (context.Reader.FindElement("Value"))
                {
                    string didStr = context.Reader.ReadString();
                    did = DIDFactory.Parse(didStr);
                }
                context.Reader.ReadEndDocument();
                return did;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        object IBsonSerializer.Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            try
            {
                IDID did = null;
                context.Reader.ReadStartDocument();
                if (context.Reader.FindElement("Value"))
                {
                    string didStr = context.Reader.ReadString();
                    did = DIDFactory.Parse(didStr);
                }
                context.Reader.ReadEndDocument();
                return did;
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        public void Serialize(BsonSerializationContext context, BsonSerializationArgs args, IDID value)
        {
            try
            {
                context.Writer.WriteStartDocument();
                context.Writer.WriteName("_t");
                context.Writer.WriteString(value.GetType().Name);
                context.Writer.WriteName("ID");
                context.Writer.WriteString(value.ID);
                context.Writer.WriteName("Value");
                context.Writer.WriteString(value.ToString());
                context.Writer.WriteEndDocument();
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
                if (value is IDID)
                {
                    IDID did = value as DID;
                    context.Writer.WriteStartDocument();
                    context.Writer.WriteName("_t");
                    context.Writer.WriteString(value.GetType().Name);
                    context.Writer.WriteName("ID");
                    context.Writer.WriteString(did.ID);
                    context.Writer.WriteName("Value");
                    context.Writer.WriteString(did.ToString());
                    context.Writer.WriteEndDocument();
                }
            }
            catch (Exception Ex)
            {
                TELogger.Log(0, Ex);
                throw;
            }
        }

        
    }
}
