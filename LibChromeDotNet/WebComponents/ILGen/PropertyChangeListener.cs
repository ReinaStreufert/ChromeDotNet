using LibChromeDotNet.ChromeInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibChromeDotNet.WebComponents.ILGen
{
    public class PropertyChangeListener<TResource, TProperty> where TResource : IComponentResource
    {
        public static PropertyChangeListener<TResource, TProperty> Create(TResource resource, Func<TProperty> getProperty, Func<TProperty, TProperty, bool> validateProperty, Action<TProperty> onValueChanged)
        {
            // validateProperty should return true if the arguments are the same. == operator is unsafe to use on generic parameters
            var listener = new PropertyChangeListener<TResource, TProperty>(getProperty());
            var sync = new object();
            resource.PropertyChanged += () =>
            {
                TProperty newValue;
                TProperty oldValue;
                lock (sync)
                {
                    newValue = getProperty();
                    oldValue = listener._LastValue;
                    listener._LastValue = newValue;
                }
                if (!validateProperty(oldValue, newValue))
                    onValueChanged(newValue);
            };
            return listener;
        }

        private PropertyChangeListener(TProperty lastValue)
        {
            _LastValue = lastValue;
        }

        private TProperty _LastValue;
    }
}
