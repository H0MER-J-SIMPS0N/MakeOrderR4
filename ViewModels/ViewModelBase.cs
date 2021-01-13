using System;
using System.Collections.Generic;
using System.Text;
using ReactiveUI;

namespace MakeOrderR4v2.ViewModels
{
    public class ViewModelBase : ReactiveObject
    {
        #region Fields and Properties
        public virtual string Name { get; set; }
        public virtual bool IsSelected { get; set; }
        #endregion
    }
}
