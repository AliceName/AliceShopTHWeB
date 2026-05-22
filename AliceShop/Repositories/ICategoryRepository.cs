using AliceShop.Models;

namespace AliceShop.Repositories
{
    public interface ICategoryRepository
    {
        IEnumerable<Category> GetAllCategories();
        Category GetById(int id);
        void Add(Category Category);
        void Update(Category Category);
        void Delete(int id);
    }
}
