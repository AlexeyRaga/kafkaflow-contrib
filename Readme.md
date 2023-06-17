# KafkaFlow - extra libraries

This project contains a set of libraries that contribute to [KafkaFlow](https://github.com/Farfetch/kafkaflow)
ecosystem.

- [KafkaFlow.Contrib.Outbox](./src/KafkaFlow.Contrib.Outbox/Readme.md)

  is a library to provide [transactional outbox](https://microservices.io/patterns/data/transactional-outbox.html)
  for KafkaFlow.

  The following backends are implemented:

  - [KafkaFlow.Contrib.Outbox.Postgres](./src/KafkaFlow.Contrib.Outbox.Postgres) - Postgres SQL backend


- [KafkaFlow.Contrib.ProcessManagers](./src/KafkaFlow.Contrib.ProcessManagers/Readme.md)

  is a library that provides [Process Managers](https://www.enterpriseintegrationpatterns.com/patterns/messaging/ProcessManager.html)
  functionality (sometimes also called [Sagas](./src/KafkaFlow.Contrib.ProcessManagers/docs/pm-or-saga.md)).

  The following backends are implemented:

  - [KafkaFlow.Contrib.ProcessManagers.Postgres](./src/KafkaFlow.Contrib.ProcessManagers.Postgres) -
    Postgres SQL backend for storing process' state
