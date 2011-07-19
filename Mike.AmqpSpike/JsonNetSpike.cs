using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace Mike.AmqpSpike
{
    public class JsonNetSpike
    {
        public void SerializeSomething()
        {
            var message = GetMyJsonTestMessage();

            var output = JsonConvert.SerializeObject(message);

            Console.Out.WriteLine("output = {0}", output);

            var deserializedMessage = JsonConvert.DeserializeObject<MyJsonTestMessage>(output);

            Console.Out.WriteLine(deserializedMessage.GetHashCode() == message.GetHashCode());
        }

        private static MyJsonTestMessage GetMyJsonTestMessage()
        {
            return new MyJsonTestMessage
            {
                Name = "Mike",
                Id = 101,
                CreatedDate = new DateTime(2010, 7, 19)
                }.AddOrder("Widget", 2, 10.23M)
                 .AddOrder("Gadget", 1, 45.32M);
        }

        public void UseDedicatedSerializer()
        {
            var message = GetMyJsonTestMessage();

            var serializer = new JsonSerializer();

            using (var writer = new StringWriter())
            using (var jsonWriter = new JsonTextWriter(writer))
            {
                jsonWriter.Formatting = Formatting.Indented;
                serializer.Serialize(jsonWriter, message);

                Console.Out.WriteLine("writer.GetStringBuilder() = {0}", writer.GetStringBuilder());
            }
        }

        public void UseBsonSerializer()
        {
            var message = GetMyJsonTestMessage();

            var serializer = new JsonSerializer();
            byte[] serializedMessage;

            using (var stream = new MemoryStream())
            using (var writer = new BsonWriter(stream))
            {
                serializer.Serialize(writer, message);

                serializedMessage = stream.GetBuffer();
            }

            MyJsonTestMessage deserializedMessage;
            using (var stream = new MemoryStream(serializedMessage))
            using (var reader = new BsonReader(stream))
            {
                

                deserializedMessage = serializer.Deserialize<MyJsonTestMessage>(reader);
            }

            Console.Out.WriteLine(deserializedMessage.GetHashCode() == message.GetHashCode());
        }
    }

    public class MyJsonTestMessage
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public DateTime CreatedDate { get; set; }
        private IList<Order> orders = new List<Order>();
        public IList<Order> Orders
        {
            get { return orders; }
        }

        public MyJsonTestMessage AddOrder(string productName, int quantity, decimal price)
        {
            Orders.Add(new Order {ProductName = productName, Price = price, Quantity = quantity});
            return this;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() +
                   Id.GetHashCode() +
                   CreatedDate.GetHashCode() +
                   orders.Sum(x => x.GetHashCode());
        }
    }

    public class Order
    {
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }

        public override int GetHashCode()
        {
            return ProductName.GetHashCode() + Quantity.GetHashCode() + Price.GetHashCode();
        }
    }
}