#EasyNetQ.Trace

EasyNetQ trace is a simple console application for watching amq.rabbitmq.trace.

See:

[http://www.rabbitmq.com/firehose.html](http://www.rabbitmq.com/firehose.html)

Simply start it with the [AMQP connection string](http://www.rabbitmq.com/uri-spec.html) to your broker.

    EasyNetQ.Trace.exe amqp://guest:guest@myhost/myVhost

By default it will connect to the default vhost on your localhost as guest.