using System;


namespace Core.UIFramework
{
    public interface UIWaiter {
        void Wait(string group, float threshold, float timeout,
            string note, string message, int buttonMask,
            Action retryHandler, Action okHandler);

        void Dialog(string group, string message, int buttonMask,
            Action retryHandler, Action okHandler);

        void Waiting(string group, string message);

        void Stop(string group);

        void StopAll();
    }
}