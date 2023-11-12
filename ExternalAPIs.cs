using System.Reflection;

namespace JsonAssets
{
    public interface IApi
    {
        void LoadAssets(string path);
        int GetObjectId(string name);
    }
}

namespace SpaceCore
{
    public interface IApi
    {
        void AddEventCommand(string command, MethodInfo info);
    }
}
