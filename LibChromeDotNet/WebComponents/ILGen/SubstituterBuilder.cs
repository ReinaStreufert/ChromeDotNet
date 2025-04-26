using LibChromeDotNet.HTML5.JS;
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
            var preprocessedPrototype = (XmlElement)prototypeXml.CloneNode(false);
            var nodeId = Identifier.New();
            preprocessedPrototype.SetAttribute("id", nodeId);
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



        public static ISubstituterBuilder EnumerateSubstituter() => new SubstituterBuilder("enumerate", EnumerateSubstituter);

        private static IEnumerable<Expression> EnumerateSubstituter(XmlElement prototypeXml, ISubstituterBuilderScope scope)
        {
            var bindingExpression = prototypeXml.GetAttribute("enumeration");
            var enumeration = scope.GetSubstitutionBinding(bindingExpression);
            var getEnumeratorMethod = enumeration.Type.GetMethod(nameof(IEnumerable<object>.GetEnumerator));
            if (getEnumeratorMethod == null)
                throw new InvalidCastException($"Substitution expression '{bindingExpression}' of type '{enumeration.Type}' does not implement IEnumerable");
            var enumeratorParameter = Expression.Parameter(getEnumeratorMethod.ReturnType);
            yield return Expression.Assign(enumeratorParameter, Expression.Call(enumeration, getEnumeratorMethod));
            var breakLabel = Expression.Label();
            var enumeratedType = enumeration.Type.GenericTypeArguments[0];
            var eachScopedName = prototypeXml.GetAttribute("each");
            var currentParameter = Expression.Parameter(enumeratedType);
            var branchedScope = scope.BranchAndSet(eachScopedName, currentParameter);
            yield return Expression.Loop(Expression.Block(EnumerateSubstituterLoopBody(prototypeXml, scope, enumeratorParameter, currentParameter, breakLabel)));
            yield return Expression.Label(breakLabel);
        }

        private static IEnumerable<Expression> EnumerateSubstituterLoopBody(XmlElement prototypeXml, ISubstituterBuilderScope branchedScope, ParameterExpression enumeratorParameter, ParameterExpression currentParameter, LabelTarget breakLabel)
        {
            IEnumerator<int> a;
            var moveNextMethod = enumeratorParameter.Type.GetMethod(nameof(IEnumerator<object>.MoveNext))!;
            yield return Expression.IfThen(Expression.Not(Expression.Call(enumeratorParameter, moveNextMethod)), Expression.Break(breakLabel));
            var currentGetter = enumeratorParameter.Type.GetProperty(nameof(IEnumerator<object>.Current))!.GetAccessors()[0];
            yield return Expression.Assign(currentParameter, Expression.Call(enumeratorParameter, currentGetter));
            foreach (var childElement in prototypeXml.ChildNodes.OfType<XmlElement>())
            {
                var childBuilder = branchedScope.GetBuilderForTagName(childElement.Name);
                foreach (var expr in childBuilder.GetExpressions(childElement, branchedScope))
                    yield return expr;
            }
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
