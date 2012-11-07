using System.Collections.Generic;
using System.Linq;
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
            kernel.Bind<IMessageHandler<int>>().To<IntHandlerOne>()
                .WhenInjectedInto(typeof(HandlerDecorator<>));
            kernel.Bind<IMessageHandler<int>>().To<IntHandlerTwo>()
                .WhenInjectedInto(typeof(HandlerDecorator<>));
            kernel.Bind(typeof(IMessageHandler<>)).To(typeof(HandlerDecorator<>));
        };

        Because of = 
            () => result = kernel.GetAll<IMessageHandler<int>>();

        It should_not_be_null = 
            () => result.Should().NotBeNull();

        It should_have_two_instances = 
            () => result.Should().HaveCount(2);

        It should_contain_instances_of_decorator_type =
            () => result.First().Should().BeOfType<HandlerDecorator<int>>();
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
}
