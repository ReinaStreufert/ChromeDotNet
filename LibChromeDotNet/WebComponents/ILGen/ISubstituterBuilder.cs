using AssemblyGen;
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
        public void Build(XmlElement substitutionPrototype, ISubstituterBuilderScope builderScope);
    }

    public interface ISubstituterBuilderScope
    {
        public IMethodGeneratorContext GeneratorContext { get; }
        public Symbol This { get; }
        public Symbol ComponentRenderContext { get; }
        public Symbol XmlContainer { get; }
        public string? ContainerId { get; }
        public void SetLocalBinding(string name, AssignableSymbol value);
        public ISubstitution GetSubstitutionBinding(string memberExpr);
        public ISubstituterBuilderScope Branch(AssignableSymbol xmlContainer, string elementId);
        public ISubstituterBuilderScope BranchAndSet(string name, AssignableSymbol value);
        public ISubstituterBuilder GetBuilderForTagName(string name);
    }

    public interface ISubstitution
    {
        public LocalSymbol? ResourceTreeTip { get; }
        public Symbol GetValueSymbolic();
        public Type Type { get; }
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

    public delegate void Substituter(IComponentRenderContext context, IComponentResource resource);
}
