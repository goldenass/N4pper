﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using OMnG;

namespace N4pper.Orm.Design
{
    internal class GenericBuilder<T> : IConstraintBuilder<T> where T : class
    {
        private void ManageConnectionSource<D>(IEnumerable<string> from)
        {
            PropertyInfo f = typeof(T).GetProperty(from.First());

            if (!OrmCoreTypes.KnownTypeSourceRelations.ContainsKey(f))
            {
                OrmCoreTypes.KnownTypeSourceRelations.Add(f, null);
            }
        }
        private void ManageConnectionDestination<D>(IEnumerable<string> from, IEnumerable<string> back)
        {
            PropertyInfo f = from != null ? typeof(T).GetProperty(from.First()) : null;
            PropertyInfo b = typeof(D).GetProperty(back.First());

            if (!OrmCoreTypes.KnownTypeDestinationRelations.ContainsKey(b))
            {
                OrmCoreTypes.KnownTypeDestinationRelations.Add(b, f);
                if(f!=null)
                {
                    OrmCoreTypes.KnownTypeSourceRelations[f] = b;
                }
            }
        }
        public IReverseConnectionBuilder<D, T> Connected<D>(Expression<Func<T, D>> from = null) where D : class
        {
            IEnumerable<string> fromP = null;
            if (from != null)
            {
                fromP = from.ToPropertyNameCollection();
                if (fromP.Count() != 1)
                    throw new ArgumentException("Only a single navigation property must be specified", nameof(from));

                ManageConnectionSource<D>(fromP);
            }

            return new ReverseConnectionBuilder<D, T>((backP=> ManageConnectionDestination<D>(fromP, backP)));
        }
        
        public IReverseConnectionBuilder<D, T> ConnectedMany<D>(Expression<Func<T, IEnumerable<D>>> from = null) where D : class
        {
            IEnumerable<string> fromP = null;
            if (from != null)
            {
                fromP = from.ToPropertyNameCollection();
                if (fromP.Count() != 1)
                    throw new ArgumentException("Only a single navigation property must be specified", nameof(from));

                ManageConnectionSource<D>(fromP);
            }

            return new ReverseConnectionBuilder<D, T>((backP => ManageConnectionDestination<D>(fromP, backP)));
        }
        
        public IPropertyConstraintBuilder<T> Ignore(Expression<Func<T, object>> expr)
        {
            expr = expr ?? throw new ArgumentNullException(nameof(expr));

            foreach (string item in expr.ToPropertyNameCollection())
            {
                if (OrmCoreTypes.KnownTypes[typeof(T)].Contains(item))
                    throw new InvalidOperationException("It's not possible to ignore a key property.");
                if (!OrmCoreTypes.KnownTypesIngnoredProperties[typeof(T)].Contains(item))
                    OrmCoreTypes.KnownTypesIngnoredProperties[typeof(T)].Add(item);
            }

            return this;
        }

        public IReverseTypedConnectionBuilder<D, C, T> ConnectedWith<C, D>(Expression<Func<T, C>> source = null)
            where C : Entities.ExplicitConnection<T, D>
            where D : class
        {
            if (typeof(Entities.ExplicitConnection).IsAssignableFrom(typeof(C)) && typeof(C).BaseType.GetGenericTypeDefinition() != typeof(Entities.ExplicitConnection<,>))
                throw new ArgumentException($"An explicit connection must inherit directly from {typeof(Entities.ExplicitConnection<,>).Name}");

            IEnumerable<string> fromP = null;
            if (source != null)
            {
                fromP = source.ToPropertyNameCollection();
                if (fromP.Count() != 1)
                    throw new ArgumentException("Only a single navigation property must be specified", nameof(source));

                ManageConnectionSource<D>(fromP);
            }

            return new ReverseTypedConnectionBuilder<D, C, T>((backP => ManageConnectionDestination<D>(fromP, backP)));
        }

        public IReverseTypedConnectionBuilder<D, C, T> ConnectedManyWith<C, D>(Expression<Func<T, IEnumerable<C>>> source = null)
            where C : Entities.ExplicitConnection<T, D>
            where D : class
        {
            if (typeof(Entities.ExplicitConnection).IsAssignableFrom(typeof(C)) && typeof(C).BaseType.GetGenericTypeDefinition() != typeof(Entities.ExplicitConnection<,>))
                throw new ArgumentException($"An explicit connection must inherit directly from {typeof(Entities.ExplicitConnection<,>).Name}");

            IEnumerable<string> fromP = null;
            if (source != null)
            {
                fromP = source.ToPropertyNameCollection();
                if (fromP.Count() != 1)
                    throw new ArgumentException("Only a single navigation property must be specified", nameof(source));

                ManageConnectionSource<D>(fromP);
            }

            return new ReverseTypedConnectionBuilder<D, C, T>((backP => ManageConnectionDestination<D>(fromP, backP)));
        }
    }
}
