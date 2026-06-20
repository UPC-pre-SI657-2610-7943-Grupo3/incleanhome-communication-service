using InCleanHome.CommunicationService.Shared;
using Microsoft.EntityFrameworkCore;

namespace InCleanHome.CommunicationService.Infrastructure.Persistence;

public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
{
    protected readonly CommunicationDbContext Context;
    protected BaseRepository(CommunicationDbContext context) { Context = context; }

    public async Task AddAsync(TEntity entity)         => await Context.Set<TEntity>().AddAsync(entity);
    public async Task<TEntity?> FindByIdAsync(int id)  => await Context.Set<TEntity>().FindAsync(id);
    public void Update(TEntity entity)                 => Context.Set<TEntity>().Update(entity);
    public void Remove(TEntity entity)                 => Context.Set<TEntity>().Remove(entity);
    public async Task<IEnumerable<TEntity>> ListAsync()=> await Context.Set<TEntity>().ToListAsync();
}

public class UnitOfWork(CommunicationDbContext context) : IUnitOfWork
{
    public async Task CompleteAsync() => await context.SaveChangesAsync();
}
