using System.Linq;

namespace ApsantaScanner.Test.Helpers
{
    public class SystemWebMvcStub
    {
        /// <summary>
        /// Types in the System.Web.Mvc namespace that the analyzer looks for, so we don't have to reference ASP.NET MVC bits.
        /// </summary>
        public const string SystemWebMvcNamespaceCSharp = @"
namespace System.Web.Mvc
{
    using System;

    public class Controller { }

    public class ControllerBase { }

    public class ActionResult { }

    public class ContentResult : ActionResult { }

    public class ValidateAntiForgeryTokenAttribute : Attribute { }

    public class HttpGetAttribute : Attribute { }

    public class HttpPostAttribute : Attribute { }

    public class HttpPutAttribute : Attribute { }

    public class HttpDeleteAttribute : Attribute { }

    public class HttpPatchAttribute : Attribute { }

    public class AcceptVerbsAttribute : Attribute 
    {
        public AcceptVerbsAttribute(params string[] verbs)
        {
        }

        public AcceptVerbsAttribute(HttpVerbs verbs)
        {
        }
    }

    public class NonActionAttribute : Attribute { }

    public class ChildActionOnlyAttribute : Attribute { }

    /// <summary>Enumerates the HTTP verbs.</summary>
    [Flags]
    public enum HttpVerbs
    {
	    /// <summary>Retrieves the information or entity that is identified by the URI of the request.</summary>
	    Get = 1,
	    /// <summary>Posts a new entity as an addition to a URI.</summary>
	    Post = 2,
	    /// <summary>Replaces an entity that is identified by a URI.</summary>
	    Put = 4,
	    /// <summary>Requests that a specified URI be deleted.</summary>
	    Delete = 8,
	    /// <summary>Retrieves the message headers for the information or entity that is identified by the URI of the request.</summary>
	    Head = 0x10,
	    /// <summary>Requests that a set of changes described in the request entity be applied to the resource identified by the Request-URI.</summary>
	    Patch = 0x20,
	    /// <summary>Represents a request for information about the communication options available on the request/response chain identified by the Request-URI.</summary>
	    Options = 0x40
    }
}
";

        /// <summary>
        /// If the tested source code starts with SystemWebMvcNamespaceCSharp, we can add offsets when specifying location line numbers,
        /// and not break everything when adding to SystemWebMvcNamespaceCSharp.
        /// </summary>
        public static readonly int SystemWebMvcNamespaceCSharpLineCount = SystemWebMvcNamespaceCSharp.Count(ch => ch == '\n');

        /// <summary>
        /// Types in the System.Web.Mvc namespace that the analyzer looks for, so we don't have to reference ASP.NET MVC bits.
        /// </summary>
        public const string SystemWebMvcNamespaceBasic = @"
Imports System

Namespace System.Web.Mvc
    Public Class Controller
    End Class

    Public Class ControllerBase
    End Class

    Public Class ActionResult
    End Class

    Public Class ContentResult
        Inherits ActionResult
    End Class

    Public Class ValidateAntiForgeryTokenAttribute
        Inherits Attribute
    End Class

    Public Class HttpGetAttribute
        Inherits Attribute
    End Class

    Public Class HttpPostAttribute
        Inherits Attribute
    End Class

    Public Class HttpPutAttribute
        Inherits Attribute
    End Class

    Public Class HttpDeleteAttribute
        Inherits Attribute
    End Class

    Public Class HttpPatchAttribute
        Inherits Attribute
    End Class

    Public Class AcceptVerbsAttribute
        Inherits Attribute

        Public Sub New(ByVal ParamArray verbs() As String)
        End Sub

        Public Sub New(ByVal verbs As HttpVerbs)
        End Sub
    End Class

    Public Class NonActionAttribute
        Inherits Attribute
    End Class

    Public Class ChildActionOnlyAttribute
        Inherits Attribute
    End Class

    <Flags>
    Public Enum HttpVerbs
        [Get] = 1
        Post = 2
        Put = 4
        Delete = 8
        Head = 16
        Patch = 32
        Options = 64
    End Enum
End Namespace
";

        /// <summary>
        /// If the tested source code starts with SystemWebMvcNamespaceBasic, we can add offsets when specifying location line numbers,
        /// and not break everything when adding to SystemWebMvcNamespaceBasic.
        /// </summary>
        public static readonly int SystemWebMvcNamespaceBasicLineCount = SystemWebMvcNamespaceBasic.Count(ch => ch == '\n');
    }
}
