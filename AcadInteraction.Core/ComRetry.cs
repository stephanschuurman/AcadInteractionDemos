using System.Runtime.InteropServices;

namespace AcadInteraction.Core
{
    /// <summary>
    /// Retries COM calls that AutoCAD rejects while it is busy.
    /// </summary>
    public static class ComRetry
    {
        public const int RPC_E_CALL_REJECTED = unchecked((int)0x80010001);
        public const int RPC_E_SERVERCALL_RETRYLATER = unchecked((int)0x8001010A);

        public static bool IsBusy(Exception ex) =>
            ex is COMException com && (com.HResult == RPC_E_CALL_REJECTED || com.HResult == RPC_E_SERVERCALL_RETRYLATER);

        public static T Invoke<T>(Func<T> action, int attempts = 10, int delayMs = 200)
        {
            for (int i = 1; ; i++)
            {
                try
                {
                    return action();
                }
                catch (Exception ex) when (IsBusy(ex) && i < attempts)
                {
                    Thread.Sleep(delayMs);
                }
            }
        }

        public static void Release(object? comObject)
        {
            if (comObject != null && Marshal.IsComObject(comObject))
                Marshal.ReleaseComObject(comObject);
        }
    }
}
