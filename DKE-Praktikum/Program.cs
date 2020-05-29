using System;
using System.Threading.Tasks;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace TEST
{
    class Program
    {
        public class Post
        {
            [BsonId]
            public ObjectId Id { get; set; }
            [BsonElement("Post-Text")]
            public string Posting { get; set; }
            [BsonElement("Datum")]
            public DateTime PostDatum { get; set; }

        }

        // replace with your connection string if it is different
        const string MongoDBConnectionString = "mongodb+srv://Yasar:admin@cluster0-pjchg.azure.mongodb.net/test";

        public static async Task Main(string[] args)
        {
            if (!await UpdateProductsAsync()) { Environment.Exit(1); }
            Console.WriteLine("Finished updating the Post collection");
            Console.ReadKey();
        }

        private static async Task<bool> UpdateProductsAsync()
        {
            // Create client connection to our MongoDB database
            var client = new MongoClient(MongoDBConnectionString);

            // Create the collection object that represents the "products" collection
            var database = client.GetDatabase("PostService");
            var posts = database.GetCollection<Post>("PostService");

            // Clean up the collection if there is data in there
            await database.DropCollectionAsync("PostService");

            // collections can't be created inside a transaction so create it first
            await database.CreateCollectionAsync("PostService");

            // Create a session object that is used when leveraging transactions
            using (var session = await client.StartSessionAsync())
            {
                // Begin transaction
                session.StartTransaction();

                try
                {
                    // Create some sample data
                    var post1 = new Post
                    {
                        Posting = "Hello world",
                        PostDatum = DateTime.Now
                    };

                    // Insert the sample data
                    await posts.InsertOneAsync(session, post1);


                    var resultsBeforeUpdates = await posts
                                    .Find<Post>(session, Builders<Post>.Filter.Empty)
                                    .ToListAsync();
                    Console.WriteLine("Posts:\n");
                    foreach (Post d in resultsBeforeUpdates)
                    {
                        Console.WriteLine(
                                    String.Format("Post: {0}\tDatum: {1:0.00}",
                                        d.Posting, d.PostDatum)
                        );
                    }

                    // Increase all the prices by 10% for all products
                    //var update = new UpdateDefinitionBuilder<Product>()
                    //        .Mul<Double>(r => r.Price, 1.1);
                    //await products.UpdateManyAsync(session,
                    //        Builders<Product>.Filter.Empty,
                    //        update); //,options);

                    // Made it here without error? Let's commit the transaction
                    await session.CommitTransactionAsync();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error writing to MongoDB: " + e.Message);
                    await session.AbortTransactionAsync();
                    return false;
                }

                //// Let's print the new results to the console
                //Console.WriteLine("\n\nNew Prices (10% increase):\n");
                //var resultsAfterCommit = await products
                //        .Find<Product>(session, Builders<Product>.Filter.Empty)
                //        .ToListAsync();
                //foreach (Product d in resultsAfterCommit)
                //{
                //    Console.WriteLine(
                //        String.Format("Product Name: {0}\tPrice: {1:0.00}",
                //                                    d.Description, d.Price)
                //    );
                //}

                return true;
            }
        }
    }
}
