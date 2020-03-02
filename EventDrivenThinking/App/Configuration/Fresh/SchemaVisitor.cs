using System;
using System.Collections.Generic;
using EventDrivenThinking.EventInference.Schema;

namespace EventDrivenThinking.App.Configuration.Fresh
{
    public class SchemaVisitor
    {
        private readonly Dictionary<ISchema, object> _visitedSchemas;

        public SchemaVisitor()
        {
            _visitedSchemas = new Dictionary<ISchema, object>();
        }
        public void VisitOnce(ISchema schema, object by)
        {
            if (_visitedSchemas.TryGetValue(schema, out object owner))
            {
                if (by != owner)
                {
                    throw new Exception($"Slice was already configured by {owner}");
                }
            }
            else _visitedSchemas.Add(schema, by);
        }
    }
}