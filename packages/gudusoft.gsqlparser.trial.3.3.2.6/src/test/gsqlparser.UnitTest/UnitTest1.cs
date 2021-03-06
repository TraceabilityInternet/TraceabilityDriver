using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace gudusoft.gsqlparser.test
{
    [TestClass]
    public class UnitTestDBVendor
    {
        [TestMethod]
        public void TestMethodDBVendor()
        {
            TGSqlParser sqlparser = null;
            int ret, success = 0, total = 0;
            string dbstr = "";
            foreach (EDbVendor dbVendor in (EDbVendor[])Enum.GetValues(typeof(EDbVendor)))
            {

                try
                {
                    if ((dbVendor != EDbVendor.dbvaccess) && (dbVendor != EDbVendor.dbvansi)
                        && (dbVendor != EDbVendor.dbvfirebird) && (dbVendor != EDbVendor.dbvgeneric) && (dbVendor != EDbVendor.dbvodbc) && (dbVendor != EDbVendor.dbvmdx))
                    {
                        total++;
                        sqlparser = new TGSqlParser(dbVendor);
                        sqlparser.sqltext = "select 2 from t";
                        ret = sqlparser.parse();
                        dbstr = dbstr + dbVendor.ToString() + ",";
                        success++;
                        Console.WriteLine("{0}", dbVendor.ToString());
                    }
                }
                catch
                {
                    Console.WriteLine("{0}(not supported)", dbVendor.ToString());
                }
            }

            Console.WriteLine("Version:{0}, Release date:{1}, Full version = {2}, \nDb: {3}/{4}, {5}", TBaseType.versionId, TBaseType.releaseDate, TBaseType.full_edition, success, total, dbstr);

        }
    }
}
