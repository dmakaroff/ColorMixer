﻿using ColorMixer.Extensions;
using ColorMixer.Model;
using ColorMixer.Services;
using ReactiveUI;
using Splat;
using System;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace ColorMixer.ViewModels
{
    public interface IMixerViewModel : IReactiveObject, IRoutableViewModel, ISupportsActivation
    {
        bool IsNodeBeingAdded { get; }
        IObservable<KeyEventArgs> MainWindowKeyDown { get; }
        IReadOnlyReactiveList<INode> Nodes { get; }
        IReadOnlyReactiveList<IConnectionViewModel> Connections { get; }
        ReactiveCommand<Unit, Unit> AddColorNodeCommand { get; }
        ReactiveCommand<Unit, Unit> AddOperationNodeCommand { get; }
        ReactiveCommand<Unit, Unit> AddResultNodeCommand { get; }
        IConnector ConnectingConnector { get; set; }
        IConnector ConnectedConnector { get; set; }
    }

    public class MixerViewModel : ReactiveObject, IMixerViewModel
    {
        private readonly IInteractionService interactions;
        private readonly IMainWindowViewModel mainWindow;

        private readonly ReactiveList<INode> nodes;
        private readonly ReactiveList<IConnectionViewModel> connections;

        private ObservableAsPropertyHelper<bool> isNodeBeingAdded;

        private IConnector connectingConnector;
        private IConnector connectedConnector;

        public MixerViewModel(IInteractionService interactions = null,
                              IMainWindowViewModel mainWindow = null)
        {
            this.interactions = interactions ?? Locator.Current.GetService<IInteractionService>();
            this.mainWindow = mainWindow ?? Locator.Current.GetService<IMainWindowViewModel>();

            HostScreen = this.mainWindow;

            nodes = new ReactiveList<INode>();
            connections = new ReactiveList<IConnectionViewModel>();

            this.WhenActivated(disposables =>
            {
                this
                    .interactions
                    .GetInConnector
                    .RegisterHandler(i => HandleConnectionRequest(i))
                    .DisposeWith(disposables);

                this
                    .interactions
                    .GetOutConnector
                    .RegisterHandler(i => HandleConnectionRequest(i))
                    .DisposeWith(disposables);

                this
                    .interactions
                    .DeleteNode
                    .RegisterHandler(i => HandleDeletionRequest(i))
                    .DisposeWith(disposables);

                AddColorNodeCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var point = await this.interactions
                                          .GetNewNodePoint
                                          .Handle(Unit.Default);

                    if (!point.HasValue) // user cancelled point selection
                    {
                        return;
                    }

                    var node = Locator.Current.GetService<IColorNodeViewModel>();

                    node.X = point.Value.X;
                    node.Y = point.Value.Y;

                    nodes.Add(node);
                })
                .DisposeWith(disposables);

                AddOperationNodeCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var point = await this.interactions
                                          .GetNewNodePoint
                                          .Handle(Unit.Default);

                    if (!point.HasValue) // user cancelled point selection
                    {
                        return;
                    }

                    var node = Locator.Current.GetService<IOperationNodeViewModel>();

                    node.X = point.Value.X;
                    node.Y = point.Value.Y;

                    nodes.Add(node);
                })
                .DisposeWith(disposables);

                AddResultNodeCommand = ReactiveCommand.CreateFromTask(async () =>
                {
                    var point = await this.interactions
                                          .GetNewNodePoint
                                          .Handle(Unit.Default);

                    if (!point.HasValue) // user cancelled point selection
                    {
                        return;
                    }

                    var node = Locator.Current.GetService<IResultNodeViewModel>();

                    node.X = point.Value.X;
                    node.Y = point.Value.Y;

                    nodes.Add(node);
                })
                .DisposeWith(disposables);

                isNodeBeingAdded = this
                    .WhenAnyObservable(vm => vm.AddColorNodeCommand.IsExecuting,
                                       vm => vm.AddOperationNodeCommand.IsExecuting,
                                       vm => vm.AddResultNodeCommand.IsExecuting,
                                       (a, b, c) => a || b || c)
                    .ToProperty(this, vm => vm.IsNodeBeingAdded)
                    .DisposeWith(disposables);
            });
        }

        public ViewModelActivator Activator { get; } = new ViewModelActivator();

        public IScreen HostScreen { get; private set; }

        public string UrlPathSegment => "Mixer";

        public bool IsNodeBeingAdded => isNodeBeingAdded.Value;

        public IObservable<KeyEventArgs> MainWindowKeyDown => mainWindow.KeyDown;

        public IReadOnlyReactiveList<INode> Nodes => nodes;

        public IReadOnlyReactiveList<IConnectionViewModel> Connections => connections;

        public ReactiveCommand<Unit, Unit> AddColorNodeCommand { get; private set; }

        public ReactiveCommand<Unit, Unit> AddOperationNodeCommand { get; private set; }

        public ReactiveCommand<Unit, Unit> AddResultNodeCommand { get; private set; }

        public IConnector ConnectingConnector
        {
            get { return connectingConnector; }
            set { this.RaiseAndSetIfChanged(ref connectingConnector, value); }
        }

        public IConnector ConnectedConnector
        {
            get { return connectedConnector; }
            set { this.RaiseAndSetIfChanged(ref connectedConnector, value); }
        }

        private async Task HandleConnectionRequest<TSrc, TDst>(
            InteractionContext<TSrc, TDst> interaction) where TDst : class, IConnector
                                                        where TSrc : class, IConnector
        {
            var connector = interaction.Input;

            if (connector.IsConnected) // remove previous connections if connector is connected
            {
                var connected = connections.Where(c => c.To == connector || c.From == connector);

                foreach (var connection in connected)
                {
                    connection.To.ConnectedTo = null;
                    connection.From.ConnectedTo = null;
                }

                connections.RemoveRange(connected);
            }

            if (ConnectingConnector == null) // connection initiated
            {
                ConnectingConnector = interaction.Input;

                // Wait for either ConnectedConnector to get the connector value or
                // ConnectingConnector to become null indicating connection was cancelled

                var sequence = Observable.Merge(this.WhenAnyValue(vm => vm.ConnectedConnector)
                                                    .Skip(1), // ignore initial value
                                                this.WhenAnyValue(vm => vm.ConnectingConnector)
                                                    .Skip(1) // ignore initial value
                                                    .Where(v => v == null));

                var secondConnector = await sequence.FirstAsync();

                interaction.SetOutput(secondConnector as TDst);
            }
            else // the second connector got set
            {
                ConnectedConnector = interaction.Input;
                interaction.SetOutput(ConnectingConnector as TDst);

                var connection = Locator.Current.GetService<IConnectionViewModel>();

                if (ConnectingConnector.Direction == ConnectorDirection.Input &&
                    ConnectedConnector.Direction == ConnectorDirection.Output)
                {
                    connection.From = ConnectedConnector as IOutConnectorViewModel;
                    connection.To = ConnectingConnector as IInConnectorViewModel;
                }
                else if (ConnectingConnector.Direction == ConnectorDirection.Output &&
                         ConnectedConnector.Direction == ConnectorDirection.Input)
                {
                    connection.From = ConnectingConnector as IOutConnectorViewModel;
                    connection.To = ConnectedConnector as IInConnectorViewModel;
                }

                if (connection.From != null &&
                    connection.To != null)
                {
                    connections.Add(connection);
                }

                ConnectingConnector = null;
                ConnectedConnector = null;
            }
        }

        private void HandleDeletionRequest(InteractionContext<INode, Unit> interaction)
        {
            var node = interaction.Input;
            var connected = connections.Where(c => c.To.Node == node || c.From.Node == node);

            foreach (var connection in connected)
            {
                connection.To.ConnectedTo = null;
                connection.From.ConnectedTo = null;
            }

            connections.RemoveRange(connected);
            nodes.Remove(node);

            interaction.SetOutput(Unit.Default);
        }
    }
}