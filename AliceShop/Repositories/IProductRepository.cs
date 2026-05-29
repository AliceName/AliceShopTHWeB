using AliceShop.Models;

namespace AliceShop.Repositories
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);

        Task<IEnumerable<Product>> SearchByNameAsync(string keyword);
    }
}
