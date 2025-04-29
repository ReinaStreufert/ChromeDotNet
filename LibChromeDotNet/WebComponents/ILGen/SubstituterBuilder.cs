using LibChromeDotNet.ChromeInterop;
using LibChromeDotNet.HTML5.DOM;
using LibChromeDotNet.HTML5.JS;
using System;
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
            ISubstitution? innerSubstitute = null;
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
                var innerSubstituteExpr = innerSubstitute.ValueExpression;
                var toStringMethod = innerSubstituteExpr.Type.GetMethod(nameof(object.ToString))!;
                var xmlInnerTextSetter = typeof(XmlElement).GetProperty(nameof(XmlElement.InnerText))!.GetAccessors()
                    .Where(m => m.ReturnType == typeof(void))
                    .FirstOrDefault()!;
                yield return Expression.Call(elementParameter, xmlInnerTextSetter, Expression.Call(innerSubstituteExpr, toStringMethod));
                if (innerSubstitute.ResourceTreeTip != null)
                {
                    var domNodeParameter = Expression.Parameter(typeof(IDOMNode));
                    var domSetInnerTextMethod = typeof(DOMExtensions).GetMethod(nameof(DOMExtensions.SetInnerTextAsync))!;
                    var innerSubstituteBindingChange = Expression.Lambda(
                        Expression.Call(domSetInnerTextMethod, domNodeParameter, Expression.Call(innerSubstituteExpr, toStringMethod)), domNodeParameter);
                    foreach (var expr in OnChangeHandler(scope, nodeId, innerSubstitute.ResourceTreeTip, innerSubstitute.ValueExpression, innerSubstituteBindingChange))
                        yield return expr;
                }
            }
            var xmlSetAttributeMethod = typeof(XmlElement).GetMethod(nameof(XmlElement.SetAttribute), new Type[] { typeof(string), typeof(string) })!;
            var domSetAttrubuteMethod = typeof(IDOMNode).GetMethod(nameof(IDOMNode.SetAttributeAsync))!;
            for (int i = 0; i < prototypeXml.Attributes.Count; i++)
            {
                var attribute = prototypeXml.Attributes[i];
                if (!attribute.Value.StartsWith("$:"))
                    continue;
                var attrSubstitute = scope.GetSubstitutionBinding(attribute.Value.Substring(2));
                var attrSubstituteExpr = attrSubstitute.ValueExpression;
                var toStringMethod = attrSubstituteExpr.Type.GetMethod(nameof(object.ToString))!;
                yield return Expression.Call(
                    elementParameter,
                    xmlSetAttributeMethod,
                    Expression.Constant(attribute.Name),
                    Expression.Call(attrSubstituteExpr, toStringMethod));
                if (attrSubstitute.ResourceTreeTip != null)
                {
                    var domNodeParameter = Expression.Parameter(typeof(IDOMNode));
                    var attrSubstituteBindingChange = Expression.Lambda(
                        Expression.Call(domNodeParameter,
                        domSetAttrubuteMethod,
                        Expression.Constant(attribute.Name),
                        Expression.Call(attrSubstituteExpr, toStringMethod)));
                    foreach (var expr in OnChangeHandler(scope, nodeId, attrSubstitute.ResourceTreeTip, attrSubstituteExpr, attrSubstituteBindingChange))
                        yield return expr;
                }
            }
            foreach (var childElement in prototypeXml.ChildNodes.OfType<XmlElement>())
            {
                var childBuilder = scope.GetBuilderForTagName(childElement.Name);
                foreach (var expr in childBuilder.GetExpressions(childElement, scope.Branch(elementParameter, nodeId)))
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
            var enumerationExpr = enumeration.ValueExpression;
            var getEnumeratorMethod = enumerationExpr.Type.GetMethod(nameof(IEnumerable<object>.GetEnumerator));
            if (getEnumeratorMethod == null)
                throw new InvalidCastException($"Substitution expression '{bindingExpression}' of type '{enumerationExpr.Type}' does not implement IEnumerable");
            var enumeratorParameter = Expression.Parameter(getEnumeratorMethod.ReturnType);
            yield return Expression.Assign(enumeratorParameter, Expression.Call(enumerationExpr, getEnumeratorMethod));
            var breakLabel = Expression.Label();
            var enumeratedType = enumerationExpr.Type.GenericTypeArguments[0];
            var eachScopedName = prototypeXml.GetAttribute("each");
            var currentParameter = Expression.Parameter(enumeratedType);
            var branchedScope = scope.BranchAndSet(eachScopedName, currentParameter);
            yield return Expression.Loop(Expression.Block(EnumerateSubstituterLoopBody(prototypeXml, scope, enumeratorParameter, currentParameter, breakLabel)));
            yield return Expression.Label(breakLabel);
            if (enumeration.ResourceTreeTip != null)
            {
                foreach (var expr in OnChangeRefresh(scope, enumeration.ResourceTreeTip, enumerationExpr))
                    yield return expr;
            }
        }

        private static IEnumerable<Expression> EnumerateSubstituterLoopBody(XmlElement prototypeXml, ISubstituterBuilderScope branchedScope, ParameterExpression enumeratorParameter, ParameterExpression currentParameter, LabelTarget breakLabel)
        {
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

        public static ISubstituterBuilder ConditionalSubstituter() => new SubstituterBuilder("conditional", ConditionalSubstituter);

        private static IEnumerable<Expression> ConditionalSubstituter(XmlElement prototypeXml, ISubstituterBuilderScope scope)
        {
            ISubstitution condition;
            Expression conditionExpr;
            if (prototypeXml.HasAttribute("when"))
            {
                condition = scope.GetSubstitutionBinding(prototypeXml.GetAttribute("when"));
                conditionExpr = condition.ValueExpression;
            } else
            {
                condition = scope.GetSubstitutionBinding(prototypeXml.GetAttribute("unless"));
                conditionExpr = Expression.Not(condition.ValueExpression);
            }
            yield return Expression.IfThen(conditionExpr, Expression.Block(ConditionalSubstituteIfBody(prototypeXml, scope)));
            if (condition.ResourceTreeTip != null)
            {
                foreach (var expr in OnChangeRefresh(scope, condition.ResourceTreeTip, conditionExpr))
                    yield return expr;
            }
        }

        private static IEnumerable<Expression> ConditionalSubstituteIfBody(XmlElement prototypeXml, ISubstituterBuilderScope scope)
        {
            foreach (var childElement in prototypeXml.ChildNodes.OfType<XmlElement>())
            {
                var childBuilder = scope.GetBuilderForTagName(childElement.Name);
                foreach (var expr in childBuilder.GetExpressions(childElement, scope))
                    yield return expr;
            }
        }

        private static IEnumerable<Expression> OnChangeRefresh(ISubstituterBuilderScope scope, Expression componentResource, Expression property)
        {
            var requestRerenderMethod = typeof(IComponentRenderContext).GetMethod(nameof(IComponentRenderContext.RequestRerender))!;
            var refreshHandler = Expression.Lambda(
                Expression.Call(scope.ComponentRenderContext, requestRerenderMethod));
            var propChangeListenerType = typeof(PropertyChangeListener<,>).MakeGenericType(componentResource.Type, property.Type);
            var createChangeListener = propChangeListenerType.GetMethod(nameof(PropertyChangeListener<IComponentResource, object>.Create))!;
            var validateLeftProp = Expression.Parameter(property.Type);
            var validateRightProp = Expression.Parameter(property.Type);
            yield return Expression.Call(
                createChangeListener,
                componentResource,
                Expression.Lambda(Expression.Block(OnChangeHandlerGetPropBody(property))),
                Expression.Lambda(Expression.Block(OnChangeHandlerValidateBody(validateLeftProp, validateRightProp)), validateLeftProp, validateRightProp),
                refreshHandler);
        }

        private static IEnumerable<Expression> OnChangeHandler(ISubstituterBuilderScope scope, string elementId, Expression componentResource, Expression property, LambdaExpression onChange)
        {
            var addDOMActionMethod = typeof(IComponentRenderContext).GetMethod(nameof(IComponentRenderContext.AddDOMAction))!;
            var domNodeParameter = Expression.Parameter(typeof(IDOMNode));
            yield return Expression.Call(
                scope.ComponentRenderContext,
                addDOMActionMethod,
                Expression.Lambda(Expression.Block(OnChangeHandlerDOMActionBody(componentResource, property, domNodeParameter, onChange))), domNodeParameter);
        }

        private static IEnumerable<Expression> OnChangeHandlerDOMActionBody(Expression componentResource, Expression property, ParameterExpression domNodeParameter, LambdaExpression onChange)
        {
            var propChangeListenerType = typeof(PropertyChangeListener<,>).MakeGenericType(componentResource.Type, property.Type);
            var createChangeListener = propChangeListenerType.GetMethod(nameof(PropertyChangeListener<IComponentResource, object>.Create))!;
            var validateLeftProp = Expression.Parameter(property.Type);
            var validateRightProp = Expression.Parameter(property.Type);
            yield return Expression.Call(
                createChangeListener,
                componentResource,
                Expression.Lambda(Expression.Block(OnChangeHandlerGetPropBody(property))),
                Expression.Lambda(Expression.Block(OnChangeHandlerValidateBody(validateLeftProp, validateRightProp)), validateLeftProp, validateRightProp),
                Expression.Lambda(Expression.Invoke(onChange, domNodeParameter)));
        }

        private static IEnumerable<Expression> OnChangeHandlerGetPropBody(Expression property)
        {
            var returnLabel = Expression.Label(property.Type);
            yield return Expression.Return(returnLabel, property);
            yield return Expression.Label(returnLabel);
        }

        private static IEnumerable<Expression> OnChangeHandlerValidateBody(ParameterExpression leftProp, ParameterExpression rightProp)
        {
            var returnLabel = Expression.Label(typeof(bool));
            yield return Expression.Return(returnLabel, Expression.Equal(leftProp, rightProp));
            yield return Expression.Label(returnLabel);
        }

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

        public bool IsMatchFor(string name)
        {
            return _Name == null || name == _Name;
        }
    }
}
