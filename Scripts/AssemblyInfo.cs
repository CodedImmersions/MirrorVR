using Mirror.VR;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

//General Info
[assembly: AssemblyTitle("MirrorVR")]
[assembly: AssemblyDescription("A wrapper for Mirror Networking that adds easy VR support.")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: AssemblyCompany("Coded Immersions")]
[assembly: AssemblyProduct("MirrorVR")]
[assembly: AssemblyCopyright("Copyright © Coded Immersions 2025-2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

//Com
[assembly: ComVisible(false)]
[assembly: Guid("8ec42443-a18b-4fc9-b1df-176c0b25201b")]

//Version
[assembly: AssemblyVersion(MirrorVRManager.Version)]
[assembly: AssemblyFileVersion(MirrorVRManager.Version)]

//Internals
[assembly: InternalsVisibleTo("Mirror.VR.Examples")]
