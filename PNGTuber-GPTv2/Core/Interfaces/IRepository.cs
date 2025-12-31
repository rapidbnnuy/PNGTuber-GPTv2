using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using PNGTuber_GPTv2.Core.Types;

namespace PNGTuber_GPTv2.Core.Interfaces
{
    public interface IRepository<T>
    {
        Result<T> GetById(string id);
        Result<T> GetById(int id);
        Result<IEnumerable<T>> Find(Expression<Func<T, bool>> predicate);
        Result Upsert(T entity);
        Result Delete(string id);
    }
}
