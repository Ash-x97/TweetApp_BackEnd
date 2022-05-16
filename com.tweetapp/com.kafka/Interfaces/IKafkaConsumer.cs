using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.kafka.Interfaces
{
    public interface IKafkaConsumer<TKey, TValue> where TValue : class
    {
        Task Consume(string topic, CancellationToken stoppingToken);
        void Close();
        void Dispose();
    }
}
