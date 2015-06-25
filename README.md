[![Stories in Ready](https://badge.waffle.io/easynetq/easynetq.svg?label=Ready&title=Ready)](http://waffle.io/easynetq/easynetq)
[![Stories in Progress](https://badge.waffle.io/easynetq/easynetq.svg?label=In%20Progress&title=In%20Progress)](http://waffle.io/easynetq/easynetq)

--

![EasyNetQ Logo](https://github.com/EasyNetQ/EasyNetQ/wiki/images/logo_design_150.png)

A Nice .NET API for RabbitMQ

Initial development was sponsored by travel industry experts [15below](http://15below.com/)

* **[Homepage](http://easynetq.com)**
* **[Documentation](https://github.com/mikehadlow/EasyNetQ/wiki/Introduction)**
* **[NuGet](http://nuget.org/List/Packages/EasyNetQ)**
* **[Discussion Group](https://groups.google.com/group/easynetq)**

Goals:

1. To make working with RabbitMQ on .NET as easy as possible.

To connect to a RabbitMQ broker...

    var bus = RabbitHutch.CreateBus("host=localhost");

To publish a message...

    bus.Publish(message);

To subscribe to a message...

	bus.Subscribe<MyMessage>("my_subscription_id", msg => Console.WriteLine(msg.Text));

Remote procedure call...

    var request = new TestRequestMessage {Text = "Hello from the client! "};
    bus.Request<TestRequestMessage, TestResponseMessage>(request, response => 
        Console.WriteLine("Got response: '{0}'", response.Text));

RPC server...

    bus.Respond<TestRequestMessage, TestResponseMessage>(request => 
		new TestResponseMessage{ Text = request.Text + " all done!" });
	

## Management API

EasyNetQ also has a client-side library for the RabbitMQ Management HTTP API. This lets you control all aspects for your
RabbitMQ broker from .NET code, including creating virtual hosts and users; setting permissions; monitoring queues, 
connections and channels; and setting up exchanges, queues and bindings. 

See the **[documentation](https://github.com/mikehadlow/EasyNetQ/wiki/Management-API-Introduction)**.

The annoucement blog post is [here](http://mikehadlow.blogspot.co.uk/2012/11/a-c-net-client-proxy-for-rabbitmq.html)

## Some blog posts about EasyNetQ ...

http://mikehadlow.blogspot.co.uk/search/label/EasyNetQ

## Getting started

Just open EasyNetQ.sln in VisualStudio 2010 and build.

All the required dependencies for the solution file to build the software are included. To run the explicit tests that send messages you will have to be running the EasyNetQ.Tests.SimpleService application and have a working local RabbitMQ server (see http://www.rabbitmq.com/ for more details).

## Mono specific

If you are building the software in monodevelop under Linux you will have to change the active solution configuration to 'Debug|Mixed platforms' to build all the included projects and set the 'Copy to output directory' property on  the app.config files to something other then 'Do not copy'. Most of the example programs will not run since they utilise the TopShelf assembly to run as a windows service. The basic tests and Tests.SimpleServer seem to behave correctly.
