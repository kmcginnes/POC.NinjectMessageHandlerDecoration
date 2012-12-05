using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using FluentAssertions;
using Ninject;
using Xunit;

namespace POC.NinjectMessageHandlerDecoration
{
    public class DecoratorSpecs
    {
        private readonly StandardKernel _kernel;
        private readonly IEnumerable<TypeInfo> _handlers;
        private readonly IEnumerable<TypeInfo> _decorators;
        private readonly IEnumerable<IGrouping<Type, TypeInfo>> _grouping;

        public DecoratorSpecs()
        {
            _kernel = new StandardKernel();

            var definedTypes = Assembly.GetExecutingAssembly().DefinedTypes;
            var handlerType = typeof (IHandle<>);
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
                    _kernel.Bind(handlerType.MakeGenericType(group.Key))
                          .To(handlerTypeInfo)
                          .WhenParentNamed(i.ToString());
                    _kernel.Bind(handlerType.MakeGenericType(group.Key))
                           .To(typeof (HandlerDecorator<>).MakeGenericType(group.Key))
                           .Named(i.ToString());
                }
            }

            _decorators = definedTypes
                .Where(type => type.Name.Contains("Decorator")
                    && type.IsAssignableToGenericType(handlerType));
        }

        [Fact]
        public void should_have_max_count_of_handlers_per_group_of_two()
        {
            _grouping.Max(g => g.Count()).Should().Be(2);
        }

        [Fact]
        public void should_have_two_groups_of_handler_types()
        {
            _grouping.Count().Should().Be(2);
        }

        [Fact]
        public void should_have_two_handlers_in_int_group()
        {
            _grouping.Single(g => g.Key == typeof (int)).Count().Should().Be(2);
        }

        [Fact]
        public void should_have_one_handler_in_string_group()
        {
            _grouping.Single(g => g.Key == typeof (string)).Count().Should().Be(1);
        }

        [Fact]
        public void should_have_three_total_handlers()
        {
            _handlers.Should().HaveCount(3);
        }

        [Fact]
        public void should_have_one_decorator()
        {
            _decorators.Should().HaveCount(1);
        }

        [Fact]
        public void should_get_decorated_int_handlers_from_ninject()
        {
            var handlers = _kernel.GetAll<IHandle<int>>().ToArray();
            handlers.Should().NotBeNull();
            handlers.Should().HaveCount(2);
            handlers.First().Should().BeOfType<HandlerDecorator<int>>();
            handlers.Cast<HandlerDecorator<int>>().First().Handler.Should().BeOfType<IntHandlerOne>();
        }

        [Fact]
        public void should_get_decorated_string_handlers_from_ninject()
        {
            var handlers = _kernel.GetAll<IHandle<string>>().ToArray();
            handlers.Should().NotBeNull();
            handlers.Should().HaveCount(1);
            handlers.First().Should().BeOfType<HandlerDecorator<string>>();
            handlers.Cast<HandlerDecorator<string>>().First().Handler.Should().BeOfType<StringHandler>();
        }
    }

    public interface IHandle<T> { void Handle(T message); }

    public class HandlerDecorator<T> : IHandle<T>
    {
        public readonly IHandle<T> Handler;

        public HandlerDecorator(IHandle<T> handler)
        {
            Handler = handler;
        }

        public void Handle(T message)
        {
            Handler.Handle(message);
        }
    }

    public class IntHandlerOne : IHandle<int>
    {
        public void Handle(int message) { }
    }
    public class IntHandlerTwo : IHandle<int>
    {
        public void Handle(int message) { }
    }
    public class StringHandler : IHandle<string>
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
