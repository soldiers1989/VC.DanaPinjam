using System;
using System.Threading;

public abstract class BaseThread
{
    public Thread WorkThread = null;
    public string ThreadName = String.Empty;


    public void Init()
    {
        WorkThread.IsBackground = true;
        WorkThread.Name = ThreadName;
        WorkThread = new Thread(new ThreadStart(ThreadProc));
        WorkThread.Start();
    }

    public virtual void ThreadProc()
    {

    }
    public bool Start()
    {
        return true;
    }

    public bool Stop()
    {
        return true;
    }
}