// Rewrites a pure-IL (ILOnly) x64 (PE32+ AMD64) .NET assembly to AnyCPU (PE32 / I386) in place.
// Community BTD6 Mod Helper mods are built x64 by default, which this macOS arm64 port's loader
// rejects with FileLoadException — even though the IL is architecture-neutral. Converting to AnyCPU
// makes them load and run. No-ops (and reports) if the assembly is already AnyCPU or is mixed-mode.
using System;
using Mono.Cecil;

class Program
{
    static int Main(string[] args)
    {
        if (args.Length < 1) { Console.Error.WriteLine("usage: anycpu-convert <assembly.dll>"); return 2; }
        var path = args[0];
        ModuleDefinition m;
        try { m = ModuleDefinition.ReadModule(path); }
        catch (Exception e) { Console.Error.WriteLine($"not a .NET assembly: {path}: {e.Message}"); return 3; }

        bool ilOnly = (m.Attributes & ModuleAttributes.ILOnly) != 0;
        if (m.Architecture == TargetArchitecture.I386 && (m.Attributes & ModuleAttributes.Required32Bit) == 0)
        {
            Console.WriteLine($"already AnyCPU: {path}");
            return 0;
        }
        if (!ilOnly)
        {
            Console.Error.WriteLine($"REFUSING: {path} is mixed-mode (has native code) — cannot convert to AnyCPU");
            return 4;
        }
        m.Architecture = TargetArchitecture.I386;
        m.Attributes = ModuleAttributes.ILOnly; // drop Required32Bit / Preferred32Bit
        m.Write(path + ".tmp");
        m.Dispose();
        System.IO.File.Delete(path);
        System.IO.File.Move(path + ".tmp", path);
        Console.WriteLine($"converted to AnyCPU: {path}");
        return 0;
    }
}
