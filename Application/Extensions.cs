namespace KCD2_PAK;

public static class Extensions
{
    public static void WaitAndUnwrapException(this Task task) => task.GetAwaiter().GetResult();

    public static T WaitAndUnwrapException<T>(this Task<T> task) => task.GetAwaiter().GetResult();
}