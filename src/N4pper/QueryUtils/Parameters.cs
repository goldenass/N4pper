﻿using OMnG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace N4pper.QueryUtils
{
    public class Parameters
    {
        public IList<string> Mappings { get; protected set; }
        public string Suffix { get; protected set; }
        public string Prefix { get; protected set; }
        public Parameters(IEnumerable<string> props, string suffix=null, string prefix = null)
        {
            props = props ?? throw new ArgumentNullException(nameof(props));

            Mappings = props.ToList();
            Suffix = suffix ?? "";
            Prefix = prefix ?? "";
        }

        public void Apply(IEntity entity)
        {
            foreach (string key in Mappings)
            {
                if (entity.Props.ContainsKey(key) && (
                    entity.Props[key] == null || 
                    !entity.Props[key].IsDateTime() && entity.Props[key].GetType() != typeof(TimeSpan) && entity.Props[key].GetType() != typeof(TimeSpan?)))
                    entity.Props[key] = new Parameter($"{Prefix}{key}{Suffix}");
            }
        }

        public Dictionary<string, object> Prepare(IDictionary<string, object> original)
        {
            original = original ?? new Dictionary<string, object>();
            Dictionary<string, object> values = new Dictionary<string, object>();
            foreach (string key in Mappings)
            {
                if (original.ContainsKey(key) && 
                    (original[key]==null || 
                    !original[key].IsDateTime() && original[key].GetType()!=typeof(TimeSpan) && original[key].GetType() != typeof(TimeSpan?)))
                    values.Add($"{Prefix}{key}{Suffix}", original[key]);
            }

            return values;
        }
    }
}
