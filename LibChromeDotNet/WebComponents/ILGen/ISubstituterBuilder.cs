using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.WebComponents.ILGen
{
    public interface ISubstituterBuilder
    {
        public string? Name { get; }
        public IEnumerable<Expression> GetExpressions(XmlElement substitutionPrototype, ISubstituterBuilderScope builderScope);
    }

    public interface ISubstituterBuilderScope
    {
        public ParameterExpression This { get; }
        public ParameterExpression ComponentRenderContext { get; }
        public ParameterExpression XmlContainer { get; }
        public void SetLocal(string name, Expression value);
        public Expression GetSubstitutionBinding(string memberExpr);
        public ISubstituterBuilderScope Branch(ParameterExpression? xmlContainer = null);
        public ISubstituterBuilderScope BranchAndSet(string name, Expression value, ParameterExpression? xmlContainer = null);
        public ISubstituterBuilder GetBuilderForTagName(string name);
    }

    public delegate XmlElement Substituter(IComponentRenderContext context, IWebComponent component);
}
