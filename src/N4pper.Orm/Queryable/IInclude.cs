﻿using N4pper.Orm.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace N4pper.Orm.Queryable
{
    public interface IInclude<T> where T : class
    {
        IInclude<D> Include<D>(Expression<Func<T, D>> expr) where D : class;
        IInclude<D> Include<D>(Expression<Func<T, IEnumerable<D>>> expr) where D : class;
        IInclude<D> Include<D>(Expression<Func<T, IList<D>>> expr) where D : class;
        IInclude<D> Include<D>(Expression<Func<T, List<D>>> expr) where D : class;

        IInclude<D> Include<C, D>(Expression<Func<T, C>> expr) where C : ExplicitConnection<T,D> where D : class;
        IInclude<D> Include<C, D>(Expression<Func<T, IEnumerable<C>>> expr) where C : ExplicitConnection<T, D> where D : class;
        IInclude<D> Include<C, D>(Expression<Func<T, IList<C>>> expr) where C : ExplicitConnection<T, D> where D : class;
        IInclude<D> Include<C, D>(Expression<Func<T, List<C>>> expr) where C : ExplicitConnection<T, D> where D : class;
    }
}
