using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Notebook15.DataService.IRepository;

namespace Notebook15.DataService.IConfiguration
{
    public interface IUnitOfWork
    {
        IUsersRepository Users { get; }
        IRefreshTokensRepository RefreshTokens { get; }
        Task CompleteAsync();
    }
}