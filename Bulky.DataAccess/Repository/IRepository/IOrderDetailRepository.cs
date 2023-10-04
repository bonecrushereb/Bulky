using Bulky.Models;

namespace Bulky.DataAccess.Repository.IRepository
{
    public interface IOrderDetailRepository : IRepository<Category>
    {
        void Update(OrderDetail obj);
    }
}