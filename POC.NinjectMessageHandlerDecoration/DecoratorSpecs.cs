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
            _handlers = definedTypes
                .Where(type => !type.Name.Contains("Decorator") 
                    && type.IsAssignableToGenericType(typeof(IMessageHandler<>)));

            _decorators = definedTypes
                .Where(type => type.Name.Contains("Decorator")
                    && type.IsAssignableToGenericType(typeof(IMessageHandler<>)));

            kernel.Bind<IMessageHandler<int>>().To<IntHandlerOne>().WhenParentNamed(1.ToString());
            kernel.Bind<IMessageHandler<int>>().To<IntHandlerTwo>().WhenParentNamed(2.ToString());
            kernel.Bind(typeof(IMessageHandler<>)).To(typeof(HandlerDecorator<>)).Named(1.ToString());
            kernel.Bind(typeof(IMessageHandler<>)).To(typeof(HandlerDecorator<>)).Named(2.ToString());
        };

        Because of = 
            () => result = kernel.GetAll<IMessageHandler<int>>();

        It should_have_three_handlers =
            () => _handlers.Should().HaveCount(2);

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
