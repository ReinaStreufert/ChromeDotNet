using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.JS;
using LibChromeDotNet.WebComponents.ILGen;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.WebComponents
{
    public class WebTemplateSet : IWebTemplateSet
    {
        private static ImmutableDictionary<string, Type>? LoadedTypes;

        private static ImmutableDictionary<string, Type> GetLoadedTypes()
        {
            if (LoadedTypes == null)
            {
                LoadedTypes = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(a => a.GetExportedTypes())
                    .Where(t => t.FullName != null)
                    .ToImmutableDictionary(t => t.FullName!);
            }
            return LoadedTypes;
        }

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
            return false;
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
            var docElement = descriptor.DocumentElement!;
            var templateName = docElement.GetAttribute("name");
            if (!docElement.HasAttribute("bindingType"))
                throw new FormatException($"Template '{templateName}' has no bindingType attribute defined");
            var typeName = docElement.GetAttribute("bindingType");
            if (!GetLoadedTypes().TryGetValue(typeName, out var bindingType))
                throw new FormatException($"Template references binding type which does not exist in any loaded assembly '{typeName}'");
            var resourceParameter = Expression.Parameter(typeof(IComponentResource));
            var renderContextParamater = Expression.Parameter(typeof(IComponentRenderContext));
            if (docElement.ChildNodes.OfType<XmlElement>().Count() != 1)
                throw new FormatException($"Template tag must have only one child tag");
            var rootBuilderTag = docElement.ChildNodes
                .OfType<XmlElement>()
                .First();
            var lambdaExpression = Expression.Lambda(
                Expression.Block(BuildLambdaBody(rootBuilderTag, resourceParameter, renderContextParamater, bindingType)),
                renderContextParamater,
                resourceParameter);
            return new SubstituterInfo(bindingType, (Substituter)lambdaExpression.Compile());
        }

        private IEnumerable<Expression> BuildLambdaBody(XmlElement rootBuilderTag, ParameterExpression resourceParameter, ParameterExpression renderContextParameter, Type bindingType)
        {
            var thisParameter = Expression.Parameter(bindingType);
            yield return Expression.Assign(thisParameter, Expression.Convert(resourceParameter, bindingType));
            var documentParameter = Expression.Parameter(typeof(XmlNode));
            var xmlDocumentGetter = typeof(IComponentRenderContext).GetProperty(nameof(IComponentRenderContext.Document))!
                .GetGetMethod()!;
            yield return Expression.Assign(
                documentParameter,
                Expression.Call(renderContextParameter, xmlDocumentGetter));
            var scope = new BuilderScope(this, thisParameter, renderContextParameter, documentParameter);
            var rootBuilder = scope.GetBuilderForTagName(rootBuilderTag.Name);
            foreach (var expr in rootBuilder.Build(rootBuilderTag, scope))
                yield return expr;
        }

        private class BuilderScope : ISubstituterBuilderScope
        {
            public ParameterExpression This => _ThisBinding;
            public ParameterExpression ComponentRenderContext => _ComponentRenderContext;
            public ParameterExpression XmlContainer => _XmlContainer;
            public string? ContainerId => _ContainerId;

            public BuilderScope(WebTemplateSet set, ParameterExpression thisExpr, ParameterExpression componentRenderContextExpr, ParameterExpression documentExpr)
            {
                _Set = set;
                _ThisBinding = thisExpr;
                _ComponentRenderContext = componentRenderContextExpr;
                _XmlContainer = documentExpr;
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
                return _Set._SubstituterBuilders
                    .Where(b => b.IsMatchFor(name))
                    .First();
            }

            public Substitution GetSubstitutionBinding(string memberExpr)
            {
                Expression binding = _ThisBinding;
                Expression? resourceTreeTip = binding;
                var first = true;
                foreach (var segment in memberExpr.Split('.'))
                {
                    if (first)
                    {
                        first = false;
                        if (_LocalParameterScope.TryGetValue(segment, out var localExpr))
                        {
                            binding = localExpr;
                            resourceTreeTip = binding.Type.IsSubclassOf(typeof(IComponentResource)) ? binding : null;
                            break;
                        }
                    }
                    var propertyGetter = binding.Type.GetProperty(segment)?.GetGetMethod();
                    if (propertyGetter == null)
                        throw new ArgumentException($"template references '{segment}' which is not a member of type '{binding.Type}'");
                    binding = Expression.Call(binding, propertyGetter);
                    if (propertyGetter.ReturnType.IsSubclassOf(typeof(IComponentRenderContext)))
                        resourceTreeTip = binding;
                }
                return new Substitution(binding, resourceTreeTip);
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

            public IEnumerable<Expression> Build(XmlElement substitutionPrototype, ISubstituterBuilderScope scope)
            {
                var documentParameter = Expression.Parameter(typeof(XmlNode));
                var xmlDocumentGetter = typeof(IComponentRenderContext).GetProperty(nameof(IComponentRenderContext.Document))!
                    .GetGetMethod()!;
                yield return Expression.Assign(
                    documentParameter,
                    Expression.Call(scope.ComponentRenderContext, xmlDocumentGetter));
                var substituterInfo = _Set.LoadTemplate(substitutionPrototype.Name);
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
                    var propertyValue = scope.GetSubstitutionBinding(attributeNode.Value);
                    yield return Expression.Call(resourceParameter, setter, propertyValue.Value);
                }
                var createElementMethod = typeof(XmlDocument).GetMethod(nameof(XmlDocument.CreateElement), new Type[] { typeof(string) })!;
                var placeholderElementId = JSIdentifier.New();
                var placeholderParameter = Expression.Parameter(typeof(XmlElement));
                yield return Expression.Assign(placeholderParameter, Expression.Call(documentParameter, createElementMethod, Expression.Constant("span")));
                var setAttributeMethod = typeof(XmlElement).GetMethod(nameof(XmlElement.SetAttribute), new Type[] { typeof(string), typeof(string) })!;
                yield return Expression.Call(placeholderParameter, setAttributeMethod, Expression.Constant("id"), Expression.Constant(placeholderElementId));
                var appendChildMethod = typeof(XmlNode).GetMethod(nameof(XmlNode.AppendChild))!;
                yield return Expression.Call(scope.XmlContainer, appendChildMethod, placeholderParameter);
                var addDOMActionMethod = typeof(IComponentRenderContext).GetMethod(nameof(IComponentRenderContext.AddDOMAction))!;
                var domNodeParameter = Expression.Parameter(typeof(IDOMNode));
                var renderTemplateMethod = typeof(WebTemplateRenderer).GetMethod(nameof(WebTemplateRenderer.RenderTemplateAsync))!;
                var domActionCallback = Expression.Lambda(
                    Expression.Call(renderTemplateMethod, domNodeParameter, Expression.Constant(substituterInfo), resourceParameter),
                    domNodeParameter);
                yield return Expression.Call(scope.ComponentRenderContext, addDOMActionMethod, domActionCallback);
            }

            public bool IsMatchFor(string name) => _Set.IsTemplateIncluded(name);
        }
    }
}
