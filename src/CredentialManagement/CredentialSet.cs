using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
/* 
Copyright (c) GitHub Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System.Globalization;
using System.Linq;

using System.Runtime.InteropServices;

namespace GitHub.Authentication.CredentialManagement
{
    public class CredentialSet : List<Credential>, IDisposable
    {
        public CredentialSet()
        {
        }

        public CredentialSet(string target)
            : this()
        {
            if (string.IsNullOrWhiteSpace(target))
                throw new ArgumentException("target");

            Target = target;
        }

        public string Target { get; set; }

        bool disposed;
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                if (Count > 0)
                {
                    ForEach(cred => cred.Dispose());
                }
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public CredentialSet Load()
        {
            LoadInternal();
            return this;
        }

        private void LoadInternal()
        {
            uint count;

            IntPtr pCredentials;
            bool result = NativeMethods.CredEnumerateW(Target, 0, out count, out pCredentials);
            if (!result)
            {
                var lastError = Marshal.GetLastWin32Error();
                Trace.WriteLine(string.Format(CultureInfo.InvariantCulture, "Win32Exception: {0}", new Win32Exception(lastError)));
                return;
            }

            // Read in all of the pointers first
            var ptrCredList = new IntPtr[count];
            for (int i = 0; i < count; i++)
            {
                ptrCredList[i] = Marshal.ReadIntPtr(pCredentials, IntPtr.Size * i);
            }

            // Now let's go through all of the pointers in the list
            // and create our Credential object(s)
            var credentialHandles =
                ptrCredList.Select(ptrCred => new NativeMethods.CriticalCredentialHandle(ptrCred)).ToList();

            var existingCredentials = credentialHandles
                .Select(handle => handle.GetCredential())
                .Select(nativeCredential =>
                {
                    Credential credential = new Credential();
                    credential.LoadInternal(nativeCredential);
                    return credential;
                });
            AddRange(existingCredentials);

            // The individual credentials should not be free'd
            credentialHandles.ForEach(handle => handle.SetHandleAsInvalid());

            // Clean up memory to the Enumeration pointer
            NativeMethods.CredFree(pCredentials);
        }
    }
}