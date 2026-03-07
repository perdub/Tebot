namespace Tebot.Model
{
    internal class PersistentStateConfigurationImpl(string stateName, string storageName) : IPersistentStateConfiguration
    {
        public string StateName => stateName;

        public string StorageName => storageName;
    }
}