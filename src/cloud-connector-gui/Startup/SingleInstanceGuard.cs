namespace CloudConnectorGui;

public static class SingleInstanceGuard
{
    private const string MutexName = "CloudConnectorGui-SingleInstance";
    private static Mutex? mutex;

    public static bool TryAcquire()
    {
        mutex = new Mutex(true, MutexName, out var createdNew);
        return createdNew;
    }

    public static void Release()
    {
        if (mutex is null)
        {
            return;
        }

        try
        {
            mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }

        mutex.Dispose();
        mutex = null;
    }
}
