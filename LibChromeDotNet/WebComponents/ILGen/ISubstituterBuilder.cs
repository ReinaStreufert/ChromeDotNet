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
        public bool IsMatchFor(string name);
        public IEnumerable<Expression> GetExpressions(XmlElement substitutionPrototype, ISubstituterBuilderScope builderScope);
    }

    public interface ISubstituterBuilderScope
    {
        public ParameterExpression This { get; }
        public ParameterExpression ComponentRenderContext { get; }
        public ParameterExpression XmlContainer { get; }
        public string? ContainerId { get; }
        public void SetLocal(string name, Expression value);
        public ISubstitution GetSubstitutionBinding(string memberExpr);
        public ISubstituterBuilderScope Branch(ParameterExpression xmlContainer, string elementId);
        public ISubstituterBuilderScope BranchAndSet(string name, Expression value);
        public ISubstituterBuilder GetBuilderForTagName(string name);
        public Substituter DemandDependency(string templateName);
    }

    public interface ISubstitution
    {
        public Expression ValueExpression { get; }
        public Expression? ResourceTreeTip { get; }
    }

    public delegate XmlElement Substituter(IComponentRenderContext context, IWebComponent component);
}
