using System.Reflection;
using System.Runtime.Loader;

namespace Mqutil.Services
{
    // https://github.com/dotnet/samples/blob/master/core/extensions/AppWithPlugin/AppWithPlugin/PluginLoadContext.cs
    public sealed class PluginLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;

        public PluginLoadContext(string path)
        {
            _resolver = new AssemblyDependencyResolver(path);
        }

        protected override Assembly Load(AssemblyName assemblyName)
        {
            //string assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            //if (assemblyPath != null)
            //{
            //    return LoadFromAssemblyPath(assemblyPath);
            //}

            // https://github.com/dotnet/coreclr/issues/13277
            // see also https://gist.github.com/andrewLarsson/f5351a7c9234ba8c0981037f79108344
            // the AssemblyDependencyResolver intentionally does not resolve
            // any framework assemblies; we return null if the resolver doesn't
            // resolve an assembly, as this should lead the binder to try to
            // load the framework assembly from the AssemblyLoadContext.Default.
            // This design is intentional, since it's almost never desirable
            // to load multiple copies of framework assemblies. This way all
            // framework assemblies are loaded through the default context,
            // and thus end up loaded exactly once.
            return null;
        }
    }
}
