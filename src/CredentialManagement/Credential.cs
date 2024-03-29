﻿/* 
Copyright (c) GitHub Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace GitHub.Authentication.CredentialManagement
{
    public class Credential : IDisposable
    {
        const int maxPasswordLengthInBytes = NativeMethods.CREDUI_MAX_PASSWORD_LENGTH * 2;

        static readonly object _lockObject = new object();
        static readonly SecurityPermission _unmanagedCodePermission;

        CredentialType _type;
        string _target;
        SecureString _password;
        string _username;
        string _description;
        DateTime _lastWriteTime;
        PersistenceType _persistanceType;

        static Credential()
        {
            lock (_lockObject)
            {
                _unmanagedCodePermission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
            }
        }

        public Credential() : this(null, (string)null)
        {}

        public Credential(
            string username,
            SecureString password,
            string target = null)
        {
            Username = username;
            SecurePassword = password;
            Target = target;
            Type = CredentialType.Generic;
            PersistenceType = PersistenceType.LocalComputer;
            _lastWriteTime = DateTime.MinValue;
        }

        public Credential(
            string username,
            string password,
            string target = null)
        {
            Username = username;
            Password = password;
            Target = target;
            Type = CredentialType.Generic;
            PersistenceType = PersistenceType.LocalComputer;
            _lastWriteTime = DateTime.MinValue;
        }

        public static Credential Load(string key)
        {
            var result = new Credential();
            result.Target = key;
            result.Type = CredentialType.Generic;
            return result.Load() ? result : null;
        }

        public static void Save(string key, string username, string password)
        {
            var result = new Credential(username, password, key);
            result.Save();
        }

        public static void Delete(string key)
        {
            var result = new Credential();
            result.Target = key;
            result.Type = CredentialType.Generic;
            result.Delete();
        }

        bool disposed;
        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (disposed) return;
                SecurePassword.Clear();
                SecurePassword.Dispose();
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void CheckNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Credential object is already disposed.");
            }
        }

        public string Username
        {
            get
            {
                CheckNotDisposed();
                return _username;
            }
            set
            {
                CheckNotDisposed();
                _username = value;
            }
        }

        public string Password
        {
            get
            {
                return SecureStringHelper.CreateString(SecurePassword);
            }
            set
            {
                CheckNotDisposed();
                SecurePassword = SecureStringHelper.CreateSecureString(string.IsNullOrEmpty(value) ? string.Empty : value);
            }
        }

        public SecureString SecurePassword
        {
            get
            {
                CheckNotDisposed();
                _unmanagedCodePermission.Demand();
                return _password == null ? new SecureString() : _password.Copy();
            }
            set
            {
                CheckNotDisposed();
                if (_password != null)
                {
                    _password.Clear();
                    _password.Dispose();
                }
                _password = null == value ? new SecureString() : value.Copy();
            }
        }

        public string Target
        {
            get
            {
                CheckNotDisposed();
                return _target;
            }
            set
            {
                CheckNotDisposed();
                _target = value;
            }
        }

        public string Description
        {
            get
            {
                CheckNotDisposed();
                return _description;
            }
            set
            {
                CheckNotDisposed();
                _description = value;
            }
        }

        public DateTime LastWriteTime
        {
            get
            {
                return LastWriteTimeUtc.ToLocalTime();
            }
        }
        public DateTime LastWriteTimeUtc
        {
            get
            {
                CheckNotDisposed();
                return _lastWriteTime;
            }
            private set { _lastWriteTime = value; }
        }

        public CredentialType Type
        {
            get
            {
                CheckNotDisposed();
                return _type;
            }
            set
            {
                CheckNotDisposed();
                _type = value;
            }
        }

        public PersistenceType PersistenceType
        {
            get
            {
                CheckNotDisposed();
                return _persistanceType;
            }
            set
            {
                CheckNotDisposed();
                _persistanceType = value;
            }
        }

        public bool Save()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            byte[] passwordBytes = Encoding.Unicode.GetBytes(Password);
            ValidatePasswordLength(passwordBytes);

            var credential = new NativeMethods.CREDENTIAL
            {
                TargetName = Target,
                UserName = Username,
                CredentialBlob = Marshal.StringToCoTaskMemUni(Password),
                CredentialBlobSize = passwordBytes.Length,
                Comment = Description,
                Type = (int)Type,
                Persist = (int)PersistenceType
            };

            bool result = NativeMethods.CredWrite(ref credential, 0);
            if (!result)
            {
                return false;
            }
            LastWriteTimeUtc = DateTime.UtcNow;
            return true;
        }

        public bool Save(byte[] passwordBytes)
        {
            if (passwordBytes == null)
                throw new ArgumentNullException("passwordBytes");
           
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            ValidatePasswordLength(passwordBytes);

            var blob = Marshal.AllocCoTaskMem(passwordBytes.Length);
            Marshal.Copy(passwordBytes, 0, blob, passwordBytes.Length);

            var credential = new NativeMethods.CREDENTIAL
            {
                TargetName = Target,
                UserName = Username,
                CredentialBlob = blob,
                CredentialBlobSize = passwordBytes.Length,
                Comment = Description,
                Type = (int)Type,
                Persist = (int)PersistenceType
            };
            
            bool result = NativeMethods.CredWrite(ref credential, 0);
            Marshal.FreeCoTaskMem(blob);
            if (!result)
            {
                return false;
            }
            LastWriteTimeUtc = DateTime.UtcNow;
            return true;
        }

        public bool Delete()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            if (string.IsNullOrEmpty(Target))
            {
                throw new InvalidOperationException("Target must be specified to delete a credential.");
            }

            StringBuilder target = string.IsNullOrEmpty(Target) ? new StringBuilder() : new StringBuilder(Target);
            bool result = NativeMethods.CredDelete(target, Type, 0);
            return result;
        }

        public bool Load()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            IntPtr credPointer;

            bool result = NativeMethods.CredRead(Target, Type, 0, out credPointer);
            if (!result)
            {
                return false;
            }
            using (NativeMethods.CriticalCredentialHandle credentialHandle = new NativeMethods.CriticalCredentialHandle(credPointer))
            {
                LoadInternal(credentialHandle.GetCredential());
            }
            return true;
        }

        public bool Exists()
        {
            CheckNotDisposed();
            _unmanagedCodePermission.Demand();

            if (string.IsNullOrEmpty(Target))
            {
                throw new InvalidOperationException("Target must be specified to check existance of a credential.");
            }

            using (Credential existing = new Credential { Target = Target, Type = Type })
            {
                return existing.Load();
            }
        }

        internal void LoadInternal(NativeMethods.CREDENTIAL credential)
        {
            Username = credential.UserName;
            if (credential.CredentialBlobSize > 0)
            {
                Password = Marshal.PtrToStringUni(credential.CredentialBlob, credential.CredentialBlobSize / 2);
            }
            Target = credential.TargetName;
            Type = (CredentialType)credential.Type;
            PersistenceType = (PersistenceType)credential.Persist;
            Description = credential.Comment;
            LastWriteTimeUtc = DateTime.FromFileTimeUtc(credential.LastWritten);
        }

        static void ValidatePasswordLength(byte[] passwordBytes)
        {
            if (passwordBytes == null)
                throw new ArgumentNullException("passwordBytes");

            if (passwordBytes.Length > maxPasswordLengthInBytes)
            {
                var message = string.Format(CultureInfo.InvariantCulture,
                    "The password length ({0} bytes) exceeds the maximum password length ({1} bytes).",
                    passwordBytes.Length,
                    maxPasswordLengthInBytes);
                throw new ArgumentOutOfRangeException(message);
            }
        }
    }
}
