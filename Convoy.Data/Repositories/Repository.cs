using Microsoft.EntityFrameworkCore;
using Convoy.Data.IRepositories;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Security.AccessControl;
using Convoy.Data.DbContexts;
using Convoy.Domain.Commons;

namespace Convoy.Data.Repositories;


public class Repository<TEnitity> : IRepository<TEnitity> where TEnitity : Auditable
{
    AppDbConText dbConText;
    DbSet<TEnitity> dbSet;
    public Repository(AppDbConText dbConText)
    {
        this.dbConText = dbConText;
        this.dbSet = dbConText.Set<TEnitity>();
    }
    public async Task<bool> DeleteAsync(long Id)
    {
        var entity = await this.dbSet.FirstOrDefaultAsync(e => e.Id == Id);
        dbSet.Remove(entity);
        return true;

    }
    public async Task<TEnitity> InsertAsync(TEnitity enitity)
        => (await this.dbSet.AddAsync(enitity)).Entity;

    public async Task<bool> SaveAsync()
        => (await this.dbConText.SaveChangesAsync() > 0);

    public IQueryable<TEnitity> SelectAll(Expression<Func<TEnitity, bool>> expression = null, string[] includes = null)
    {
        var query = expression is null ? dbSet : dbSet.Where(expression);
        if (includes is not null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        return query;
    }
    public async Task<IList<TEnitity>> SelectAllAsync(
    Expression<Func<TEnitity, bool>> expression = null,
    string[] includes = null)
    {
        var query = expression is null ? dbSet.AsQueryable() : dbSet.Where(expression);

        if (includes is not null)
        {
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
        }

        return await query.ToListAsync();
    }

    public async Task<TEnitity> SelectAsync(Expression<Func<TEnitity, bool>> expression, string[] includes = null)
        => await this.SelectAll(expression, includes).FirstOrDefaultAsync();

    public async Task<TEnitity> Update(TEnitity enitity, long longid)
        => this.dbSet.Update(enitity).Entity;



}
