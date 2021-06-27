using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TraceabilityEngine.Databases.Mongo;
using TraceabilityEngine.Interfaces.DB;
using TraceabilityEngine.Interfaces.DB.DocumentDB;
using TraceabilityEngine.Interfaces.Models.Account;
using TraceabilityEngine.Interfaces.Models.Products;
using TraceabilityEngine.Models;
using TraceabilityEngine.Models.Identifiers;
using TraceabilityEngine.Util.StaticData;

namespace UnitTests.DB.DocumentDB
{
    [TestClass]
    public class DocumentDBTests
    {
        [TestMethod]
        public void Connect()
        {
            using (ITEDocumentDB db = UnitTests.GetDocumentDB("UT_DocumentDB"))
            {

            }
        }

        [TestMethod]
        public async Task CRUD()
        {
            string collectionName = "basic_entities";
            for (int threads = 0; threads < 5; threads++)
            {
                List<Entity> entities = new List<Entity>();
                for (int i = 0; i < 50; i++)
                {
                    entities.Add(new Entity() { Name = "Entity #" + i });
                }
                using (ITEDocumentDB db = UnitTests.GetDocumentDB("UT_DocumentDB"))
                {
                    foreach (Entity ent in entities)
                    {
                        await db.SaveAsync<Entity>(ent, collectionName);
                        Entity loaded = await db.LoadAsync<Entity>(ent.ObjectID, collectionName);
                        Assert.IsNotNull(loaded);
                        Assert.AreEqual(ent, loaded);

                        ent.Name = "Modified " + ent.Name;

                        await db.SaveAsync<Entity>(ent, collectionName);
                        Entity loadedAgain = await db.LoadAsync<Entity>(ent.ObjectID, collectionName);
                        Assert.IsNotNull(loadedAgain);
                        Assert.AreEqual(ent, loadedAgain);
                    }
                }
            }

            using (ITEDocumentDB db = UnitTests.GetDocumentDB("UT_DocumentDB"))
            {
                await db.DropAsync(collectionName);
            }
        }

        [TestMethod]
        public async Task Sequence()
        {
            using (ITEDocumentDB db = UnitTests.GetDocumentDB("UT_DocumentDB"))
            {
                await db.DropAsync("sequences");
                for (long i = 1; i < 1000; i++)
                {
                    long seq = await db.GetNextSequenceAsync("test_sequence");
                    Assert.AreEqual(i, seq);
                }
                await db.ResetSequence("test_sequence", 1);
                for (long i = 1; i < 1000; i++)
                {
                    long seq = await db.GetNextSequenceAsync("test_sequence");
                    Assert.AreEqual(i, seq);
                }
            }
        }
    }

    class Entity : ITEDocumentObject, IEquatable<Entity>
    {
        [BsonRepresentation(BsonType.ObjectId)]
        [BsonElement("_id")]
        public string ObjectID { get; set; }

        [BsonElement]
        public string Name { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (!(obj is Entity))
            {
                return false;
            }

            return (this.Name == ((Entity)obj).Name);
        }

        public bool Equals(Entity other)
        {
            if(other == null)
            {
                return false;
            }

            return (this.Name == ((Entity)other).Name);
        }

        public override int GetHashCode()
        {
            return this.Name?.GetHashCode() ?? 0;
        }
    }

    //class MongoTestAccessToken : ITEAccessToken
    //{
    //    public object Lock => throw new NotImplementedException();
    //    public ITEDatabaseFactory DBFactory => new MongoTestDatabaseFactory();
    //    public ITEAccount Account { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //    public ITEAccountUser User { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    //}

    //class MongoTestDatabaseFactory : ITEDatabaseFactory
    //{
    //    public ITEAccountDB GetAccountDB()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ITEAttachmentDB GetAttachmentDB()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ITEDocumentDB GetDocumentDB()
    //    {
    //        ITEDocumentDB db = new TEMongoDatabase(DocumentDBTests.connectionString, DocumentDBTests.dbName);
    //        return db;
    //    }

    //    public ITEEventDB GetEvent()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ITELocationDB GetLocationDB()
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public ITEProductDB GetProductDB()
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
