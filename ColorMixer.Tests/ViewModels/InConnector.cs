﻿using ColorMixer.Model;
using ColorMixer.Services;
using ColorMixer.Tests.Attributes;
using ColorMixer.ViewModels;
using FluentAssertions;
using Ninject;
using NSubstitute;
using Ploeh.AutoFixture.Xunit2;
using ReactiveUI;
using System.Reactive.Linq;
using Xunit;

namespace ViewModels
{
    public class InConnector
    {
        private readonly IKernel kernel;

        private readonly IInteractionService interactions;
        private readonly IMixerViewModel mixer;
        private readonly INode node;

        public InConnector()
        {
            kernel = new StandardKernel();

            interactions = new InteractionService();
            mixer = Substitute.For<IMixerViewModel, ReactiveObject>();
            node = Substitute.For<INode>();

            kernel.Bind<IInteractionService>()
                  .ToConstant(interactions);

            kernel.Bind<IMixerViewModel>()
                  .ToConstant(mixer);

            kernel.Bind<IInConnectorViewModel>()
                  .To<InConnectorViewModel>()
                  .WithPropertyValue(nameof(IInConnectorViewModel.Node),
                                     node); // system under test

            mixer.ConnectingConnector = Arg.Do<IConnector>(
                _ => mixer.RaisePropertyChanged(nameof(mixer.ConnectingConnector)));
        }

        [Fact]
        public void InDirection()
            => kernel.Get<IInConnectorViewModel>()
                     .Direction.Should().Be(ConnectorDirection.Input);

        [Theory]
        [InlineAutoNSubstituteData(true, ConnectorDirection.Output)]
        [InlineAutoNSubstituteData(false, ConnectorDirection.Input)]
        public async void Disabled_WhenNotConnectableTo(bool expected,
                                                        ConnectorDirection connectingDirection,
                                                        INode connectingNode)
        {
            // Arrange

            var connectingConnector = Substitute.For<IConnector>();

            connectingConnector.Direction.Returns(connectingDirection);
            connectingConnector.Node.Returns(connectingNode);

            // Act

            var connector = kernel.Get<IInConnectorViewModel>();

            connector.Activator
                     .Activate();

            mixer.ConnectingConnector = connectingConnector;

            var actual = await connector.WhenAnyValue(vm => vm.IsEnabled)
                                        .FirstAsync();
            // Assert

            actual.Should().Be(expected);
        }

        [Theory]
        [InlineAutoData(ConnectorDirection.Output)]
        [InlineAutoData(ConnectorDirection.Input)]
        public async void Disabled_WhenConnectingSource(ConnectorDirection connectingDirection)
        {
            // Arrange

            var connectingConnector = Substitute.For<IConnector>();

            connectingConnector.Direction.Returns(connectingDirection);
            connectingConnector.Node.Returns(node);

            // Act

            var connector = kernel.Get<IInConnectorViewModel>();

            connector.Activator
                     .Activate();

            mixer.ConnectingConnector = connectingConnector;

            var isEnabled = await connector.WhenAnyValue(vm => vm.IsEnabled)
                                           .FirstAsync();
            // Assert

            isEnabled.Should().BeFalse();
        }

        [Fact]
        public async void Enabled_WhenNotConnecting()
        {
            // Act

            var connector = kernel.Get<IInConnectorViewModel>();

            connector.Activator
                     .Activate();

            mixer.ConnectingConnector = null;

            var isEnabled = await connector.WhenAnyValue(vm => vm.IsEnabled)
                                           .FirstAsync();
            // Assert

            isEnabled.Should().BeTrue();
        }

        [Theory]
        [InlineAutoNSubstituteData(true)]
        [InlineData(false, default(IOutConnectorViewModel))]
        public async void SetsIsConnected(bool expected, IOutConnectorViewModel connectedTo)
        {
            // Act

            var connector = kernel.Get<IInConnectorViewModel>();

            connector.Activator
                     .Activate();

            connector.ConnectedTo = connectedTo;

            var isConnected = await connector.WhenAnyValue(vm => vm.IsConnected)
                                             .FirstAsync();
            // Assert

            isConnected.Should().Be(expected);
        }

        [Fact]
        public async void ConnectorCommand_InvokesGetOutConnectorInteraction()
        {
            // Arrange

            var input = default(IInConnectorViewModel);
            var output = Substitute.For<IOutConnectorViewModel>();

            interactions.GetOutConnector
                        .RegisterHandler(i =>
                        {
                            input = i.Input;
                            i.SetOutput(output);
                        });
            // Act

            var connector = kernel.Get<IInConnectorViewModel>();

            connector.Activator
                     .Activate();

            await connector.ConnectorCommand
                           .Execute();

            // Assert

            input.Should().Be(connector);

            connector.ConnectedTo.Should().Be(output);
        }

        [Theory]
        [InlineData(true, false)]
        [InlineData(false, true)]
        public async void ConnectorCommand_CanExecute(bool expected, bool isNodeBeingAdded)
        {
            // Arrange

            mixer.IsNodeBeingAdded
                 .Returns(isNodeBeingAdded);

            // Act

            var connector = kernel.Get<IInConnectorViewModel>();

            connector.Activator
                     .Activate();

            var actual = await connector.ConnectorCommand
                                        .CanExecute
                                        .FirstAsync();
            // Assert

            actual.Should().Be(expected);
        }
    }
}