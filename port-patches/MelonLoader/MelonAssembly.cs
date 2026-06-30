using Semver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using MelonLoader.Logging;
using MelonLoader.Utils;
#if NET6_0_OR_GREATER
using System.Runtime.Loader;
using System.Runtime.InteropServices;
#endif

namespace MelonLoader
{
    public sealed class MelonAssembly
    {
        #region Static

        /// <summary>
        /// Called before a process of resolving Melons from a MelonAssembly has started.
        /// </summary>
        public static readonly MelonEvent<Assembly> OnAssemblyResolving = new();

        public static event LemonFunc<Assembly, ResolvedMelons> CustomMelonResolvers;

        internal static List<MelonAssembly> loadedAssemblies = new();

        /// <summary>
        /// List of all loaded MelonAssemblies.
        /// </summary>
        public static ReadOnlyCollection<MelonAssembly> LoadedAssemblies => loadedAssemblies.AsReadOnly();

        /// <summary>
        /// Tries to find the instance of Melon with type T, whether it's registered or not
        /// </summary>
        public static T FindMelonInstance<T>() where T : MelonBase
        {
            foreach (var asm in loadedAssemblies)
            {
                foreach (var melon in asm.loadedMelons)
                {
                    if (melon is T teaMelon)
                        return teaMelon;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the MelonAssembly of the given member. If the given member is not in any MelonAssembly, returns null.
        /// </summary>
        public static MelonAssembly GetMelonAssemblyOfMember(MemberInfo member, object obj = null)
        {
            if (member == null)
                return null;

            if (obj != null && obj is MelonBase melon)
                return melon.MelonAssembly;

            var name = member.DeclaringType.Assembly.FullName;
            var ma = loadedAssemblies.Find(x => x.Assembly.FullName == name);
            return ma;
        }

        /// <summary>
        /// Loads or finds a MelonAssembly from path.
        /// </summary>
        /// <param name="path">Path of the MelonAssembly</param>
        /// <param name="loadMelons">Sets whether Melons should be auto-loaded or not</param>
        public static MelonAssembly LoadMelonAssembly(string path, bool loadMelons = true)
        {
            if (path == null)
            {
                MelonLogger.Error("Failed to load a Melon Assembly: Path cannot be null.");
                return null;
            }

            path = Path.GetFullPath(path);

            try
            {
#if NET6_0_OR_GREATER
                Assembly assembly;
                try
                {
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                }
                catch (Exception)
                {
                    // macOS Apple-Silicon port: community Mod Helper mods are architecture-neutral IL but
                    // ship built x64 (PE32+), which coreclr's loader rejects on arm64 with a FileLoadException.
                    // Re-mark the assembly AnyCPU in place (the IL itself is untouched) and retry once. Anything
                    // that isn't a pure-IL x64 image (already AnyCPU, or mixed-mode native) is left alone and
                    // the original failure is rethrown below.
                    if (!TryConvertWindowsX64ToAnyCpu(path))
                        throw;
                    MelonLogger.Msg($"Auto-converted x64 Melon Assembly to AnyCPU for load: {Path.GetFileName(path)}");
                    assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                }
#else
                var assembly = Assembly.LoadFrom(path);
#endif
                return LoadMelonAssembly(path, assembly, loadMelons);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load Melon Assembly from '{path}':\n{ex}");
                return null;
            }
        }

#if NET6_0_OR_GREATER
        // macOS-only safety net for community mods. A pure-IL assembly built x64 (PE32+ / Required32Bit) is
        // rejected by coreclr's loader on Apple Silicon even though its IL is architecture-neutral. This
        // re-marks such an assembly AnyCPU (PE32 / I386, no 32-bit-required flag) in place so the retry load
        // succeeds. Returns true only when a conversion was actually performed; no-ops (false) for assemblies
        // that are already AnyCPU or are mixed-mode (contain native code we must not rewrite).
        private static bool TryConvertWindowsX64ToAnyCpu(string path)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return false;
            string tmp = path + ".anycpu.tmp";
            try
            {
                using (var module = Mono.Cecil.ModuleDefinition.ReadModule(path,
                           new Mono.Cecil.ReaderParameters { InMemory = true }))
                {
                    bool ilOnly = (module.Attributes & Mono.Cecil.ModuleAttributes.ILOnly) != 0;
                    bool isAnyCpu = module.Architecture == Mono.Cecil.TargetArchitecture.I386
                                    && (module.Attributes & Mono.Cecil.ModuleAttributes.Required32Bit) == 0;
                    if (isAnyCpu || !ilOnly)
                        return false; // nothing to do, or mixed-mode native code — don't touch it
                    module.Architecture = Mono.Cecil.TargetArchitecture.I386;
                    module.Attributes = Mono.Cecil.ModuleAttributes.ILOnly; // drops Required32Bit / Preferred32Bit
                    module.Write(tmp);
                }
                File.Delete(path);
                File.Move(tmp, path);
                return true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
                MelonLogger.Warning($"x64->AnyCPU auto-convert failed for '{Path.GetFileName(path)}': {ex.Message}");
                return false;
            }
        }
#endif

        /// <summary>
        /// Loads or finds a MelonAssembly from raw Assembly Data.
        /// </summary>
        public static MelonAssembly LoadRawMelonAssembly(string path, byte[] assemblyData, byte[] symbolsData = null, bool loadMelons = true)
        {
            if (assemblyData == null)
            {
                MelonLogger.Error("Failed to load a Melon Assembly: assemblyData cannot be null.");
                return null;
            }

            try
            {
#if NET6_0_OR_GREATER
                var fileStream = new MemoryStream(assemblyData);
                var symStream = symbolsData == null ? null : new MemoryStream(symbolsData);

                var assembly = AssemblyLoadContext.Default.LoadFromStream(fileStream, symStream);
#else
                var assembly = symbolsData != null ? Assembly.Load(assemblyData, symbolsData) : Assembly.Load(assemblyData);
#endif
                return LoadMelonAssembly(path, assembly, loadMelons);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"Failed to load Melon Assembly from raw Assembly Data (length {assemblyData.Length}):\n{ex}");
                return null;
            }
        }

        /// <summary>
        /// Loads or finds a MelonAssembly.
        /// </summary>
        public static MelonAssembly LoadMelonAssembly(string path, Assembly assembly, bool loadMelons = true)
        {
            if (!File.Exists(path))
                path = assembly.Location;

            if (assembly == null)
            {
                MelonLogger.Error("Failed to load a Melon Assembly: Assembly cannot be null.");
                return null;
            }

            var ma = loadedAssemblies.Find(x => x.Assembly.FullName == assembly.FullName);
            if (ma != null)
                return ma;

            var shortPath = path;
            if (shortPath.StartsWith(MelonEnvironment.MelonBaseDirectory))
                shortPath = "." + shortPath.Remove(0, MelonEnvironment.MelonBaseDirectory.Length);

            OnAssemblyResolving.Invoke(assembly);
            ma = new MelonAssembly(assembly, path);
            loadedAssemblies.Add(ma);

            if (loadMelons)
                ma.LoadMelons();

            MelonLogger.MsgDirect(ColorARGB.DarkGray, $"Melon Assembly loaded: '{shortPath}'");
            MelonLogger.MsgDirect(ColorARGB.DarkGray, $"SHA256 Hash: '{ma.Hash}'");
            return ma;
        }

        #endregion

        #region Instance

        private bool melonsLoaded;

        private readonly List<MelonBase> loadedMelons = new();
        private readonly List<RottenMelon> rottenMelons = new();

        public readonly MelonEvent OnUnregister = new();

        public bool HarmonyDontPatchAll { get; private set; } = true;

        /// <summary>
        /// A SHA256 Hash of the Assembly.
        /// </summary>
        public string Hash { get; private set; }

        public Assembly Assembly { get; private set; }

        public string Location { get; private set; }

        /// <summary>
        /// A list of all loaded Melons in the Assembly.
        /// </summary>
        public ReadOnlyCollection<MelonBase> LoadedMelons => loadedMelons.AsReadOnly();

        /// <summary>
        /// A list of all broken Melons in the Assembly.
        /// </summary>
        public ReadOnlyCollection<RottenMelon> RottenMelons => rottenMelons.AsReadOnly();

        private MelonAssembly(Assembly assembly, string location)
        {
            Assembly = assembly;
            Location = location ?? "";
            Hash = MelonUtils.ComputeSimpleSHA256Hash(Location);
        }

        /// <summary>
        /// Unregisters all Melons in this Assembly.
        /// </summary>
        public void UnregisterMelons(string reason = null, bool silent = false)
        {
            foreach (var m in loadedMelons)
                m.UnregisterInstance(reason, silent);

            OnUnregister.Invoke();
        }

        private void OnApplicationQuit()
        {
            UnregisterMelons("MelonLoader is deinitializing.", true);
        }

        private T SafeGetAttribute<T>(bool rotten = false)
            where T : Attribute
        {
            try
            {
                var att = MelonUtils.PullAttributeFromAssembly<T>(Assembly);
                return att;
            }
            catch (Exception ex)
            {
                if (rotten)
                    rottenMelons.Add(new RottenMelon(Assembly, $"Failed to Pull Attribute '{typeof(T).Name}' from the Melon.", ex));
                else
                {
                    MelonLogger.Error($"Failed to Pull Attribute '{typeof(T).Name}' from the Melon.");
                    MelonLogger.Error(ex);
                }
                return null;
            }
        }

        private T[] SafeGetAllAttributes<T>(bool rotten = false)
            where T : Attribute
        {
            try
            {
                var att = MelonUtils.PullAttributesFromAssembly<T>(Assembly);
                return att;
            }
            catch (Exception ex)
            {
                if (rotten)
                    rottenMelons.Add(new RottenMelon(Assembly, $"Failed to Pull Attribute '{typeof(T).Name}' from the Melon.", ex));
                else
                {
                    MelonLogger.Error($"Failed to Pull Attribute '{typeof(T).Name}' from the Melon.");
                    MelonLogger.Error(ex);
                }
                return null;
            }
        }

        public void LoadMelons()
        {
            if (melonsLoaded)
                return;

            melonsLoaded = true;

            MelonEvents.OnApplicationDefiniteQuit.Subscribe(OnApplicationQuit);

            // \/ Custom Resolver \/
            var resolvers = CustomMelonResolvers?.GetInvocationList();
            if (resolvers != null)
                foreach (LemonFunc<Assembly, ResolvedMelons> r in resolvers)
                {
                    var customMelon = r.Invoke(Assembly);

                    loadedMelons.AddRange(customMelon.loadedMelons);
                    rottenMelons.AddRange(customMelon.rottenMelons);
                }


            // \/ Default resolver \/
            var baseType = typeof(MelonBase);
            var info = SafeGetAttribute<MelonInfoAttribute>(true);
            if ((info != null)
                && (info.SystemType != null)
                && info.SystemType.IsSubclassOf(baseType))
            {
                var baseTypeMod = typeof(MelonMod);
                var baseTypePlugin = typeof(MelonPlugin);
                if ((info.SystemType == baseType)
                    || (info.SystemType == baseTypeMod)
                    || (info.SystemType == baseTypePlugin))
                {
                    rottenMelons.Add(new RottenMelon(Assembly, $"{info.SystemType.FullName} cannot be used for MelonInfoAttribute.SystemType"));
                    return;
                }

                MelonBase melon;
                try
                {
                    melon = (MelonBase)Activator.CreateInstance(info.SystemType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, null, null);
                }
                catch (Exception ex)
                {
                    melon = null;
                    rottenMelons.Add(new RottenMelon(info.SystemType, "Failed to create an instance of the Melon.", ex));
                }

                if (melon != null)
                {
                    var priorityAttr = SafeGetAttribute<MelonPriorityAttribute>();
                    var colorAttr = SafeGetAttribute<MelonColorAttribute>();
                    var authorColorAttr = SafeGetAttribute<MelonAuthorColorAttribute>();
                    var additionalCreditsAttr = SafeGetAttribute<MelonAdditionalCreditsAttribute>();
                    var procAttrs = SafeGetAllAttributes<MelonProcessAttribute>();
                    var gameAttrs = SafeGetAllAttributes<MelonGameAttribute>();
                    var optionalDependenciesAttr = SafeGetAttribute<MelonOptionalDependenciesAttribute>();
                    var idAttr = SafeGetAttribute<MelonIDAttribute>();
                    var gameVersionAttrs = SafeGetAllAttributes<MelonGameVersionAttribute>();
                    var platformAttr = SafeGetAttribute<MelonPlatformAttribute>();
                    var domainAttr = SafeGetAttribute<MelonPlatformDomainAttribute>();
                    var mlVersionAttr = SafeGetAttribute<VerifyLoaderVersionAttribute>();
                    var mlBuildAttr = SafeGetAttribute<VerifyLoaderBuildAttribute>();
                    var harmonyDPAAttr = SafeGetAttribute<HarmonyDontPatchAllAttribute>();

                    melon.Info = info;
                    melon.AdditionalCredits = additionalCreditsAttr;
                    melon.MelonAssembly = this;
                    melon.Priority = priorityAttr?.Priority ?? 0;
                    melon.ConsoleColor = colorAttr?.DrawingColor ?? MelonLogger.DefaultMelonColor;
                    melon.AuthorConsoleColor = authorColorAttr?.DrawingColor ?? MelonLogger.DefaultTextColor;
                    melon.SupportedProcesses = procAttrs;
                    melon.Games = gameAttrs;
                    melon.SupportedGameVersions = gameVersionAttrs;
                    melon.SupportedPlatforms = platformAttr;
                    melon.SupportedDomain = domainAttr;
                    melon.SupportedMLVersion = mlVersionAttr;
                    melon.SupportedMLBuild = mlBuildAttr;
                    melon.OptionalDependencies = optionalDependenciesAttr;
                    melon.ID = idAttr?.ID;
                    HarmonyDontPatchAll = harmonyDPAAttr != null;

                    loadedMelons.Add(melon);

                    if (!SemVersion.TryParse(info.Version, out _))
                        MelonLogger.Warning($"==Normal users can ignore this warning==\nMelon '{info.Name}' by '{info.Author}' has version '{info.Version}' which does not use the Semantic Versioning format. Versions using formats other than the Semantic Versioning format will not be supported in the future versions of MelonLoader.\nFor more details, see: https://semver.org");
                }
            }

#if NET6_0_OR_GREATER
            RegisterTypeInIl2Cpp.RegisterAssembly(Assembly);
            RegisterTypeInIl2CppWithInterfaces.RegisterAssembly(Assembly);
#endif

            if (rottenMelons.Count != 0)
            {
                MelonLogger.Error($"Failed to load {rottenMelons.Count} {"Melon".MakePlural(rottenMelons.Count)} from {Path.GetFileName(Location)}:");
                foreach (var r in rottenMelons)
                {
                    if (r.type != null)
                        MelonLogger.Error($"Failed to load Melon '{r.type.FullName}': {r.errorMessage}");
                    else if (r.assembly != null)
                        MelonLogger.Error($"Failed to load Melon '{r.assembly.GetName().Name}': {r.errorMessage}");
                    if (!string.IsNullOrEmpty(r.exception))
                        MelonLogger.Error(r.exception);
                }
            }
        }

        #endregion
    }
}