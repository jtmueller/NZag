using System;
using System.IO;
using System.Threading.Tasks;

namespace NZag.Utilities
{
    internal static class Throw
    {
        internal static void ObjectDisposed(string objectName)
            => throw new ObjectDisposedException(objectName);

        internal static void InvalidOperation(string message = null)
        {
            if (String.IsNullOrWhiteSpace(message)) throw new InvalidOperationException();
            else throw new InvalidOperationException(message);
        }

        internal static void NotSupported(string message = null)
        {
            if (String.IsNullOrWhiteSpace(message)) throw new NotSupportedException();
            else throw new NotSupportedException(message);
        }

        internal static void ArgumentOutOfRange(string paramName)
            => throw new ArgumentOutOfRangeException(paramName);

        internal static void ArgumentNull(string paramName)
            => throw new ArgumentNullException(paramName);

        internal static void Timeout(string message = null)
        {
            if (String.IsNullOrWhiteSpace(message)) throw new TimeoutException();
            else throw new TimeoutException(message);
        }

        internal static void InvalidCast()
            => throw new InvalidCastException();

        internal static void TaskCanceled()
            => throw new TaskCanceledException();

        internal static void Argument(string message, string paramName = null)
        {
            if (String.IsNullOrWhiteSpace(paramName)) throw new ArgumentException(message);
            else throw new ArgumentException(message, paramName);
        }

        internal static void FileNotFound(string message, string path)
            => throw new FileNotFoundException(message, path);

        internal static void IndexOutOfRange()
            => throw new IndexOutOfRangeException();
    }
}
