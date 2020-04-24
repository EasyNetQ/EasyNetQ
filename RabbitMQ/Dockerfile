FROM rabbitmq:3.8-management
RUN apt-get update && apt-get install -y wget
RUN wget -O $RABBITMQ_HOME/plugins/rabbitmq_delayed_message_exchange-3.8.0.ez https://github.com/rabbitmq/rabbitmq-delayed-message-exchange/releases/download/v3.8.0/rabbitmq_delayed_message_exchange-3.8.0.ez
RUN rabbitmq-plugins enable --offline rabbitmq_delayed_message_exchange
