using Moq;
using FluentAssertions;
using Xunit;
using LegacyOrderProcessor.Models;
using LegacyOrderProcessor.Services;
using LegacyOrderProcessor;

public class OrderProcessorTests
{
    private readonly Mock<IDatabase> _dbMock;
    private readonly Mock<IEmailService> _emailMock;
    private readonly OrderProcessor _processor;

    public OrderProcessorTests()
    {
        _dbMock = new Mock<IDatabase>();
        _emailMock = new Mock<IEmailService>();
        _processor = new OrderProcessor(_dbMock.Object, _emailMock.Object);
    }

    [Fact]
    public void ProcessOrder_NullOrder_ThrowsArgumentNullException()
    {
        var exception = Assert.Throws<ArgumentNullException>(() => _processor.ProcessOrder(null));
        exception.ParamName.Should().Be("order");
    }

    [Fact]
    public void ProcessOrder_ZeroOrNegativeAmount_ReturnsFalse_AndDoesNotSave()
    {
        var order = new Order { TotalAmount = 0, CustomerEmail = "test@mail.ru" };

        var result = _processor.ProcessOrder(order);

        result.Should().BeFalse();
        order.IsProcessed.Should().BeFalse();
        _dbMock.Verify(db => db.Save(It.IsAny<Order>()), Times.Never);
        _emailMock.Verify(e => e.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ProcessOrder_DatabaseNotConnected_CallsConnectOnce()
    {
        _dbMock.Setup(db => db.IsConnected).Returns(false);
        var order = new Order { Id = 1, CustomerEmail = "a@b.c", TotalAmount = 150 };

        _processor.ProcessOrder(order);

        _dbMock.Verify(db => db.Connect(), Times.Once);
    }

    [Fact]
    public void ProcessOrder_DatabaseAlreadyConnected_DoesNotCallConnect()
    {
        _dbMock.Setup(db => db.IsConnected).Returns(true);
        var order = new Order { TotalAmount = 150 };

        _processor.ProcessOrder(order);

        _dbMock.Verify(db => db.Connect(), Times.Never);
    }

    [Fact]
    public void ProcessOrder_ValidOrderAbove100_SavesOrder_SendsEmail_SetsProcessed_True()
    {
        var order = new Order { Id = 42, CustomerEmail = "john@doe.com", TotalAmount = 299.99m };

        var result = _processor.ProcessOrder(order);

        result.Should().BeTrue();
        order.IsProcessed.Should().BeTrue();
        _dbMock.Verify(db => db.Save(order), Times.Once);
        _emailMock.Verify(e => e.SendOrderConfirmation("john@doe.com", 42), Times.Once);
    }

    [Fact]
    public void ProcessOrder_ValidOrderExactly100_SavesOrder_DoesNotSendEmail()
    {
        var order = new Order { Id = 7, CustomerEmail = "x@y.z", TotalAmount = 100m };

        _processor.ProcessOrder(order);

        _dbMock.Verify(db => db.Save(order), Times.Once);
        _emailMock.Verify(e => e.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ProcessOrder_ValidOrderBelow100_SavesOrder_DoesNotSendEmail()
    {
        var order = new Order { TotalAmount = 99.99m };

        _processor.ProcessOrder(order);

        _dbMock.Verify(db => db.Save(order), Times.Once);
        _emailMock.Verify(e => e.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ProcessOrder_DatabaseSaveThrowsException_ReturnsFalse_DoesNotSendEmail_OrderNotProcessed()
    {
        var order = new Order { Id = 5, CustomerEmail = "fail@db.com", TotalAmount = 200 };
        _dbMock.Setup(db => db.Save(order)).Throws<InvalidOperationException>();

        var result = _processor.ProcessOrder(order);

        result.Should().BeFalse();
        order.IsProcessed.Should().BeFalse();
        _emailMock.Verify(e => e.SendOrderConfirmation(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public void ProcessOrder_EmailServiceThrowsException_SaveStillHappens_ReturnsTrue_OrderProcessed()
    {
        var order = new Order { Id = 33, CustomerEmail = "bad@email.com", TotalAmount = 500 };
        _emailMock.Setup(e => e.SendOrderConfirmation("bad@email.com", 33))
                   .Throws<TimeoutException>();

        var result = _processor.ProcessOrder(order);

        result.Should().BeTrue(); // Важно: текущая легаси-логика НЕ откатывает сохранение при ошибке email!
        order.IsProcessed.Should().BeTrue();
        _dbMock.Verify(db => db.Save(order), Times.Once);
    }

    [Fact]
    public void ProcessOrder_WhenConnectThrows_ExceptionPropagates_SaveMayNotBeCalled()
    {
        var dbMock = new Mock<IDatabase>();
        dbMock.Setup(db => db.IsConnected).Returns(false);
        dbMock.Setup(db => db.Connect()).Throws<InvalidOperationException>();

        var processor = new OrderProcessor(dbMock.Object, Mock.Of<IEmailService>());
        var order = new Order { TotalAmount = 150 };

        Action act = () => processor.ProcessOrder(order);

        act.Should().Throw<InvalidOperationException>();
    }
}