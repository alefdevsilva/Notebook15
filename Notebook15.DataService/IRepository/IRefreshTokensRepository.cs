using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Notebook15.Domain.DbSet;

namespace Notebook15.DataService.IRepository
{
    public interface IRefreshTokensRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken> GetByRefreshToken(string refreshToken);
        Task<bool> MarkRefreshTokenAsUsed(RefreshToken refreshToken);
    }
}