using System;

namespace Irony.Parsing.Construction {
#if SILVERLIGHT
  //Less precise version for Silverlight
  internal class Stopwatch {
    long _startTime, _endTime;
    public void Start() {
      _startTime = Environment.TickCount;
    }
    public void Stop() {
      _endTime = Environment.TickCount;
    }
    public long ElapsedMilliseconds {
      get {return _endTime - _startTime; }
    }
  }
#endif

}
