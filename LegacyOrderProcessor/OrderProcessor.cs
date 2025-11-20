using LegacyOrderProcessor.Models;
using LegacyOrderProcessor.Services;

namespace LegacyOrderProcessor;

public class OrderProcessor
{
    private readonly IDatabase _database;
    private readonly IEmailService _emailService;

    public OrderProcessor(IDatabase database, IEmailService emailService)
    {
        _database = database ?? throw new ArgumentNullException(nameof(database));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    public bool ProcessOrder(Order order)
    {
        if (order == null) throw new ArgumentNullException(nameof(order));
        if (order.TotalAmount <= 0) return false;

        EnsureDatabaseConnected();

        try
        {
            _database.Save(order);

            if (order.TotalAmount > 100)
            {
                TrySendConfirmationEmail(order);
            }

            order.IsProcessed = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void EnsureDatabaseConnected()
    {
        if (!_database.IsConnected)
        {
            _database.Connect();
        }
    }

    private void TrySendConfirmationEmail(Order order)
    {
        try
        {
            _emailService.SendOrderConfirmation(order.CustomerEmail, order.Id);
        }
        catch
        {
            // Ошибка отправки письма НЕ ломает весь процесс
        }
    }
}