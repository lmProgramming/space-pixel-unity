using System;

namespace Core.MVCVM
{
    public abstract class ObservableModel
    {
        public event Action Changed;

        protected void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}