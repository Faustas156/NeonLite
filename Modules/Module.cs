namespace NeonLite.Modules
{
    /// <summary>
    /// A marker interface. All types in an assembly inheriting IModule will be picked up by NeonLite upon calling `NeonLite.NeonLite.LoadModules(MelonAssembly)`.
    /// They must include a static bool `priority` and a static bool `active`.
    /// They can also optionally include:
    /// - a static void `Setup()` to configure settings, load assets, and first activation,
    /// - a static void `Activate(bool)` to preform Harmony (un)patching (passed true means to activate, false means to deactivate), and
    /// - a static void `OnLevelLoad(LevelData)` that will get called as late as possible before the staging screen.
    ///   - This can also return a bool to stall the load: `false` to stall and `true` to continue.
    /// </summary>
    public interface IModule
    {
    }
}
