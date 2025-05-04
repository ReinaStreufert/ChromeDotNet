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
        public void SetLocalBinding(string name, ParameterExpression value);
        public Substitution GetSubstitutionBinding(string memberExpr);
        public ISubstituterBuilderScope Branch(ParameterExpression xmlContainer, string elementId);
        public ISubstituterBuilderScope BranchAndSet(string name, ParameterExpression value);
        public ISubstituterBuilder GetBuilderForTagName(string name);
    }

    public class Substitution
    {
        public Expression ValueExpression { get; }
        public Expression? ResourceTreeTip { get; }

        public Substitution(Expression valueExpression, Expression? resourceTreeTip)
        {
            ValueExpression = valueExpression;
            ResourceTreeTip = resourceTreeTip;
        }
    }

    public class SubstituterInfo
    {
        public Type ResourceType { get; }
        public Substituter Substituter { get; }

        public SubstituterInfo(Type resourceType, Substituter substituter)
        {
            ResourceType = resourceType;
            Substituter = substituter;
        }
    }

    public delegate XmlElement Substituter(IComponentRenderContext context, IComponentResource resource);
}
