using System;
using System.Collections.Generic;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Windows;
using NodeNetwork.ViewModels;
using NodeNetwork.Views;
using ReactiveUI;

namespace NodeNetwork.Toolkit.ValueNode
{
    /// <summary>
    /// A viewmodel for a node output that produces a value based on the inputs.
    /// </summary>
    /// <typeparam name="T">The type of object produced by this output.</typeparam>
    public class ValueNodeOutputViewModel<T> : NodeOutputViewModel
    {
        static ValueNodeOutputViewModel()
        {
            NNViewRegistrar.AddRegistration(() => new NodeOutputView(), typeof(IViewFor<ValueNodeOutputViewModel<T>>));
        }
        
        #region Value
        /// <summary>
        /// Observable that produces the value every time it changes.
        /// </summary>
        public OutputSource<T> Value
        {
            get => _value;
            set => this.RaiseAndSetIfChanged(ref _value, value);
        }
        private OutputSource<T> _value;
        #endregion

        #region CurrentValue
        /// <summary>
        /// The latest value produced by this output.
        /// </summary>
        public T CurrentValue => _currentValue.Value;
        private readonly ObservableAsPropertyHelper<T> _currentValue;
        #endregion
        public IObservable<T> ValueChanged;
        public string TypeName { get; set; }
        public ValueNodeOutputViewModel()
        {
            TypeName = typeof(T).FullName;
            Value = new OutputSource<T>();
            ValueChanged = this.WhenAnyObservable(vm => vm.Value);
            ValueChanged.ToProperty(this, vm => vm.CurrentValue, out _currentValue, false, Scheduler.Immediate);
            var localValues = this.WhenAnyValue(vm => vm.Editor)
                .Select(e =>
                {

                    if ((e is OutputValueEditorViewModel<T>))
                    {
                        var editor = e as OutputValueEditorViewModel<T>;
                        return editor;


                    }
                    else
                    {
                        return null;
                    }

                });
            var connect = localValues.Do((x) =>
            {
                if (x != null)
                {
                    x.SetViewSouce(this);

                }

            });
            connect.Publish().Connect();
            localValues.Publish().Connect();



            //this.WhenAnyValue(vm => vm.Editor).Select(editor =>
            //{
            //    if (Editor != null)
            //    {
            //        Value.Subscribe((e) => ((
            //        ValueEditorViewModel<T>)Editor).Value = e);
            //    }
            //    return null;
            //});


            //Value.sub
        }
        public void OnNext(T data)
        {
            IsUpdated = true;
            Value.OnNext(data);
            
        }
    }
    public class OutputSource<T>:IObservable<T>
    {
        public List<IObserver<T>> obser = new List<IObserver<T>>();
        object obserlock = new object();
        public IDisposable Subscribe(IObserver<T> observer)
        {
            lock (obserlock)
            {
                obser.Add(observer);
            }

            return new Unsubscriber<T>(obser, observer);
        }
        public T Data;
        public void OnNext(T data)
        {
            Data = data;
            lock (obserlock)
            {
                foreach (var item in obser)
                {
                    
                    item.OnNext(data);
                    
                }
            }
        }
    }
    public class Unsubscriber<T> : IDisposable
    {
        private List<IObserver<T>> _observers;
        private IObserver<T> _observer;

        public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
        {
            this._observers = observers;
            this._observer = observer;
        }

        public void Dispose()
        {
            if (_observer != null && _observers.Contains(_observer))
                _observers.Remove(_observer);
        }
    }
}
