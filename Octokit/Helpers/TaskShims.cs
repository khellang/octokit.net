#if !NET_45
namespace System.Threading.Tasks
{
    public static class TaskShims
    {
        public static Task ConfigureAwait(this Task task, bool continueOnCapturedContext)
        {
            // yuuuuuuuuuuuuuuuuuuuuuup
            return task;
        }

        public static Task<T> ConfigureAwait<T>(this Task<T> task, bool continueOnCapturedContext)
        {
            // yuuuuuuuuuuuuuuuuuuuuuup
            return task;
        }
    }
}
#endif