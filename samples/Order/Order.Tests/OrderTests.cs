using Fiffi;
using Fiffi.Testing;
using Fiffi.Visualization;
using Payment;
using Sales;
using Shipping;
using System.Linq;
using System.Threading.Tasks;
using Warehouse;
using Xunit;

namespace Order.Tests
{
    public class OrderTests
    {
        [Fact]
        public async Task FlowAsync()
        {
            var context = TestContextBuilder.Create<InMemoryEventStore, SalesModule>(
                SalesModule.Initialize, 
                PaymentModule.Initialize,
                WarehouseModule.Initialize,
                ShippingModule.Initialize
                );
            //var id = new AggregateId(Guid.NewGuid());
            await context.WhenAsync(new Sales.Order());

            context.Then((events, table) => {
                Assert.True(events.OfType<GoodsShipped>().Happened());
            });
        }
    }
}
