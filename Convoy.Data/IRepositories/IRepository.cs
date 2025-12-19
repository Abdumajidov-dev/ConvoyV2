using System.Linq.Expressions;

namespace Convoy.Data.IRepositories;

public interface IRepository<TEnitity>
{
    public Task<bool> SaveAsync();
    public Task<bool> DeleteAsync(long Id);
    public Task<TEnitity> Update(TEnitity enitity, long longid);
    public Task<TEnitity> InsertAsync(TEnitity enitity);
    public Task<TEnitity> SelectAsync(Expression<Func<TEnitity, bool>> expression, string[] includes = null);
    public IQueryable<TEnitity> SelectAll(Expression<Func<TEnitity, bool>> expression = null, string[] includes = null);
    public Task<IList<TEnitity>> SelectAllAsync(Expression<Func<TEnitity, bool>> expression = null, string[] includes = null);
}
