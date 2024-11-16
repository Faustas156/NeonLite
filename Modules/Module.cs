namespace NeonLite.Modules
{
    /// <summary>
    /// A marker interface. All types inheriting IModule will be picked up by NeonLite automatically.
    /// They must include: 
    /// - a static bool `priority` and a static bool `active`,
    /// - a static void `Setup` to configure settings, load assets, and first activation, and
    /// - a static void `Activate(bool)` to preform Harmony (un)patching. Passed true means to activate, false means to deactivate.
    /// Optionally, it can also include a static void `OnLevelLoad(LevelData)` that will get called as late as possible before the staging screen.
    /// </summary>
    public interface IModule
    {
    }
}
