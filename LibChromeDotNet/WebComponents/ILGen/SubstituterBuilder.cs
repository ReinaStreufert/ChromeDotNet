using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace LibChromeDotNet.WebComponents.ILGen
{
    public class SubstituterBuilder : ISubstituterBuilder
    {
        public static ISubstituterBuilder DefaultSubstituter() => new SubstituterBuilder("", DefaultSubstituter);

        private static IEnumerable<Expression> DefaultSubstituter(XmlElement prototypeXml, ISubstituterBuilderScope scope)
        {
            var tagName = prototypeXml.Name;
            var documentParameter = Expression.Parameter(typeof(XmlDocument));
            var documentGetter = typeof(IComponentRenderContext).GetProperty(nameof(IComponentRenderContext.Document))!.GetAccessors()[0];
            yield return Expression.Assign(documentParameter, Expression.Call(scope.ComponentRenderContext, documentGetter));
            var createElementMethod = typeof(XmlDocument).GetMethod(nameof(XmlDocument.CreateElement), new Type[] { typeof(string) })!;
            var elementParameter = Expression.Parameter(typeof(XmlElement));
            yield return Expression.Assign(elementParameter, Expression.Call(documentParameter, createElementMethod, Expression.Constant(tagName)));
            var preprocessedPrototype = prototypeXml.CloneNode(false);
            Expression? innerSubstitute = null;
            if (prototypeXml.HasAttribute("substitute"))
            {
                var innerSubstAttr = prototypeXml.GetAttribute("substitute");
                innerSubstitute = scope.GetSubstitutionBinding(innerSubstAttr);
                preprocessedPrototype.Attributes!.RemoveNamedItem("substitute");
            }
            var outerXmlSetter = typeof(XmlElement).GetProperty(nameof(XmlElement.OuterXml))!.GetAccessors()
                .Where(m => m.ReturnType == typeof(void))
                .FirstOrDefault()!;
            yield return Expression.Call(elementParameter, outerXmlSetter, Expression.Constant(preprocessedPrototype.OuterXml));
            if (innerSubstitute != null)
            {
                var toStringMethod = innerSubstitute.Type.GetMethod(nameof(object.ToString))!;
                var innerTextSetter = typeof(XmlElement).GetProperty(nameof(XmlElement.InnerText))!.GetAccessors()
                    .Where(m => m.ReturnType == typeof(void))
                    .FirstOrDefault()!;
                yield return Expression.Call(elementParameter, innerTextSetter, Expression.Call(innerSubstitute, toStringMethod));
            }
            var setAttributeMethod = typeof(XmlElement).GetMethod(nameof(XmlElement.SetAttribute), new Type[] { typeof(string), typeof(string) })!;
            for (int i = 0; i < prototypeXml.Attributes.Count; i++)
            {
                var attribute = prototypeXml.Attributes[i];
                if (!attribute.Value.StartsWith("$:"))
                    continue;
                var attrSubstitute = scope.GetSubstitutionBinding(attribute.Value.Substring(2));
                var toStringMethod = attrSubstitute.Type.GetMethod(nameof(object.ToString))!;
                yield return Expression.Call(
                    elementParameter,
                    setAttributeMethod,
                    Expression.Constant(attribute.Name),
                    Expression.Call(attrSubstitute, toStringMethod));
            }
            foreach (var childElement in prototypeXml.ChildNodes.OfType<XmlElement>())
            {
                var childBuilder = scope.GetBuilderForTagName(childElement.Name);
                foreach (var expr in childBuilder.GetExpressions(childElement, scope.Branch(elementParameter)))
                    yield return expr;
            }
            var appendChildMethod = typeof(XmlElement).GetMethod(nameof(XmlElement.AppendChild))!;
            yield return Expression.Call(scope.XmlContainer, appendChildMethod, elementParameter);
        }

        public string? Name => _Name;

        public SubstituterBuilder(string? name, Func<XmlElement, ISubstituterBuilderScope, IEnumerable<Expression>> getExpressionsCallback)
        {
            _Name = name;
            _GetExpressionsFunc = getExpressionsCallback;
        }

        private string? _Name;
        private Func<XmlElement, ISubstituterBuilderScope, IEnumerable<Expression>> _GetExpressionsFunc;

        public IEnumerable<Expression> GetExpressions(XmlElement substitutionPrototype, ISubstituterBuilderScope builderScope)
        {
            return _GetExpressionsFunc(substitutionPrototype, builderScope);
        }
    }
}
