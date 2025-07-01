using AssemblyGen;
using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.CSS;
using LibChromeDotNet.HTML5.DOM;
using LibChromeDotNet.HTML5.JS;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.WebComponents.ILGen
{
    public class SubstituterBuilder : ISubstituterBuilder
    {
        private static readonly string[] _ReservedAttributeNames = new[]
        {
            "enumeration",
            "each",
            "when",
            "unless"
        };

        public static SubstituterBuilder Fallback => Default(string.Empty, SubstituterMatchType.Fallback, (prototypeXml, scope) =>
        {
            foreach (var childElement in prototypeXml.ChildNodes.OfType<XmlElement>())
            {
                var builder = scope.GetBuilderForTagName(childElement.Name);
                builder.Build(childElement, scope);
            }
        });

        public static SubstituterBuilder EnumerateSuffix => Default("enumerate", SubstituterMatchType.Suffix, (prototypeXml, scope) =>
        {
            var ctx = scope.GeneratorContext;
            var enumerationBinding = scope.GetSubstitutionBinding(prototypeXml.GetAttribute("enumeration"));
            var itemType = enumerationBinding.Type.GenericTypeArguments[0];
            var enumerator = ctx.DeclareLocal(typeof(IEnumerator<>).MakeGenericType(itemType));
            enumerator.Assign(enumerationBinding.GetValueSymbolic().CallMethod(nameof(IEnumerable.GetEnumerator)));
            var loop = ctx.BeginLoop();
            var breakIfStatement = ctx.BeginIfStatement(enumerator
                .CallMethod(nameof(IEnumerator.MoveNext))
                .Operation(UnaryOperator.Not));
            loop.Break();
            breakIfStatement.End();
            var item = ctx.DeclareLocal(itemType);
            item.Assign(enumerator.GetFieldOrProperty(nameof(IEnumerator.Current)));
            var branchedScope = scope.BranchAndSet(prototypeXml.GetAttribute("each"), item);
            foreach (var childElement in prototypeXml.ChildNodes.OfType<XmlElement>())
            {
                var builder = scope.GetBuilderForTagName(childElement.Name);
                builder.Build(childElement, branchedScope);
            }
            loop.End();
            BuildChangeListener(scope, enumerationBinding, scope.ContainerId!, (domNode, updatedValue) =>
            {
                
            });
        });

        private static SubstituterBuilder Default(string name, SubstituterMatchType matchType, Action<XmlElement, ISubstituterBuilderScope> childContentCallback) => Builder(name, matchType, (prototypeXml, scope) =>
        {
            var ctx = scope.GeneratorContext;
            var xmlDocument = scope.ComponentRenderContext.GetFieldOrProperty(nameof(IComponentRenderContext.Document));
            var elementNode = ctx.DeclareLocal(typeof(XmlElement));
            var elementNameSplit = prototypeXml.Name.Split('.');
            var elementName = elementNameSplit.Length == 2 ? elementNameSplit[0] : prototypeXml.Name;
            elementNode.Assign(xmlDocument.CallMethod(nameof(XmlDocument.CreateElement), ctx.Constant(elementName)));
            var elementId = Identifier.Random();
            elementNode.CallMethod(nameof(XmlElement.SetAttribute), ctx.Constant("id"), ctx.Constant(elementId));
            var prototypeAttributes = prototypeXml.Attributes.Cast<XmlAttribute>();
            var domExtensions = ctx.StaticType(typeof(DOMExtensions));
            foreach (var attr in prototypeAttributes)
            {
                if (_ReservedAttributeNames.Contains(attr.Name))
                    continue;
                if (attr.Name == "substitute")
                {
                    var innerTextBinding = scope.GetSubstitutionBinding(attr.Value);
                    elementNode.SetFieldOrProperty(nameof(XmlElement.InnerText), innerTextBinding.GetValueSymbolic());
                    BuildChangeListener(scope, innerTextBinding, elementId, (domNode, updatedValue) =>
                        domExtensions.CallMethod(nameof(DOMExtensions.SetInnerTextAsync), domNode, updatedValue));
                }
                else if (attr.Value.StartsWith("="))
                {
                    var attrValueBinding = scope.GetSubstitutionBinding(attr.Value.Substring(1));
                    elementNode.CallMethod(nameof(XmlElement.SetAttribute), ctx.Constant(attr.Name), attrValueBinding.GetValueSymbolic());
                    BuildChangeListener(scope, attrValueBinding, elementId, (domNode, updatedValue) =>
                        domNode.CallMethod(nameof(IDOMNode.SetAttributeAsync), updatedValue));
                }
                else
                    elementNode.CallMethod(nameof(XmlElement.SetAttribute), ctx.Constant(attr.Value));
            }
            scope.XmlContainer.CallMethod(nameof(XmlNode.AppendChild), elementNode);
            var branchedScope = scope.Branch(elementNode, elementId);
            childContentCallback(prototypeXml, branchedScope);
        });

        private static void BuildChangeListener(ISubstituterBuilderScope scope, ISubstitution binding, string domElementId, ChangeListenerDOMHandlerBuilder onChangeCallback)
        {
            var resourceTreeTip = binding.ResourceTreeTip ?? throw new ArgumentException(nameof(binding));
            var ctx = scope.GeneratorContext;
            var domNodeParam = typeof(IDOMNode).AsParameter();
            var domActionLambda = ctx.BeginLambda(typeof(void), domNodeParam);
            var domNode = ctx.GetArgument(domNodeParam);
            BuildChangeListener(scope, binding, updatedValue => onChangeCallback(domNode, updatedValue));
            domActionLambda.End();
            scope.ComponentRenderContext.CallMethod(nameof(IComponentRenderContext.AddDOMAction),
                ctx.Constant(domElementId),
                domActionLambda.ToDelegate(typeof(Action<IDOMNode>)));
        }

        private static void BuildChangeListener(ISubstituterBuilderScope scope, ISubstitution binding, Action<Symbol> onChangeCallback)
        {
            var resourceTreeTip = binding.ResourceTreeTip ?? throw new ArgumentException(nameof(binding));
            var ctx = scope.GeneratorContext;
            var propertyChangeListenerType = ctx.StaticType(typeof(PropertyChangeListener<,>)
                .MakeGenericType(resourceTreeTip.Type, binding.Type));
            var getPropertyLambda = ctx.BeginLambda(binding.Type);
            ctx.Return(binding.GetValueSymbolic());
            getPropertyLambda.End();
            var chkPropLeftParam = binding.Type.AsParameter();
            var chkPropRightParam = binding.Type.AsParameter();
            var validatePropertyLambda = ctx.BeginLambda(typeof(bool), chkPropLeftParam, chkPropRightParam);
            ctx.Return(ctx.GetArgument(chkPropLeftParam)
                .Operation(BinaryOperator.EqualTo, ctx.GetArgument(chkPropRightParam)));
            validatePropertyLambda.End();
            var updatedValueParam = binding.Type.AsParameter();
            var valueChangedLambda = ctx.BeginLambda(typeof(void), updatedValueParam);
            onChangeCallback(ctx.GetArgument(updatedValueParam));
            valueChangedLambda.End();
            propertyChangeListenerType.CallMethod(nameof(PropertyChangeListener<IComponentResource, object>.Create),
                resourceTreeTip,
                getPropertyLambda.ToDelegate(typeof(Func<>).MakeGenericType(binding.Type)),
                validatePropertyLambda.ToDelegate(typeof(Func<,,>).MakeGenericType(binding.Type, binding.Type, typeof(bool))),
                valueChangedLambda.ToDelegate(typeof(Action<>).MakeGenericType(binding.Type)));
        }

        private delegate void ChangeListenerDOMHandlerBuilder(Symbol domNode, Symbol updatedValue);

        private static SubstituterBuilder Builder(string name, SubstituterMatchType matchType, Action<XmlElement, ISubstituterBuilderScope> buildCallback)
            => new SubstituterBuilder(name, matchType, buildCallback);

        public SubstituterBuilder(string name, SubstituterMatchType matchType, Action<XmlElement, ISubstituterBuilderScope> buildCallback)
        {
            _Name = name;
            _MatchType = matchType;
            _BuildAction = buildCallback;
        }

        private string _Name;
        private SubstituterMatchType _MatchType;
        private Action<XmlElement, ISubstituterBuilderScope> _BuildAction;

        public void Build(XmlElement substitutionPrototype, ISubstituterBuilderScope builderScope)
        {
            _BuildAction(substitutionPrototype, builderScope);
        }

        public bool IsMatchFor(string name)
        {
            return _MatchType switch
            {
                SubstituterMatchType.Fallback => true,
                SubstituterMatchType.Suffix => name.EndsWith($".{_Name}"),
                SubstituterMatchType.Name => name == _Name,
                _ => false
            };
        }

        public enum SubstituterMatchType
        {
            Fallback,
            Suffix,
            Name
        }
    }
}
