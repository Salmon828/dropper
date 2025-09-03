using System;

namespace Unity.Services.Multiplay.Authoring.Core.Exceptions
{
    class UploadException : Exception
    {
        public UploadException(string message, Exception inner = null)
            : base(message, inner) {}
    }
}
