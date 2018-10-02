using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Xenko.Metrics.ServerApp.Models
{
    public abstract class AggregateBase
    {
        public int Count { get; set; }
    }
}