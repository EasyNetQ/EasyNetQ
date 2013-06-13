#EasyNetQ.Trace

EasyNetQ trace is a simple console application for watching amq.rabbitmq.trace.

See:

[http://www.rabbitmq.com/firehose.html](http://www.rabbitmq.com/firehose.html)

Simply start it with the [AMQP connection string](http://www.rabbitmq.com/uri-spec.html) to your broker.

    EasyNetQ.Trace.exe -a amqp://guest:guest@myhost/myVhost

By default it will connect to the default vhost on your localhost as guest.

It is also possible to export the messages to a csv file for later analysis. Command line options detailed as below:

  -a, --amqp-connection-string    (Default: amqp://localhost/) AMQP Connection string.

  -q, --quiet                     (Default: False) Switch off verbose console output

  -o, --output-csv                CSV File name for output

  --help                          Display help screen.