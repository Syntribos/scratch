namespace Scratch;

public static class Extensions
{
    public static void List<T>(this List<T> list, Action<T> action)
    {
        foreach (var item in list) action(item);
    }
}