/*
 * Work around replacements for stuff in Python, but not in Csharp
 */
namespace util
{
    class pfun
    {
        public static bool isDigit(char c) // taken directly from clox
        {
            return c >= '0' && c <= '9';
        }

        public static bool isAlpha(char c) // taken directly from clox
        {
            return (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                c == '_';
        }

        public static bool isAlNum(char c)
        {
            return isDigit(c) || isAlpha(c);
        }

        public static void Exit(string message, int value = -1)
        {
            System.Console.WriteLine(message);
            System.Environment.Exit(value);
        }

    }
}
