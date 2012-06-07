A Nice .NET API for RabbitMQ

Development is sponsored by travel industry experts [15below](http://15below.com/)

**[Documentation](https://github.com/mikehadlow/EasyNetQ/wiki/Introduction)**

**[NuGet](http://nuget.org/List/Packages/EasyNetQ)**

**[Discussion Group](https://groups.google.com/group/easynetq)**

Goals:

1. Zero or at least minimal configuration.
2. Simple API

To connect to a RabbitMQ broker...

	var bus = RabbitHutch.CreateBus("host=localhost");

To publish a message...

    using (var publishChannel = bus.OpenPublishChannel())
    {
        publishChannel.Publish(message);
    }

To subscribe to a message...

	bus.Subscribe<MyMessage>("my_subscription_id", msg => Console.WriteLine(msg.Text));

Remote procedure call...

    var request = new TestRequestMessage {Text = "Hello from the client! "};
    using (var publishChannel = bus.OpenPublishChannel())
    {
		publishChannel.Request<TestRequestMessage, TestResponseMessage>(request, response => 
			Console.WriteLine("Got response: '{0}'", response.Text));
	}

RPC server...

	bus.Respond<TestRequestMessage, TestResponseMessage>(request => 
		new TestResponseMessage{ Text = request.Text + " all done!" });
	

## Some blog posts about EasyNetQ ...

http://mikehadlow.blogspot.com/2011/05/easynetq-simple-net-api-for-rabbitmq.html

http://mikehadlow.blogspot.com/2011/05/futurepublish-with-easynetq-rabbitmq.html

http://mikehadlow.blogspot.com/2011/06/rabbitmq-subscription-and-bouncing.html

http://mikehadlow.blogspot.com/2011/07/rabbitmq-subscriptions-with-dotnet.html

http://mikehadlow.blogspot.com/2011/07/easynetq-how-should-messaging-client.html

http://mikehadlow.blogspot.co.uk/2012/05/easynetq-breaking-change.html

## Getting started

Just open EasyNetQ.sln in VisualStudio 2010 and build.

All the required dependencies for the solution file to build the software are included. To run the explicit tests that send messages you will have to be running the EasyNetQ.Tests.SimpleService application and have a working local RabbitMQ server (see http://www.rabbitmq.com/ for more details).

## Mono specific

If you are building the software in monodevelop under Linux you will have to change the active solution configuration to 'Debug|Mixed platforms' to build all the included projects and set the 'Copy to output directory' property on  the app.config files to something other then 'Do not copy'. Most of the example programs will not run since they utilise the TopShelf assembly to run as a windows service. The basic tests and Tests.SimpleServer seem to behave correctly.
