using LegacyOrderProcessor.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LegacyOrderProcessor.Services
{
    public interface IDatabase
    {
        bool IsConnected { get; }
        void Connect();
        void Save(Order order);
        Order GetOrder(int id);
    }
}
