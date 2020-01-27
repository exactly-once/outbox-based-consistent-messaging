The repository contains multiple implementations of Message-Driven State Machine (MDSM) pattern with built-in detection and handling of duplicate messages. Within this repository we use the term Saga to refer to this concept, following the [NServiceBus nomenclature](https://docs.particular.net/nservicebus/sagas/).

## Project structure


## Persistence

All samples use an in-memory document database for simplicity reasons. Such a database can be substituted by a cloud-based blob store such as Azure Blob Store or AWS S3.

## Variants

### Base

The [base variant](https://github.com/exactly-once/BlobSagaPersistence/tree/master/src/Baseline) implements the simplest duplicate detection and handling logic based directly on [NServiceBus Outbox](https://docs.particular.net/nservicebus/outbox/).

 - The document representing machine's state contains a list of messages it already processed
 - When persisting new state after a transition, the ID of the message that caused the transition is added to the collection of processed messages
 - Messages generated in the state transition are added to the collection of outgoing messages
 - Outgoing messages are dispatched to the messaging system asynchronously, driven by failure handling built-into messaging framework

The main advantages of the base variant are simplicity and cost (measured in roundtrips to the storage required to process a single incoming message). The base variant requires *3 rountrips*: `Load`, `Store` and `MarkDispatched`.

The biggest disadvantage of the basic variant is the need to keep all information about processed messages inside the state document. That means that loading the state takes longer with more processed messages.

### Basic inbox

The [basic inbox](https://github.com/exactly-once/BlobSagaPersistence/tree/master/src/BasicInbox) variant is an attempt to eliminate the biggest disadvanage of the base variant by using a concept of *inbox* -- a shared (between MDSMs) store of all processed messages. The *inbox* eliminetas the need to store IDs of processed messages in the state document.

 - After dispatching the outgoing messages the inbox document is created for a given incoming message.
 - After the inbox document is created, the ID of that incoming message can be safely removed from the *outbox* kept inside the state document
 
The disadvantage of the basic inbox variant, compared to the base variant, is it now requires one more rountrip (a total of 4) to process each message.

Another disadvantage of the basic inbox is the fact that it requires quite stronge consistency model in which a `GET` issued after `PUT` returns is guaranteed to return the fresh result. This consistency model is [not compatible with AWS S3](https://docs.aws.amazon.com/AmazonS3/latest/dev/Introduction.html#ConsistencyModel) which guarantees this only for keys that were never subject to `GET` or `HEAD`. In the basic inbox the sequence of operations against inbox store is `GET`, `PUT`, `GET` for a message that has two copies and this violates the S3 assumptions.

### Low-consistency inbox

For this very reason we designed a [low-consistency store inbox variant](https://github.com/exactly-once/BlobSagaPersistence/tree/master/src/LowConsistencyInbox). In this version the inbox records are _claimed_ (stamped) as part of message processing. Each inbox record can only be stamped once and checking the stamp is a reliable operation even on S3 as it relies on optimistic concurrency control.

### Out-of-document outbox

The [out-of-document outbox](https://github.com/exactly-once/BlobSagaPersistence/tree/master/src/InboxWithOutOfDocumentOutbox) variant has been create to cater for state stores that have size limitations, such as Azure Table Store or Amazon Dynamo. In this version the bodies of the outgoing messages are keps in a separate store and the outbox structures in the state document contain only references. 

### Token-based inbox

The [token-based inbox](https://github.com/exactly-once/BlobSagaPersistence/tree/master/src/TokenBasedInbox) variant attempts to solve the problem of non-deterministic evition of de-duplication information that was the downside of all previous approaches. All de-duplication schemes that are based on adding information as part of message processing have the associated problem of how long that information should be kept. In theory it should be kept forever as there is no intrinsic limit on how much a duplicated can be delayeded.

The token-based inbox is based on an inverse principle. The de-duplication information is created in form of a _token_ before a message is sent. As part of processing the message the token is first _claimed_ (stamped) and then removed. The token-based inbox offers 100% de-duplication guarantees at the cost of additional round trips required to created the tokens.
