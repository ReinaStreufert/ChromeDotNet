using LibChromeDotNet.WebComponents.ILGen;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.WebComponents
{
    public class WebTemplateSet : IWebTemplateSet
    {
        private Dictionary<string, XmlDocument> _UnloadedTemplates = new Dictionary<string, XmlDocument>();
        private Dictionary<string, SubstituterInfo> _TemplateSubstituters = new Dictionary<string, SubstituterInfo>();
        private IEnumerable<ISubstituterBuilder> _SubstituterBuilders;

        public WebTemplateSet(IEnumerable<ISubstituterBuilder> builders)
        {
            _SubstituterBuilders = builders
                .Prepend(new DependencySubstitutionBuilder(this));
        }

        public void IncludeTemplate(XmlDocument templateDescription)
        {
            var docElement = templateDescription.DocumentElement ?? throw new ArgumentException(nameof(templateDescription));
            if (!docElement.HasAttribute("name"))
                throw new FormatException(nameof(templateDescription));
            var templateName = docElement.GetAttribute("name");
            _UnloadedTemplates.Add(templateName, templateDescription);
        }

        public void IncludeTemplates(IEnumerable<XmlDocument> templateDescriptions)
        {
            foreach (var template in templateDescriptions)
                IncludeTemplate(template);
        }

        public bool IsTemplateIncluded(string name) => _UnloadedTemplates.ContainsKey(name) || _TemplateSubstituters.ContainsKey(name);

        public bool IsTemplateLoaded(string name)
        {
            if (_TemplateSubstituters.ContainsKey(name))
                return true;
            if (!_UnloadedTemplates.ContainsKey(name))
                throw new ArgumentException($"{nameof(name)} does not refer to an included template");
        }

        public SubstituterInfo LoadTemplate(string name)
        {
            if (_TemplateSubstituters.TryGetValue(name, out var loadedSubstituter))
                return loadedSubstituter;
            if (!_UnloadedTemplates.TryGetValue(name, out var descriptor))
                throw new ArgumentException($"{nameof(name)} does not refer to an included template");
            var result = BuildSubstituter(descriptor);
            _TemplateSubstituters.Add(name, result);
            return result;
        }

        private SubstituterInfo BuildSubstituter(XmlDocument descriptor)
        {

        }

        private class BuilderScope : ISubstituterBuilderScope
        {
            public ParameterExpression This => _ThisBinding;
            public ParameterExpression ComponentRenderContext => _ComponentRenderContext;
            public ParameterExpression XmlContainer => _XmlContainer;
            public string? ContainerId => _ContainerId;

            public BuilderScope(WebTemplateSet set, ParameterExpression thisExpr, ParameterExpression componentRenderContextExpr, ParameterExpression documentElementExpr)
            {
                _Set = set;
                _ThisBinding = thisExpr;
                _ComponentRenderContext = componentRenderContextExpr;
                _XmlContainer = documentElementExpr;
                _LocalParameterScope = new Dictionary<string, ParameterExpression>();
            }

            private BuilderScope(BuilderScope baseScope, ParameterExpression xmlContainer, string? containerElementId)
            {
                _Set = baseScope._Set;
                _ThisBinding = baseScope._ThisBinding;
                _ComponentRenderContext = baseScope._ComponentRenderContext;
                _XmlContainer = xmlContainer;
                _ContainerId = containerElementId;
                _LocalParameterScope = new Dictionary<string, ParameterExpression>(baseScope._LocalParameterScope);
            }

            private WebTemplateSet _Set;
            private ParameterExpression _ThisBinding;
            private ParameterExpression _ComponentRenderContext;
            private ParameterExpression _XmlContainer;
            private string? _ContainerId;
            private Dictionary<string, ParameterExpression> _LocalParameterScope;

            public ISubstituterBuilderScope Branch(ParameterExpression xmlContainer, string elementId)
            {
                return new BuilderScope(this, xmlContainer, elementId);
            }

            public ISubstituterBuilderScope BranchAndSet(string name, ParameterExpression value)
            {
                var result = new BuilderScope(this, _XmlContainer, _ContainerId);
                result.SetLocalBinding(name, value);
                return result;
            }

            public ISubstituterBuilder GetBuilderForTagName(string name)
            {
                throw new NotImplementedException();
            }

            public Substitution GetSubstitutionBinding(string memberExpr)
            {
                throw new NotImplementedException();
            }

            public void SetLocalBinding(string name, ParameterExpression value)
            {
                _LocalParameterScope.Add(name, value);
            }
        }

        private class DependencySubstitutionBuilder : ISubstituterBuilder
        {
            public DependencySubstitutionBuilder(WebTemplateSet set)
            {
                _Set = set;
            }

            private WebTemplateSet _Set;

            public IEnumerable<Expression> GetExpressions(XmlElement substitutionPrototype, ISubstituterBuilderScope builderScope)
            {
                var substituterInfo = _Set.LoadTemplate(substitutionPrototype.Name);
                var branchMethod = typeof(IComponentRenderContext).GetMethod(nameof(IComponentRenderContext.Branch));
                var branchedRenderContextParameter = Expression.Parameter(typeof(IComponentRenderContext))!;
                yield return Expression.Assign(branchedRenderContextParameter, Expression.Call(builderScope.ComponentRenderContext, branchMethod!));
                var resourceType = substituterInfo.ResourceType;
                var resourceParameter = Expression.Parameter(substituterInfo.ResourceType);
                var resourceConstructor = resourceType.GetConstructor(new Type[] { })!;
                yield return Expression.Assign(resourceParameter, Expression.New(resourceConstructor));
                foreach (var attributeNode in substitutionPrototype.Attributes.OfType<XmlAttribute>())
                {
                    var property = resourceType.GetProperty(attributeNode.Name)
                        ?? throw new ArgumentException($"template contains reference to a property which does not exist '{resourceType.Name}.{attributeNode.Name}'");
                    var setter = property.GetSetMethod()
                        ?? throw new ArgumentException($"template property '{property.DeclaringType}.{property.Name}' has no public set accessor");
                    
                }

            }

            public bool IsMatchFor(string name) => _Set.IsTemplateIncluded(name);
        }
    }
}
