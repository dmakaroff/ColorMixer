﻿using ReactiveUI;
using Splat;
using System.Reactive.Disposables;
using System.Windows.Media;

namespace ColorMixer.ViewModels
{
    public interface INodeViewModel : IReactiveObject, ISupportsActivation
    {
        double X { get; set; }
        double Y { get; set; }
        double Width { get; set; }
        double Height { get; set; }
        Color Color { get; set; }
        IConnectorViewModel Connector { get; }
    }

    public class NodeViewModel : ReactiveObject, INodeViewModel
    {
        private double x;
        private double y;
        private double width;
        private double height;
        private Color color;

        public NodeViewModel(IConnectorViewModel connector = null)
        {
            Connector = connector ?? Locator.Current.GetService<IConnectorViewModel>();

            Activator = new ViewModelActivator();

            this.WhenActivated(disposables =>
            {
                Disposable.Empty.DisposeWith(disposables);
            });
        }

        public ViewModelActivator Activator { get; private set; }

        public double X
        {
            get { return x; }
            set { this.RaiseAndSetIfChanged(ref x, value); }
        }

        public double Y
        {
            get { return y; }
            set { this.RaiseAndSetIfChanged(ref y, value); }
        }

        public double Width
        {
            get { return width; }
            set { this.RaiseAndSetIfChanged(ref width, value); }
        }

        public double Height
        {
            get { return height; }
            set { this.RaiseAndSetIfChanged(ref height, value); }
        }

        public Color Color
        {
            get { return color; }
            set { this.RaiseAndSetIfChanged(ref color, value); }
        }

        public IConnectorViewModel Connector { get; private set; }
    }
}