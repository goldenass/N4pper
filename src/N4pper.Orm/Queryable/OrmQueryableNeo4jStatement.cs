﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using N4pper.Queryable;
using N4pper.QueryUtils;
using Neo4j.Driver.V1;
using OMnG;

namespace N4pper.Orm.Queryable
{
    internal class OrmQueryableNeo4jStatement<TData> : IOrderedQueryable<TData>, IInclude<TData> where TData : class
    {
        public IStatementRunner Runner { get; set; }
        public Statement Statement { get; set; }
        public Func<IRecord, Type, object> Mapper { get; set; }

        #region Constructors
        /// <summary> 
        /// This constructor is called by the client to create the data source. 
        /// </summary> 
        public OrmQueryableNeo4jStatement(IStatementRunner runner, Func<IRecord, Type, object> mapper)
        {
            Runner = runner ?? throw new ArgumentNullException(nameof(runner));
            Mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));

            Provider = new CypherQueryProvider(runner, () => { BuildStatement(); return Statement; }, mapper);
            Expression = Expression.Constant(this);
        }

        /// <summary> 
        /// This constructor is called by Provider.CreateQuery(). 
        /// </summary> 
        /// <param name="expression"></param>
        public OrmQueryableNeo4jStatement(IStatementRunner runner, Func<Statement> statement, Func<IRecord, Type, object> mapper, CypherQueryProvider provider, Expression expression)
            : this(runner, mapper)
        {
            provider = provider ?? throw new ArgumentNullException(nameof(provider));
            expression = expression ?? throw new ArgumentNullException(nameof(expression));

            if (!typeof(IQueryable<TData>).IsAssignableFrom(expression.Type))
            {
                throw new ArgumentOutOfRangeException(nameof(expression));
            }

            Statement = statement();
            Provider = provider;
            Expression = expression;
        }
        #endregion

        #region Properties

        public IQueryProvider Provider { get; private set; }
        public Expression Expression { get; private set; }

        public Type ElementType
        {
            get { return typeof(TData); }
        }

        #endregion

        #region Enumerators
        public virtual IEnumerator<TData> GetEnumerator()
        {
            if (Statement == null)
                BuildStatement();
            return (Provider.Execute<IEnumerable<TData>>(Expression)).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            if (Statement == null)
                BuildStatement();
            return (Provider.Execute<System.Collections.IEnumerable>(Expression)).GetEnumerator();
        }
        #endregion

        private string RemoveAsStatement(string statement)
        {
            return Regex.Replace(statement, @"\s+AS\s+.+$", "", RegexOptions.IgnoreCase);
        }

        private string BuildMatchStatement()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append($"MATCH {new Node(FirstSymbol, typeof(TData)).BuildForQuery()}");
            Stack<Symbol> symbols = new Stack<Symbol>();
            symbols.Push(FirstSymbol);
            foreach (IncludePathTree item in Paths.Branches)
            {
                builder.Append(" OPTIONAL MATCH ");
                RecursiveBuildMatchStatement(item, builder, symbols);
            }
            symbols.Pop();

            return builder.ToString();
        }
        private void RecursiveBuildMatchStatement(IncludePathTree tree, StringBuilder builder, Stack<Symbol> symbols)
        {
            Type t = tree.Path.IsEnumerable ? tree.Path.Property.PropertyType.GetGenericArguments()[0] : tree.Path.Property.PropertyType;
            builder.Append(
                new Node(symbols.Peek())
                ._(type:typeof(Entities.Connection), props: new { PropertyName = tree.Path.Property.Name }.ToPropDictionary())
                ._V(tree.Path.Symbol, t).BuildForQuery()
                );
            symbols.Push(tree.Path.Symbol);
            foreach (IncludePathTree item in tree.Branches)
            {
                builder.Append(" OPTIONAL MATCH ");
                RecursiveBuildMatchStatement(item, builder, symbols);
            }
            symbols.Pop();
        }
        private string BuildReturnStatement()
        {
            StringBuilder builder = new StringBuilder();

            List<IncludePathComponent> symbols = new List<IncludePathComponent>() { Paths.Path };

            Dictionary<IncludePathTree, IncludePathComponent> res = RecursiveBuildReturnStatement(new List<IncludePathTree>(){ Paths }, builder, symbols);

            builder.Append($" RETURN {FirstSymbol}");
            if (res.Keys.Count > 0)
                builder.Append($",{res[Paths].Symbol}");

            return builder.ToString();
        }
        private Dictionary<IncludePathTree, IncludePathComponent> RecursiveBuildReturnStatement(List<IncludePathTree> trees, StringBuilder builder, List<IncludePathComponent> symbols)
        {
            Dictionary<IncludePathTree, IncludePathComponent> replacements = new Dictionary<IncludePathTree, IncludePathComponent>();
              List <IncludePathTree> branches = trees.SelectMany(p => p.Branches).ToList();

            StringBuilder sb = new StringBuilder();

            if (branches.Count > 0)
            {
                symbols.AddRange(branches.Select(p => p.Path));

                replacements = RecursiveBuildReturnStatement(branches, sb, symbols);

                sb.Append($" WITH ");
                sb.Append(string.Join(",", symbols.Select(p => p.Symbol)));
                
                foreach (IncludePathTree branch in trees)
                {
                    Symbol s = new Symbol();
                    sb.Append($",{{this:{branch.Path.Symbol}");

                    foreach (IncludePathTree item in branch.Branches)
                    {
                        IncludePathComponent path = replacements.ContainsKey(item) ? replacements[item] : item.Path;

                        sb.Append(",");
                        sb.Append($"{path.Property.Name}:");
                        if (path.IsEnumerable)
                        {
                            sb.Append("collect(distinct ");
                        }
                        sb.Append(path.Symbol);
                        if (path.IsEnumerable)
                        {
                            sb.Append(")");
                        }
                    }

                    sb.Append($"}} AS {s}");
                    replacements.Add(branch, new IncludePathComponent() { Property = branch.Path.Property, IsEnumerable = branch.Path.IsEnumerable, Symbol = s });
                }
            }
            else
            {
                sb.Append($" WITH ");
                sb.Append(string.Join(",", symbols.Select(p => p.Symbol)));
                foreach (IncludePathTree branch in trees)
                {
                    Symbol s = new Symbol();
                    sb.Append($",{{this:{branch.Path.Symbol}}} AS {s}");
                    replacements.Add(branch, new IncludePathComponent() { Property = branch.Path.Property, IsEnumerable = branch.Path.IsEnumerable, Symbol = s });
                }
            }

            builder.Append(sb.ToString());

            symbols.RemoveAll(p => trees.Select(t => t.Path).Contains(p));

            return replacements;
        }

        protected void BuildStatement()
        {
            StringBuilder sb = new StringBuilder();

            sb.Append(BuildMatchStatement());
            sb.Append(BuildReturnStatement());
            
            Statement = new Statement(sb.ToString());
        }

        protected IncludePathTree Paths { get; } = new IncludePathTree() { Path = new IncludePathComponent() { IsEnumerable=false, Symbol=new Symbol() } };
        protected Symbol FirstSymbol => Paths.Path.Symbol;

        protected IInclude<D> StartNewInclude<D>(IEnumerable<string> props, bool isEnumerable) where D : class
        {
            if (props.Count() != 1)
                throw new ArgumentException("Only a single navigation property must be specified", nameof(props));
            PropertyInfo pinfo = typeof(TData).GetProperty(props.First());

            Symbol to = new Symbol();
            IncludePathTree newTree = new IncludePathTree()
            {
                Path = new IncludePathComponent() { Property = pinfo, IsEnumerable = isEnumerable, Symbol = to } 
            };

            newTree = Paths.Add(newTree);

            //Rel rel = new Rel(
            //    null,
            //    typeof(Entities.Connection),
            //    new { PropertyName = pinfo.Name }.ToPropDictionary());
            //Node node = new Node(to, typeof(D));
            
            //StringBuilder sb = new StringBuilder();
            //Builders.Add(sb);
            //List<string> returns = new List<string>();
            //ReturnStatements.Add(returns);

            //if (isEnumerable)
            //    returns.Add($"reverse(collect({to})) as {to}");
            //else
            //    returns.Add(to);

            //sb.Append($"OPTIONAL MATCH {new Node(FirstSymbol, typeof(TData)).BuildForQuery()}-{rel.BuildForQuery()}->{node.BuildForQuery()}");

            return new IncludeQueryBuilder<D>(newTree);
        }

        public IInclude<D> Include<D>(Expression<Func<TData, D>> expr) where D : class
        {
            expr = expr ?? throw new ArgumentNullException(nameof(expr));
            return StartNewInclude<D>(expr.ToPropertyNameCollection(), false);
        }

        public IInclude<D> Include<D>(Expression<Func<TData, IEnumerable<D>>> expr) where D : class
        {
            expr = expr ?? throw new ArgumentNullException(nameof(expr));
            return StartNewInclude<D>(expr.ToPropertyNameCollection(), true);
        }

        public IInclude<D> Include<D>(Expression<Func<TData, IList<D>>> expr) where D : class
        {
            expr = expr ?? throw new ArgumentNullException(nameof(expr));
            return StartNewInclude<D>(expr.ToPropertyNameCollection(), true);
        }

        public IInclude<D> Include<D>(Expression<Func<TData, List<D>>> expr) where D : class
        {
            expr = expr ?? throw new ArgumentNullException(nameof(expr));
            return StartNewInclude<D>(expr.ToPropertyNameCollection(), true);
        }
    }
}
