using LegacyOrderProcessor.Models;
using LegacyOrderProcessor.Services;

namespace LegacyOrderProcessor.Legacy;

public class OrderProcessor_Legacy
{
    private readonly IDatabase _database;
    private readonly IEmailService _emailService;

    public OrderProcessor_Legacy(IDatabase database, IEmailService emailService)
    {
        _database = database;
        _emailService = emailService;
    }

    public bool ProcessOrder(Order order)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));
        if (order.TotalAmount <= 0) return false;

        if (!_database.IsConnected)
        {
            _database.Connect();
        }

        try
        {
            _database.Save(order);
            // Отправляем email только если сумма заказа > 100
            if (order.TotalAmount > 100)
            {
                _emailService.SendOrderConfirmation(order.CustomerEmail, order.Id);
            }
            order.IsProcessed = true;
            return true;
        }
        catch (Exception)
        {
            // В текущей реализации просто отлавливаем исключение и возвращаем false
            return false;
        }
    }
}