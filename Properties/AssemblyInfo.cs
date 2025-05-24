using MelonLoader;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("NeonLite3")]
[assembly: AssemblyProduct("NeonLite3")]

[assembly: ComVisible(false)]
[assembly: Guid("1de1e79a-01e5-48f7-b877-48836e3eb396")]

[assembly: AssemblyVersion("3.0.10.4")]
[assembly: AssemblyFileVersion("3.0.10.4")]

[assembly: MelonInfo(typeof(NeonLite.NeonLite), "NeonLite", "3.0.10+4", "Faustas, MOPSKATER, stxticOVFL")]
[assembly: MelonGame("Little Flag Software, LLC", "Neon White")]
[assembly: MelonPriority(-1000)]
#pragma warning disable CS0618
[assembly: MelonColor(System.ConsoleColor.White)] // uses the obsolete version for the sake of pre v0.6.0