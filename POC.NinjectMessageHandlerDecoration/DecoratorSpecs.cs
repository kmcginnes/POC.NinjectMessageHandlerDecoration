using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Machine.Specifications;
using Ninject;

namespace POC.NinjectMessageHandlerDecoration
{
    public class DecoratorSpecs
    {
        static StandardKernel kernel;
        static IEnumerable<IMessageHandler<int>> result;

        Establish context = () =>
        {
            kernel = new StandardKernel();

            var definedTypes = Assembly.GetExecutingAssembly().DefinedTypes;
            var handlerType = typeof (IMessageHandler<>);
            _handlers = definedTypes
                .Where(type => !type.Name.Contains("Decorator") 
                    && type.IsAssignableToGenericType(handlerType));

            _grouping = _handlers
                .GroupBy(h => h.GetInterfaces()
                    .First(i => i.IsGenericType && i.GetGenericTypeDefinition() == handlerType)
                    .GetGenericArguments().First());

            foreach (var group in _grouping)
            {
                for (int i = 0; i < group.Count(); i++)
                {
                    var handlerTypeInfo = group.ElementAt(i);
                    kernel.Bind(handlerType.MakeGenericType(group.Key))
                          .To(handlerTypeInfo)
                          .WhenParentNamed(i.ToString());
                }
            }

            for (int i = 0; i < _grouping.Max(g => g.Count()); i++)
            {
                kernel.Bind(handlerType).To(typeof(HandlerDecorator<>)).Named(i.ToString());
            }

            _decorators = definedTypes
                .Where(type => type.Name.Contains("Decorator")
                    && type.IsAssignableToGenericType(handlerType));
        };

        Because of = 
            () => result = kernel.GetAll<IMessageHandler<int>>();

        It should_have_two_named_decorators =
            () => _grouping.Max(g => g.Count()).Should().Be(2);

        It should_have_two_groupings =
            () => _grouping.Count().Should().Be(2);

        It should_have_groupings_of_int_with_count_of_two =
            () => _grouping.Single(g => g.Key == typeof(int)).Count().Should().Be(2);

        It should_have_groupings_of_string_with_count_of_one =
            () => _grouping.Single(g => g.Key == typeof(string)).Count().Should().Be(1);

        It should_have_three_handlers =
            () => _handlers.Should().HaveCount(3);

        It should_have_one_decorator =
            () => _decorators.Should().HaveCount(1);

        It should_not_be_null = 
            () => result.Should().NotBeNull();

        It should_have_two_instances =
            () => result.Should().HaveCount(2);

        It should_contain_instances_of_decorator_type =
            () => result.First().Should().BeOfType<HandlerDecorator<int>>();

        private static IEnumerable<TypeInfo> _handlers;
        private static IEnumerable<TypeInfo> _decorators;
        private static IEnumerable<IGrouping<Type, TypeInfo>> _grouping;
    }

    public interface IMessageHandler<T> { void Handle(T message); }

    public class HandlerDecorator<T> : IMessageHandler<T>
    {
        private readonly IMessageHandler<T> _handler;

        public HandlerDecorator(IMessageHandler<T> handler)
        {
            _handler = handler;
        }

        public void Handle(T message)
        {
            _handler.Handle(message);
        }
    }

    public class IntHandlerOne : IMessageHandler<int>
    {
        public void Handle(int message) { }
    }
    public class IntHandlerTwo : IMessageHandler<int>
    {
        public void Handle(int message) { }
    }
    public class StringHandler : IMessageHandler<string>
    {
        public void Handle(string message) { }
    }

    public static class TypeExtensions
    {
        public static bool IsAssignableToGenericType(this Type givenType, Type genericType)
        {
            if (givenType.GetInterfaces()
                .Where(it => it.IsGenericType)
                .Any(it => it.GetGenericTypeDefinition() == genericType))
            {
                return true;
            }

            var baseType = givenType.BaseType;
            if (baseType == null)
            {
                return false;
            }

            return baseType.IsGenericType &&
                   baseType.GetGenericTypeDefinition() == genericType ||
                   IsAssignableToGenericType(baseType, genericType);
        }
    }
}
