﻿using ColorMixer.ViewModels;
using ColorMixer.Views;
using ReactiveUI;
using Splat;

namespace ColorMixer
{
    public interface IAppBootstrapper : IReactiveObject, IScreen
    {
    }

    public class AppBootstrapper : ReactiveObject, IAppBootstrapper
    {
        public AppBootstrapper(IMutableDependencyResolver resolver = null,
                               RoutingState router = null)
        {
            Router = router ?? new RoutingState();
            resolver = resolver ?? Locator.CurrentMutable;

            RegisterDependencies(resolver);

            Router.Navigate.Execute(resolver.GetService<IMixerViewModel>());
        }

        public RoutingState Router { get; private set; }

        private void RegisterDependencies(IMutableDependencyResolver resolver)
        {
            // Screen

            resolver.RegisterConstant(this,
                                      typeof(IScreen));
            // ViewModels

            resolver.RegisterConstant(new MixerViewModel(),
                                      typeof(IMixerViewModel));

            resolver.Register(() => new NodeViewModel(),
                                    typeof(INodeViewModel));

            resolver.Register(() => new ConnectorViewModel(),
                                    typeof(IConnectorViewModel));
            // Views

            resolver.RegisterConstant(new MixerView(),
                                      typeof(IViewFor<MixerViewModel>));

            resolver.Register(() => new NodeView(),
                                    typeof(IViewFor<NodeViewModel>));

            resolver.Register(() => new ConnectorView(),
                                    typeof(IViewFor<ConnectorViewModel>));
        }
    }
}