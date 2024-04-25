using Silk.NET.OpenAL;

namespace Stride.Audio;

public sealed unsafe class ContextState
{

    private readonly bool swap;
    private readonly Context* mOldContext;
    private static readonly SpinLock sOpenAlLock = new();
    private static readonly ALContext alc = ALContext.GetApi();
    public ContextState(Context* context)
    {
        sOpenAlLock.Lock();

        mOldContext = alc.GetCurrentContext();
        if (context != mOldContext)
        {
            alc.MakeContextCurrent(context);
            swap = true;
        }
        else
        {
            swap = false;
        }
    }

    ~ContextState()
    {
        if (swap)
        {
            alc.MakeContextCurrent(mOldContext);
        }
        
        sOpenAlLock.Unlock();
    }    
}