using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// Local Addressables load/release helper. Always Release handles when done — do not load-and-forget.
/// Groups (create via Game → Foundation → Initialize Addressables Groups): Core, Units, Abilities, Enemies, Bosses, VFX, Audio, UI.
/// </summary>
public static class AddressableContent
{
    public static async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
    {
        if (string.IsNullOrEmpty(address))
            return null;

        AsyncOperationHandle<T> handle = Addressables.LoadAssetAsync<T>(address);
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("[Addressables] Failed to load: " + address + " — " + handle.OperationException);
            if (handle.IsValid())
                Addressables.Release(handle);
            return null;
        }

        return handle.Result;
    }

    public static AsyncOperationHandle<T> LoadAssetHandleAsync<T>(string address) where T : UnityEngine.Object
    {
        return Addressables.LoadAssetAsync<T>(address);
    }

    public static void Release(AsyncOperationHandle handle)
    {
        if (handle.IsValid())
            Addressables.Release(handle);
    }

    public static void Release(UnityEngine.Object asset)
    {
        if (asset != null)
            Addressables.Release(asset);
    }

    public static async Task<GameObject> InstantiateAsync(string address, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, position, rotation, parent);
        await handle.Task;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError("[Addressables] Failed to instantiate: " + address);
            if (handle.IsValid())
                Addressables.Release(handle);
            return null;
        }

        return handle.Result;
    }

    public static void ReleaseInstance(GameObject instance)
    {
        if (instance != null)
            Addressables.ReleaseInstance(instance);
    }
}
