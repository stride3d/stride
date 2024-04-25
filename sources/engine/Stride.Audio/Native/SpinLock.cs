using System.Threading;
namespace Stride.Audio;

public class SpinLock
{
    //private volatile int mLocked;

    public SpinLock()
    {
        //mLocked = 0;
    }

    public void Lock()
    {
        //while (Interlocked.Exchange(ref mLocked, 1) != 0) { }
    }

    public void Unlock()
    {
        //Interlocked.Exchange(ref mLocked, 0);
    }
}