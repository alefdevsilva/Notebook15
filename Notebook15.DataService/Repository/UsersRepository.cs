using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notebook15.DataService.Data;
using Notebook15.DataService.IRepository;
using Notebook15.Domain.DbSet;

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

        public async Task<User> GetByIdentityId(Guid IdentityId)
        {
             try
            {
                return await dbSet.Where(x => x.Status == 1
                                    && x.IdentityId == IdentityId)
                                .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Repo} GetByIdentityId method has generated an error", typeof(UsersRepository));
                return null;
            }
        }

        public async Task<bool> UpdateUserProfile(User user)
        {
             try
            {
                var existingUser = await dbSet.Where(x => x.Status == 1
                                    && x.Id == user.Id)
                                  .FirstOrDefaultAsync();

                if (existingUser == null) return false;

                existingUser.FirstName = user.FirstName;
                existingUser.LastName = user.LastName;
                existingUser.MobileNumber = user.MobileNumber;
                existingUser.Phone = user.Phone;
                existingUser.Sex = user.Sex;
                existingUser.UpdateDate = DateTime.UtcNow;
                existingUser.Address = user.Address;

                return true;
            }
            catch (Exception ex)
            {
                
                _logger.LogError(ex, "{Repo} UpdateUserProfile method has generated an error", typeof(UsersRepository));
                return false;
            }
        }
    }
}