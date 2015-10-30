﻿using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace StreamingBulkCopy
{
    public class StreamingBulkCopy<T> where T : new()
    {
        private readonly string importFile;

        public StreamingBulkCopy(string importFile)
        {
            this.importFile = importFile;
        }

        public IEnumerable<T> BuildDtosFromDataInImportFile()
        {
            var listOfT = new List<T>();
            
            //"When you use ReadLines, you can start enumerating the collection of strings before the whole collection is returned"
            //foreach starts enumerating, but based on the explination above from MSDN, sounds like even though I've started enumerating, the file is still only loaded into memory one line at a time?
            var lines = File.ReadLines(importFile);

            var propertyInfoOfT = typeof(T).GetProperties();
            foreach (var line in lines)
            {
                var data = line.Split(',');
                var newT = new T();

                var i = 0;
                foreach (var propertyInfo in propertyInfoOfT)
                {
                    propertyInfo.SetValue(newT, data[i], null);
                    i++;
                }
                listOfT.Add(newT);
            }

            return listOfT;
        }

        //find a way to execute this line:
        //var dataReader = new EnumerableDataReader<Data>(data);
        //in this class. That way, the dataReader is internal to the class, and does not have to be fed to the WriteToDatabase method below

        public IEnumerable<Data> GetData()
        {
            //stream the file out first using LINQ
            //File.ReadLines method reads one line at a time, and returns an IEnumerable of string
            //it only every loads one line into memory at a time
            //https://msdn.microsoft.com/en-us/library/dd383503(v=vs.110).aspx: "The ReadLines and ReadAllLines methods differ as follows: When you use ReadLines, you can start enumerating the collection of strings before the whole collection is returned; when you use ReadAllLines, you must wait for the whole array of strings be returned before you can access the array. Therefore, when you are working with very large files, ReadLines can be more efficient."
            return File.ReadLines(importFile)
                .Select(line => line.Split(','))
                .Select(split => new Data { Field1 = split[0], Field2 = split[1], Field3 = split[2] });
        }

        public void WriteToDatabase(IDataReader dataReader)
        {
            using (var bulkCopy = new SqlBulkCopy(@"Data Source=(LocalDB)\MSSQLLocalDB; Initial Catalog=StreamingBulkCopy; Integrated Security=True;"))
            {
                bulkCopy.DestinationTableName = "dbo.Data";
                bulkCopy.EnableStreaming = true;
                bulkCopy.WriteToServer(dataReader);
            }
        }
    }
}

