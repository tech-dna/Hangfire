using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Hangfire")]
[assembly: AssemblyDescription("Core classes of Hangfire that are independent of any framework.")]
[assembly: Guid("4deecd4f-19f6-426b-aa87-6cd1a03eaa48")]
[assembly: CLSCompliant(true)]
[assembly: InternalsVisibleTo("Hangfire.Core.Tests")]
[assembly: NeutralResourcesLanguage("en")]

// Allow the generation of mocks for internal types
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: SuppressMessage("Usage", "CA1510: Use 'ArgumentNullException.ThrowIfNull' instead of explicitly throwing a new exception instance", Justification = "Requires major overhaul")]
[assembly: SuppressMessage("Usage", "CA1305: The behavior of 'StringBuilder.Append(ref StringBuilder.AppendInterpolatedStringHandler)' could vary based on the current user's locale settings.")]
[assembly: SuppressMessage("Usage", "CA1513", Justification = "Requires major overhaul")]
[assembly: SuppressMessage("Usage", "CA1837", Justification = "Requires major overhaul")]
[assembly: SuppressMessage("Usage", "CA2263", Justification = "Requires major overhaul")]
[assembly: SuppressMessage("Usage", "CA1845", Justification = "Requires major overhaul")]
[assembly: SuppressMessage("Usage", "CA1846", Justification = "Requires major overhaul")]
[assembly: SuppressMessage("Usage", "CA0618", Justification = "Requires major overhaul")]
[assembly: SuppressMessage("Usage", "CA1864", Justification = "Requires major overhaul")]
[assembly: SuppressMessage("Usage", "SYSLIB0006", Justification = "Requires major overhaul")]
