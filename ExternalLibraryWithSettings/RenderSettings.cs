using LuaSettings;

namespace ExternalLibraryWithSettings
{
    public enum RenderMode
    {
        None,
        DirectX,
        Vulkan
    }

    [Settings("RenderSettings")]
    public class RenderSettings
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public RenderMode RenderMode { get; set; }
    }
}
