using System;
using System.Threading;
using RabbitMQ.Client;

namespace EasyNetQ.Tests.Integration
{
    public class ChannelExceptionBug
    {
        public void Reproduce()
        {
            // Run conditions: test-exchange must not be declared
            using (var bus = EasyNetQ.RabbitHutch.CreateBus("host=localhost").Advanced)
            {
                for (int i = 0; i < 10; i++ )
                {
                    var c = i;

                    Console.Out.WriteLine("\n-- {0} --\n", c);

                    try
                    {
                        var exc = bus.ExchangeDeclare("test-exchange" + c.ToString(), "fanout", true);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error getting exchange: {0}. {1}", ex.Message, ex.InnerException.Message);
                        // this MUST be written
                    }

                    Console.Out.WriteLine("\n----\n");

                    try
                    {
                        var exc = bus.ExchangeDeclare("test-exchange" + c.ToString(), "direct");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error declaring exchange: {0}. {1}", ex.Message, ex.InnerException.Message);
                        // this must NOT be written
                    }
                }
                Thread.Sleep(10000);
                Console.Out.WriteLine("-- end of test --");
            }            
        }

        public void Spike()
        {
            var connectionFactory = new ConnectionFactory
                {
                    HostName = "localhost"
                };

            var connection = connectionFactory.CreateConnection();

            var model = connection.CreateModel();
            model.CallbackException += model_CallbackException;

            Console.Out.WriteLine("declare passive 1");
            try
            {
                model.ExchangeDeclarePassive("test_exchange_new");
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }

            Console.Out.WriteLine("\nmodel.IsOpen: {0}", model.IsOpen);
            Console.Out.WriteLine("Next\n");

            try
            {
                model.ExchangeDeclare("test_exchange_new", "direct");
            }
            catch (Exception e)
            {
                Console.Out.WriteLine("{0}", e.Message);
            }

            Console.Out.WriteLine("Completed");

            Thread.Sleep(1000);

            connection.Dispose();
        }

        void model_CallbackException(object sender, RabbitMQ.Client.Events.CallbackExceptionEventArgs e)
        {
            foreach (var value in e.Detail)
            {
                Console.Out.WriteLine("[Detail] {0} {1}", value.Key, value.Value);
            }
        }
    }
}