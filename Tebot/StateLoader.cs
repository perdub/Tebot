using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace Tebot
{

    public abstract class StateLoader
    {
        public LoaderStrategy Strategy { get; set; }

        public
#if NETSTANDARD2_0
            Func<IEnumerable<long>>
#else
            Func<IEnumerable<long>>? 
#endif
            loaderDelegate = null;

        internal abstract object Load();
        internal abstract IEnumerable<(long, Base)> asTuptes();
        public static StateLoader<Base> Empty()
        {
            return new StateLoader<Base>(null)
            {
                Strategy = LoaderStrategy.None,
                loaderDelegate = () => { return Array.Empty<long>(); }
            };
        }
    }

    public class StateLoader<T>
#if NETSTANDARD2_0
        
        #else
        (IServiceProvider? serviceProvider)
#endif
        : StateLoader where T : Base
    {
        #if NETSTANDARD2_0
        private IServiceProvider serviceProvider;
        public StateLoader(IServiceProvider serv)
        {
            serviceProvider = serv;   
        }
        #endif
        internal IEnumerable<T> LoadTyped()
        {
            return (IEnumerable<T>)Load();
        }
        internal override object Load()
        {
            if (loaderDelegate is null)
            {
                throw new NullReferenceException("Func<> delegate is null.");
            }
            if (serviceProvider is null)
            {
                throw new NullReferenceException("serviceProvider is null.");
            }
            return loaderDelegate()
                .Select((id) =>
                {
                    T inst = ActivatorUtilities.CreateInstance<T>(serviceProvider);
                    inst.UserId = id;
                    inst.IsLoaded = true;
                    inst.OnLoad(id);
                    return inst;
                });
        }
        internal override IEnumerable<(long, Base)> asTuptes()
        {
            return LoadTyped().Select(item => (item.UserId, (Base)item));
        }
    }

    public enum LoaderStrategy
    {
        None,
        AllSync
    }
}