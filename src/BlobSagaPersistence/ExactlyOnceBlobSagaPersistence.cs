using System;
using NServiceBus.Persistence;

namespace NServiceBus
{
    public class ExactlyOnceBlobSagaPersistence : PersistenceDefinition
    {
        public ExactlyOnceBlobSagaPersistence()
        {
            Supports<StorageType.Sagas>(settings =>
            {

            });
            Supports<StorageType.Outbox>(settings =>
            {

            });
        }
    }
}