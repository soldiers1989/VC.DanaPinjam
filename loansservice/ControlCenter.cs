using System;

namespace loansservice
{
    public class ControlCenter
    {
        private TaskThread taskThread = null;
        public void Start()
        {
            taskThread.Init();
            taskThread.Start();
        }

        public void Stop()
        {
            taskThread.Stop();
        }
    }
}
