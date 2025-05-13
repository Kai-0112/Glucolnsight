using ApplicationCore.Interfaces;
using Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Repositories
{
    public class GenericRepository<T> : IRepository<T> where T : class
    {
        private readonly GlucoInsightContext _ctx;
        private readonly DbSet<T> _db;

        public GenericRepository(GlucoInsightContext ctx)
        {
            _ctx = ctx;
            _db = ctx.Set<T>();
        }

        public async Task<T?> GetByIdAsync(int id) => await _db.FindAsync(id);

        public async Task<IReadOnlyList<T>> ListAsync() => await _db.ToListAsync();

        public async Task AddAsync(T entity) => await _db.AddAsync(entity);

        public void Update(T entity) => _db.Update(entity);

        public void Remove(T entity) => _db.Remove(entity);

        public Task<int> SaveChangesAsync() => _ctx.SaveChangesAsync();
    }
}
