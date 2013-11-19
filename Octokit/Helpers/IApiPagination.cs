using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Octokit
{
    public interface IApiPagination
    {
        Task<IReadOnlyList<T>> GetAllPages<T>(Func<Task<IReadOnlyPagedCollection<T>>> getFirstPage);
    }
}