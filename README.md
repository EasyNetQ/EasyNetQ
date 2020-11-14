[![Build status](https://ci.appveyor.com/api/projects/status/github/EasyNetQ/easynetq?svg=true)](https://ci.appveyor.com/project/EasyNetQ/easynetq)

[![NuGet Status](https://img.shields.io/nuget/v/EasyNetQ)](https://www.nuget.org/packages/EasyNetQ)
[![Nuget Status](https://img.shields.io/nuget/vpre/EasyNetQ)](https://www.nuget.org/packages/EasyNetQ)
[![Nuget Status](https://img.shields.io/nuget/dt/EasyNetQ)](https://www.nuget.org/packages/EasyNetQ)

![Activity](https://img.shields.io/github/commit-activity/w/EasyNetQ/easynetq)
![Activity](https://img.shields.io/github/commit-activity/m/EasyNetQ/easynetq)
![Activity](https://img.shields.io/github/commit-activity/y/EasyNetQ/easynetq)

![Size](https://img.shields.io/github/repo-size/graphql-dotnet/graphql-dotnet)
--

![EasyNetQ Logo](https://github.com/EasyNetQ/EasyNetQ/wiki/images/logo_design_150.png)

A Nice .NET API for RabbitMQ

Initial development was sponsored by travel industry experts [15below](http://15below.com/)

* **[Homepage](http://easynetq.com)**
* **[Documentation](https://github.com/EasyNetQ/EasyNetQ/wiki/Introduction)**
* **[NuGet](http://www.nuget.org/packages/EasyNetQ)**
* **[Discussion Group](https://groups.google.com/group/easynetq)**

Goals:

1. To make working with RabbitMQ on .NET as easy as possible.

To connect to a RabbitMQ broker...
```c#
    var bus = RabbitHutch.CreateBus("host=localhost");
```
To publish a message...
```c#
    await bus.PubSub.PublishAsync(message);
```
To publish a message with 5s delay...
```c#
    await bus.Scheduler.FuturePublishAsync(message, TimeSpan.FromSeconds(5));
```
To subscribe to a message...
```c#
    await bus.PubSub.SubscribeAsync<MyMessage>(
        "my_subscription_id", msg => Console.WriteLine(msg.Text)
    );
```
Remote procedure call...
```c#
    var request = new TestRequestMessage {Text = "Hello from the client! "};
    await bus.Rpc.RequestAsync<TestRequestMessage, TestResponseMessage>(request);
```
RPC server...
```c#
    await bus.Rpc.RespondAsync<TestRequestMessage, TestResponseMessage>(request =>
        new TestResponseMessage{ Text = request.Text + " all done!" }
    );
```

## Management API

EasyNetQ also has a client-side library for the RabbitMQ Management HTTP API. This lets you control all aspects for your
RabbitMQ broker from .NET code, including creating virtual hosts and users; setting permissions; monitoring queues,
connections and channels; and setting up exchanges, queues and bindings.

See the **[documentation](https://github.com/EasyNetQ/EasyNetQ/wiki/Management-API-Introduction)**.

The announcement blog post is [here](http://mikehadlow.blogspot.co.uk/2012/11/a-c-net-client-proxy-for-rabbitmq.html)

## Some blog posts about EasyNetQ ...

http://mikehadlow.blogspot.co.uk/search/label/EasyNetQ

## Getting started

Just open EasyNetQ.sln in VisualStudio and build.

All the required dependencies for the solution file to build the software are included. To run the explicit tests that send messages you will have to be running the EasyNetQ.Tests.SimpleService application and have a working local RabbitMQ server (see http://www.rabbitmq.com/ for more details).

