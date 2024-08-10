using System.Collections.Concurrent;

namespace Tebot;

public class CallbackManager
{
    //string it`s a callback and its a key because when we recive updates we get only callback
    private ConcurrentDictionary<string, long> callbacksIds = new ConcurrentDictionary<string, long>();

    public long GetId(string callback, bool needToRemove = false){
        long id;
        bool isSuss = callbacksIds.TryGetValue(callback, out id);
        if(!isSuss)
            return -1;
        if(needToRemove){
            long idontknowwhy;
            callbacksIds.TryRemove(callback, out idontknowwhy);
        }
        return id;
    }

    public void RegistryCallback(string callback, long id){
        callbacksIds.TryAdd(callback, id);
    }
}