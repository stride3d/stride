using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Stride.Metrics.ServerApp.Models
{
    public abstract class AggregateBase
    {
        public int Count { get; set; }
    }
}