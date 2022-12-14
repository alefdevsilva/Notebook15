using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Notebook15.Domain.DbSet;

namespace Notebook15.DataService.IRepository
{
    public interface IUsersRepository : IGenericRepository<User>
    {
        Task<bool> UpdateUserProfile(User user);
        Task<User> GetByIdentityId(Guid IdentityId);
    }
}