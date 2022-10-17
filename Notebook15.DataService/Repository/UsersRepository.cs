using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notebook15.DataService.Data;
using Notebook15.DataService.IRepository;
using Notebook15.Domain.Models;

namespace Notebook15.DataService.Repository
{
    public class UsersRepository : GenericRepository<User>, IUsersRepository
    {
        public UsersRepository(AppDbContext context, ILogger logger) : base(context, logger)
        {
            
        }

        public override async Task<IEnumerable<User>> All(){
            try
            {
                return await dbSet.Where(x => x.Status == 1)
                                  .AsNoTracking()
                                  .ToListAsync();
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "{Repo} All method has generated an error", typeof(UsersRepository));
                return new List<User>();
            }
        } 
        
    }
}