using AzureServiceBusExample.Bus.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureServiceBusExample.Storages
{
    public interface IStorage
    {
        Task Save<T>(T t) where T : IMessage;
        Task<T> Load<T>(string key) where T : IMessage;

        Task<T[]> GetAll<T>() where T : IMessage;
    }
}
